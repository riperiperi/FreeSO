using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FSO.AgentBridge
{
    /// <summary>
    /// OpenAI backend, over the Chat Completions API.
    ///
    /// Raw HTTP rather than an SDK on purpose: the only thing this class does is translate
    /// between our reflected tool schemas and one stable, well-documented JSON shape. An SDK
    /// would add a dependency and hide the mapping, and the mapping is the part worth reading.
    /// </summary>
    public class OpenAIProvider : ILlmProvider
    {
        public const string DefaultModel = "gpt-4o";

        // Approximate list price per million tokens, and DELIBERATELY overridable: these are
        // hardcoded rates that go stale, and a wrong constant here silently produces a wrong
        // cost figure that someone then makes a pricing decision on. Defaults are gpt-4o.
        // Cached input bills at a discount on OpenAI; the 0.5x is an approximation.
        private readonly double _inputPerM, _outputPerM, _cachedInputPerM;

        private readonly HttpClient _http;
        private readonly string _endpoint;
        private readonly JsonArray _history = new();

        public string Name => "openai";
        public string Model { get; }
        public LlmUsage Usage { get; } = new();

        /// <param name="http">Injected with its Authorization header already set — this class
        /// never sees or stores the key, matching the Anthropic path where the SDK client is
        /// constructed outside.</param>
        public OpenAIProvider(HttpClient http, string model = DefaultModel,
            string endpoint = "https://api.openai.com/v1/chat/completions",
            double inputPerM = 2.50, double outputPerM = 10.00, double cachedInputPerM = 1.25)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _endpoint = endpoint;
            Model = model;
            _inputPerM = inputPerM;
            _outputPerM = outputPerM;
            _cachedInputPerM = cachedInputPerM;
        }

        public void AddUserMessage(string text) =>
            _history.Add(new JsonObject { ["role"] = "user", ["content"] = text });

        public void AddToolResults(IReadOnlyList<LlmToolResult> results)
        {
            // Unlike Anthropic (all results in one user message), OpenAI wants one message
            // per result, each with role "tool" and the originating tool_call_id.
            foreach (var r in results)
            {
                _history.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = r.Id,
                    // No is_error flag on this API — a failed call is just result content,
                    // which suits us: our diagnostics are meant to be read and acted on.
                    ["content"] = r.ResultJson,
                });
            }
        }

        public async Task<LlmTurn> NextTurnAsync(
            string systemPrompt,
            IReadOnlyList<ToolSchemaGenerator.ToolDefinition> tools,
            int maxTokens)
        {
            var messages = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = systemPrompt });
            foreach (var m in _history) messages.Add(m.DeepClone());

            var body = new JsonObject
            {
                ["model"] = Model,
                ["max_completion_tokens"] = maxTokens,
                ["messages"] = messages,
                ["tools"] = new JsonArray(tools.Select(ToOpenAiTool).ToArray()),
            };

            JsonNode json;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, _endpoint)
                {
                    Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
                };
                using var resp = await _http.SendAsync(req);
                var text = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode) throw Classify(resp.StatusCode, text);
                json = JsonNode.Parse(text);
            }
            catch (AgentFailure) { throw; }
            catch (Exception e)
            {
                throw new AgentFailure(AgentFailureKind.Transient, e.Message, e);
            }

            var usage = json?["usage"];
            Usage.Turns++;
            Usage.InputTokens += usage?["prompt_tokens"]?.GetValue<long>() ?? 0;
            Usage.OutputTokens += usage?["completion_tokens"]?.GetValue<long>() ?? 0;
            // OpenAI reports cached prompt tokens nested under prompt_tokens_details; they're
            // already counted in prompt_tokens, so this is recorded for visibility only and
            // deliberately not added into the input total.
            Usage.CacheReadTokens += usage?["prompt_tokens_details"]?["cached_tokens"]?.GetValue<long>() ?? 0;

            // Cached tokens are already inside prompt_tokens, so bill the uncached remainder
            // at full rate and the cached portion at the discounted rate.
            var uncached = Math.Max(0, Usage.InputTokens - Usage.CacheReadTokens);
            Usage.EstimatedCostUsd =
                uncached / 1e6 * _inputPerM +
                Usage.CacheReadTokens / 1e6 * _cachedInputPerM +
                Usage.OutputTokens / 1e6 * _outputPerM;

            var message = json?["choices"]?[0]?["message"];
            if (message == null)
                throw new AgentFailure(AgentFailureKind.Bug, "response had no choices[0].message");

            // Echo the assistant message back verbatim — it carries the tool_calls the
            // follow-up tool messages must pair with.
            _history.Add(message.DeepClone());

            var turn = new LlmTurn();
            var content = message["content"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(content)) turn.Narration.Add(content.Trim());

            foreach (var call in message["tool_calls"]?.AsArray() ?? new JsonArray())
            {
                turn.ToolCalls.Add(new LlmToolCall
                {
                    Id = call["id"]?.GetValue<string>(),
                    ToolName = call["function"]?["name"]?.GetValue<string>(),
                    // Arguments arrive as a JSON *string*, not an object — the one real
                    // shape difference from Anthropic, which sends a parsed object.
                    ArgumentsJson = call["function"]?["arguments"]?.GetValue<string>() ?? "{}",
                });
            }

            return turn;
        }

        /// <summary>
        /// Our reflected schema is already JSON Schema, so this is a wrapper, not a
        /// translation: name/description/parameters nested under a "function" envelope.
        /// </summary>
        private static JsonNode ToOpenAiTool(ToolSchemaGenerator.ToolDefinition t) => new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = t.Name,
                ["description"] = t.Description,
                ["parameters"] = t.InputSchema.DeepClone(),
            },
        };

        private static AgentFailure Classify(HttpStatusCode status, string body)
        {
            var code = ExtractErrorCode(body);

            // 429 covers both "too fast" and "you're out of money" — the error code is what
            // separates a retry-in-a-second from a never-going-to-work.
            if (status == HttpStatusCode.TooManyRequests)
            {
                return string.Equals(code, "insufficient_quota", StringComparison.OrdinalIgnoreCase)
                    ? new AgentFailure(AgentFailureKind.OutOfCredit, body)
                    : new AgentFailure(AgentFailureKind.RateLimited, body);
            }

            if (status == HttpStatusCode.Unauthorized || status == HttpStatusCode.Forbidden)
                return new AgentFailure(AgentFailureKind.Unauthorized, body);

            if ((int)status >= 500)
                return new AgentFailure(AgentFailureKind.Transient, body);

            return new AgentFailure(AgentFailureKind.Bug, body);
        }

        private static string ExtractErrorCode(string body)
        {
            try { return JsonNode.Parse(body)?["error"]?["code"]?.GetValue<string>(); }
            catch { return null; }
        }
    }
}
