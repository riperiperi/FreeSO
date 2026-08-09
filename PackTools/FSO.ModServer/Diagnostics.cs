using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FSO.PackCompiler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FSO.ModServer
{
    /// <summary>
    /// Machine-readable error entry, per MCP-DESIGN.md §2. Property names are snake_case
    /// on the wire (object_id, tree_name, ...) to match that doc's envelope exactly, even
    /// though the runtime tool-result serializer (System.Text.Json) and the diagnostics
    /// mapper's own tests (Newtonsoft, via JObject.FromObject) default to different casing —
    /// both attribute sets are needed so the two serializers agree.
    /// </summary>
    public class ToolError
    {
        // Properties, not fields: System.Text.Json (the SDK's actual return-value
        // serializer) ignores public fields by default and would silently emit "{}".
        [JsonPropertyName("code")] [JsonProperty("code")]
        public string Code { get; set; } = "compile_error";

        [JsonPropertyName("object_id")] [JsonProperty("object_id")]
        public string ObjectId { get; set; }

        [JsonPropertyName("tree_name")] [JsonProperty("tree_name")]
        public string TreeName { get; set; }

        [JsonPropertyName("node_id")] [JsonProperty("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("field")] [JsonProperty("field")]
        public string Field { get; set; }

        [JsonPropertyName("message")] [JsonProperty("message")]
        public string Message { get; set; }

        [JsonPropertyName("expected")] [JsonProperty("expected")]
        public List<string> Expected { get; set; }

        [JsonPropertyName("known_node_ids")] [JsonProperty("known_node_ids")]
        public List<string> KnownNodeIds { get; set; }
    }

    public class ToolResult
    {
        [JsonPropertyName("ok")] [JsonProperty("ok")]
        public bool Ok { get; set; } = true;

        [JsonPropertyName("errors")] [JsonProperty("errors")]
        public List<ToolError> Errors { get; set; } = new List<ToolError>();

        [JsonPropertyName("warnings")] [JsonProperty("warnings")]
        public List<ToolError> Warnings { get; set; } = new List<ToolError>();
    }

    /// <summary>
    /// Turns FSO.PackCompiler's flat "$.json.path: message" diagnostic strings into the
    /// scoped, coded envelope MCP-DESIGN.md §2 specifies, by re-resolving the path against
    /// the session's own working JObject (same object/tree/node indices the compiler saw,
    /// since we serialize that exact JObject to the temp file it just validated).
    /// </summary>
    public static class DiagnosticMapper
    {
        // $.objects[N](.trees.NAME(.nodes[N])?)?...
        private static readonly Regex PathRe = new Regex(
            @"^\$\.objects\[(?<oi>\d+)\](\.trees\.(?<tree>[^.\[]+))?(\.nodes\[(?<ni>\d+)\])?(\.(?<field>[^.\[]+))?",
            RegexOptions.Compiled);

        public static ToolResult Map(Diagnostics d, JObject pack)
        {
            var result = new ToolResult();
            foreach (var raw in d.Errors) result.Errors.Add(MapOne(raw, pack));
            foreach (var raw in d.Warnings) result.Warnings.Add(MapOne(raw, pack));
            result.Ok = result.Errors.Count == 0;
            return result;
        }

        private static ToolError MapOne(string raw, JObject pack)
        {
            var colon = raw.IndexOf(": ");
            var path = colon >= 0 ? raw.Substring(0, colon) : raw;
            var message = colon >= 0 ? raw.Substring(colon + 2) : raw;

            var e = new ToolError { Message = message };
            var m = PathRe.Match(path);
            if (m.Success)
            {
                if (m.Groups["oi"].Success)
                {
                    var oi = int.Parse(m.Groups["oi"].Value);
                    var objects = pack["objects"] as JArray;
                    if (objects != null && oi < objects.Count)
                        e.ObjectId = (string)objects[oi]["id"];
                }
                if (m.Groups["tree"].Success) e.TreeName = m.Groups["tree"].Value;
                if (m.Groups["field"].Success) e.Field = m.Groups["field"].Value;

                if (m.Groups["ni"].Success && e.ObjectId != null && e.TreeName != null)
                {
                    var ni = int.Parse(m.Groups["ni"].Value);
                    var objects = pack["objects"] as JArray;
                    var obj = FindById(objects, e.ObjectId);
                    var nodes = obj?["trees"]?[e.TreeName]?["nodes"] as JArray;
                    if (nodes != null)
                    {
                        if (ni < nodes.Count) e.NodeId = (string)nodes[ni]["id"];
                        e.KnownNodeIds = new List<string>();
                        foreach (var n in nodes) e.KnownNodeIds.Add((string)n["id"]);
                    }
                }
            }

            e.Code = ClassifyCode(message);
            return e;
        }

        private static JToken FindById(JArray arr, string id)
        {
            if (arr == null) return null;
            foreach (var t in arr)
                if ((string)t["id"] == id) return t;
            return null;
        }

        private static string ClassifyCode(string message)
        {
            if (message.Contains("unknown field")) return "unknown_field";
            if (message.Contains("unresolved label")) return "unresolved_label";
            if (message.Contains("unresolved attribute")) return "unresolved_attribute";
            if (message.Contains("unresolved tree name")) return "unresolved_tree_name";
            if (message.Contains("unknown scope")) return "unknown_scope";
            if (message.Contains("unknown primitive")) return "unknown_primitive";
            if (message.Contains("unknown motive")) return "unknown_motive";
            if (message.Contains("unknown category")) return "unknown_category";
            if (message.Contains("unknown attenuation")) return "invalid_enum_value";
            if (message.Contains("GUID collision")) return "guid_collision";
            if (message.Contains("duplicate node id")) return "duplicate_node_id";
            if (message.Contains("duplicate interaction name")) return "duplicate_interaction_name";
            if (message.Contains("duplicate attribute")) return "duplicate_attribute";
            if (message.Contains("duplicate name")) return "duplicate_name";
            if (message.Contains("max is 253")) return "tree_too_large";
            if (message.Contains("too many locals")) return "locals_overflow";
            if (message.Contains("too many args")) return "args_overflow";
            if (message.Contains("missing required field")) return "missing_required_field";
            if (message.Contains("invalid JSON")) return "invalid_json";
            if (message.Contains("unsupported schema")) return "unsupported_schema";
            if (message.Contains("unsupported engine")) return "unsupported_engine";
            if (message.Contains("unwritable scope")) return "unwritable_scope";
            if (message.Contains("expected a string") || message.Contains("expected an integer") || message.Contains("expected a name or number"))
                return "invalid_type";
            return "compile_error";
        }
    }
}
