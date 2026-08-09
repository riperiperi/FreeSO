using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FSO.AgentBridge
{
    /// <summary>
    /// One assistant turn, normalized across providers: the prose the player sees and the
    /// tool calls the agent must execute.
    /// </summary>
    public class LlmTurn
    {
        public List<string> Narration = new();
        public List<LlmToolCall> ToolCalls = new();
    }

    public class LlmToolCall
    {
        /// <summary>Provider's id for this call, echoed back with the result so they pair up.</summary>
        public string Id;
        public string ToolName;
        /// <summary>Raw JSON object of arguments, as the model emitted it.</summary>
        public string ArgumentsJson;
    }

    public class LlmToolResult
    {
        public string Id;
        public string ToolName;
        public string ResultJson;
        public bool IsError;
    }

    public class LlmUsage
    {
        public long Turns;
        public long InputTokens;
        public long OutputTokens;
        public long CacheReadTokens;
        public long CacheWriteTokens;

        /// <summary>
        /// Rough list-price estimate. Per-provider because the rate card differs; the point
        /// is a per-object number Kat can reason about, not billing-grade accounting.
        /// </summary>
        public double EstimatedCostUsd;
    }

    /// <summary>
    /// Why a run failed, in terms of what the player should be told to DO about it.
    /// The categories are chosen by remedy, not by HTTP status: two different status codes
    /// that call for the same player action belong in the same bucket.
    /// </summary>
    public enum AgentFailureKind
    {
        /// <summary>Blip, overload, timeout. Retrying genuinely works.</summary>
        Transient,

        /// <summary>Account has no credit. Retrying can NEVER work — needs money or a key.</summary>
        OutOfCredit,

        /// <summary>Quota/rate limit on the shared account. Waiting or a personal key works.</summary>
        RateLimited,

        /// <summary>Bad key, revoked key, wrong endpoint. Needs an operator, not a player.</summary>
        Unauthorized,

        /// <summary>Our defect. Retrying probably won't help and it isn't the player's fault.</summary>
        Bug,
    }

    /// <summary>
    /// Carries a failure already classified by remedy, so the agent never has to sniff
    /// provider-specific exception text to decide what to tell the player.
    /// </summary>
    public class AgentFailure : Exception
    {
        public readonly AgentFailureKind Kind;

        public AgentFailure(AgentFailureKind kind, string technicalDetail, Exception inner = null)
            : base(technicalDetail, inner)
        {
            Kind = kind;
        }
    }

    /// <summary>
    /// A provider owns its own conversation history in its own native message types —
    /// Anthropic needs thinking blocks echoed back with intact signatures, OpenAI needs
    /// tool_call ids threaded through assistant/tool messages, and neither round-trip
    /// survives being flattened into a shared shape. So the agent drives the conversation
    /// through this interface and never sees a provider's wire format.
    /// </summary>
    public interface ILlmProvider
    {
        /// <summary>For logs and diagnostics. Never shown to a player — see NARRATION-CONTRACT.md.</summary>
        string Name { get; }

        string Model { get; }

        LlmUsage Usage { get; }

        void AddUserMessage(string text);

        void AddToolResults(IReadOnlyList<LlmToolResult> results);

        /// <summary>
        /// Sends the accumulated conversation and returns one assistant turn, appending it to
        /// the provider's own history. Throws <see cref="AgentFailure"/> — already classified.
        /// </summary>
        Task<LlmTurn> NextTurnAsync(
            string systemPrompt,
            IReadOnlyList<ToolSchemaGenerator.ToolDefinition> tools,
            int maxTokens);
    }
}
