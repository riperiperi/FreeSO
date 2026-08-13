using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSO.HouseGen
{
    /// <summary>
    /// Floor-plan image → HouseLayout JSON. Vision only; BlueprintWriter stays deterministic.
    ///
    /// One Anthropic Messages call with the image. The model must emit a HouseLayout object and
    /// nothing else. We then deserialize and run BlueprintWriter.Write so invalid rooms/doors
    /// fail here, before anyone loads the lot.
    /// </summary>
    public static class FloorPlanVision
    {
        public const string DefaultModel = "claude-opus-5";

        // Kept in sync with RoomLayout.cs / examples/layouts/*.json. Doors and windows are
        // objects on wall tiles: edge "west" or "north" only. 1 tile = 1 metre.
        internal const string SystemPrompt =
            "You convert residential floor-plan images into FreeSO HouseLayout JSON.\n" +
            "\n" +
            "Scale: 1 tile = 1 metre. If the plan is drawn on a metre grid or labels metres, " +
            "count those cells — do not upscale. Lot Size is always 77. Place the house near " +
            "the lot centre (rooms typically start around X/Y 28–40). Origin of a room is its " +
            "lowest (x,y) corner; Width and Height are inclusive extents in tiles. Minimum " +
            "room dimension is 2.\n" +
            "\n" +
            "Topology: copy the plan's shape. If it is an L (one room shorter than its " +
            "neighbour), do not stretch the short room to invent a new shared wall. Rooms must " +
            "not overlap. Only rooms that share a wall in the plan may share a wall in JSON.\n" +
            "\n" +
            "Doors and windows are NOT wall attributes. They are objects on the tile whose LOW " +
            "edge carries the wall: Edge \"west\" cuts/styles the west (TopLeft) segment of (X,Y); " +
            "Edge \"north\" cuts/styles the north (TopRight) segment. For a room's east wall use " +
            "X = room.X + room.Width (tile just outside); for south use Y = room.Y + room.Height. " +
            "Only \"west\" or \"north\" are valid Edge values.\n" +
            "\n" +
            "Doors: place ONE door per doorway marker on the plan, on the exact wall it sits " +
            "on. Interior doors go on the shared wall of the two rooms they connect — never on " +
            "a different wall. Do not invent doors. At least one exterior door. Windows only " +
            "where the plan shows them.\n" +
            "\n" +
            "Default Guid for doors: \"0x23941850\". Default Guid for windows: \"0x44E8992A\". " +
            "Floor pattern: 3. Level: 0 for ground floor.\n" +
            "\n" +
            "Emit ONE JSON object only, no markdown fences, no commentary. Schema:\n" +
            "{\n" +
            "  \"Size\": 77,\n" +
            "  \"Rooms\": [ { \"Name\": string, \"X\": int, \"Y\": int, \"Width\": int, \"Height\": int, \"Floor\": 3, \"Level\": 0 } ],\n" +
            "  \"Doors\": [ { \"X\": int, \"Y\": int, \"Edge\": \"west\"|\"north\", \"Guid\": \"0x23941850\", \"Level\": 0 } ],\n" +
            "  \"Windows\": [ { \"X\": int, \"Y\": int, \"Edge\": \"west\"|\"north\", \"Guid\": \"0x44E8992A\", \"Level\": 0 } ]\n" +
            "}\n" +
            "Omit furniture — architecture only.";

        /// <summary>
        /// Reads image bytes, calls the vision model, returns a layout that already survived
        /// BlueprintWriter validation (architecture-only write — no base lot required).
        /// </summary>
        public static async Task<HouseLayout> FromImageAsync(
            string imagePath,
            string apiKey = null,
            string model = null,
            HttpClient http = null)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                throw new FileNotFoundException("Floor-plan image not found.", imagePath);

            apiKey ??= Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "ANTHROPIC_API_KEY is not set. Export it or put it in .env.local.");

            model ??= Environment.GetEnvironmentVariable("FSO_HOUSEGEN_MODEL")
                   ?? Environment.GetEnvironmentVariable("FSO_AGENT_MODEL")
                   ?? DefaultModel;

            var bytes = await File.ReadAllBytesAsync(imagePath);
            var mediaType = MediaTypeFor(imagePath);
            var b64 = Convert.ToBase64String(bytes);

            var ownHttp = http == null;
            http ??= new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
            try
            {
                var body = BuildRequestJson(model, mediaType, b64);
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                req.Headers.Add("x-api-key", apiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                req.Content = new StringContent(body, Encoding.UTF8, "application/json");

                using var resp = await http.SendAsync(req);
                var respText = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    throw new InvalidOperationException(
                        $"Anthropic vision call failed ({(int)resp.StatusCode}): {Truncate(respText, 800)}");

                var raw = ExtractAssistantText(respText);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    using var doc = JsonDocument.Parse(respText);
                    var stop = doc.RootElement.TryGetProperty("stop_reason", out var sr)
                        ? sr.GetString() : "unknown";
                    throw new InvalidOperationException(
                        $"Vision returned no text (stop_reason: {stop}). " +
                        (stop == "max_tokens"
                            ? "The whole budget went to thinking — raise max_tokens."
                            : "Check the response."));
                }
                return ParseAndValidate(raw);
            }
            finally
            {
                if (ownHttp) http.Dispose();
            }
        }

        /// <summary>Parse model text into HouseLayout and prove BlueprintWriter accepts it.</summary>
        public static HouseLayout ParseAndValidate(string modelText)
        {
            var json = ExtractJsonObject(modelText);
            HouseLayout layout;
            try
            {
                layout = JsonSerializer.Deserialize<HouseLayout>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true, IncludeFields = true });
            }
            catch (JsonException e)
            {
                throw new InvalidOperationException(
                    "Vision returned JSON that does not match HouseLayout: " + e.Message +
                    "\n---\n" + Truncate(json, 600), e);
            }

            if (layout == null)
                throw new InvalidOperationException("Vision returned null HouseLayout.");

            // Force Size to 77 — models invent other grids; the lot is always this.
            layout.Size = 77;
            layout.Rooms ??= new List<Room>();
            layout.Doors ??= new List<Door>();
            layout.Windows ??= new List<Window>();

            // Defaults the model often omits.
            foreach (var room in layout.Rooms)
            {
                if (room.Floor == 0) room.Floor = 3;
            }
            foreach (var door in layout.Doors)
            {
                if (string.IsNullOrWhiteSpace(door.Guid)) door.Guid = "0x23941850";
                if (string.IsNullOrWhiteSpace(door.Edge)) door.Edge = "west";
            }
            foreach (var window in layout.Windows)
            {
                if (string.IsNullOrWhiteSpace(window.Guid)) window.Guid = "0x44E8992A";
                if (string.IsNullOrWhiteSpace(window.Edge)) window.Edge = "west";
            }

            try
            {
                BlueprintWriter.Write(layout);
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException(
                    "Vision layout failed BlueprintWriter validation: " + e.Message +
                    "\n---\n" + Truncate(json, 600), e);
            }

            return layout;
        }

        public static string ToJson(HouseLayout layout)
        {
            return JsonSerializer.Serialize(layout, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            });
        }

        /// <summary>
        /// Loads KEY=VALUE pairs from a .env.local into the process environment when the key
        /// is not already set. Matches AgentBridge's documented home for API keys.
        /// </summary>
        public static void LoadDotEnv(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim();
                if (val.Length >= 2 &&
                    ((val[0] == '"' && val[^1] == '"') || (val[0] == '\'' && val[^1] == '\'')))
                    val = val.Substring(1, val.Length - 2);
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    Environment.SetEnvironmentVariable(key, val);
            }
        }

        public static string ExtractJsonObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Vision returned empty text.");

            var trimmed = text.Trim();
            // Strip ```json ... ``` if the model ignored the no-fences rule.
            var fence = Regex.Match(trimmed, @"```(?:json)?\s*(\{[\s\S]*\})\s*```", RegexOptions.IgnoreCase);
            if (fence.Success) trimmed = fence.Groups[1].Value.Trim();

            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start < 0 || end <= start)
                throw new InvalidOperationException(
                    "Vision response contained no JSON object.\n---\n" + Truncate(trimmed, 600));

            return trimmed.Substring(start, end - start + 1);
        }

        private static string ExtractAssistantText(string responseJson)
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (!doc.RootElement.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Anthropic response missing content[].");

            var sb = new StringBuilder();
            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) &&
                    type.GetString() == "text" &&
                    block.TryGetProperty("text", out var text))
                    sb.Append(text.GetString());
            }
            return sb.ToString();
        }

        private static string BuildRequestJson(string model, string mediaType, string b64)
        {
            // Manual JSON so we never pull the Anthropic SDK into HouseGen — vision is one
            // shot, not a tool loop.
            using var stream = new MemoryStream();
            using (var w = new Utf8JsonWriter(stream))
            {
                w.WriteStartObject();
                // Opus 5 thinks by default and max_tokens caps thinking + text together.
                // 4096 was consumed entirely by thinking on a real photo (stop_reason
                // max_tokens, zero text blocks) — the layout JSON itself is small.
                w.WriteString("model", model);
                w.WriteNumber("max_tokens", 16000);
                w.WriteStartArray("system");
                w.WriteStartObject();
                w.WriteString("type", "text");
                w.WriteString("text", SystemPrompt);
                w.WriteEndObject();
                w.WriteEndArray();
                w.WriteStartArray("messages");
                w.WriteStartObject();
                w.WriteString("role", "user");
                w.WriteStartArray("content");
                w.WriteStartObject();
                w.WriteString("type", "image");
                w.WriteStartObject("source");
                w.WriteString("type", "base64");
                w.WriteString("media_type", mediaType);
                w.WriteString("data", b64);
                w.WriteEndObject();
                w.WriteEndObject();
                w.WriteStartObject();
                w.WriteString("type", "text");
                w.WriteString("text",
                    "Read this floor plan and emit the HouseLayout JSON for it. Architecture only. " +
                    "Match room sizes and door walls to the drawing; do not invent adjacencies.");
                w.WriteEndObject();
                w.WriteEndArray();
                w.WriteEndObject();
                w.WriteEndArray();
                w.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static string MediaTypeFor(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => throw new ArgumentException(
                    $"Unsupported image type \"{ext}\". Use png, jpeg, gif, or webp."),
            };
        }

        private static string Truncate(string s, int n) =>
            s == null ? "" : s.Length <= n ? s : s.Substring(0, n) + "…";
    }
}
