using System.Collections.Generic;

namespace FSO.ModServer
{
    /// <summary>Client-side (pre-compile) errors — bad session id, unknown object/tree, bad JSON.</summary>
    public static class Errors
    {
        public static ToolResult Make(string code, string objectId, string treeName, string nodeId, string field, string message, List<string> expected = null, List<string> knownNodeIds = null)
        {
            return new ToolResult
            {
                Ok = false,
                Errors = new List<ToolError>
                {
                    new ToolError
                    {
                        Code = code,
                        ObjectId = objectId,
                        TreeName = treeName,
                        NodeId = nodeId,
                        Field = field,
                        Message = message,
                        Expected = expected,
                        KnownNodeIds = knownNodeIds,
                    },
                },
            };
        }

        public static ToolResult UnknownSession(string id) =>
            Make("unknown_session", null, null, null, "pack_session_id", $"no session \"{id}\" (did create_pack run, or did the server restart?)");

        public static ToolResult UnknownObject(string objectId) =>
            Make("unknown_object", objectId, null, null, "object_id", $"no object \"{objectId}\" in this session — call add_object first");

        public static ToolResult UnknownTree(string objectId, string treeName) =>
            Make("unknown_tree", objectId, treeName, null, "tree_name", $"no tree \"{treeName}\" on \"{objectId}\" — call add_tree first");

        public static ToolResult InvalidJson(string field, string detail) =>
            Make("invalid_json", null, null, null, field, $"invalid JSON in {field}: {detail}");

        public static ToolResult MissingField(string objectId, string treeName, string nodeId, string field, string message) =>
            Make("missing_required_field", objectId, treeName, nodeId, field, message);

        public static ToolResult NotImplemented(string tool, string reason) =>
            Make("not_implemented", null, null, null, null, $"{tool}: {reason}");
    }
}
