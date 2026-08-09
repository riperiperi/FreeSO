using System.IO;
using System.Linq;
using System.ComponentModel;
using FSO.Files.FAR1;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;

namespace FSO.ModServer
{
    /// <summary>
    /// Tool surface per PackTools/MCP-DESIGN.md §1. Fine-grained editing tools over a
    /// server-held pack_session_id, not one "write the whole pack" tool — see that doc's
    /// §1 for why. Static methods + a static SessionStore (design's §5 "simple
    /// ConcurrentDictionary in the server process") so xunit can call these directly
    /// without a running MCP host.
    /// </summary>
    [McpServerToolType]
    public static class PackToolHandlers
    {
        [McpServerTool(Name = "create_pack"), Description("Start a new pack authoring session. Returns pack_session_id.")]
        public static object CreatePack(
            [Description("Object/pack identifier, e.g. \"gossip-gnome\"")] string id,
            [Description("Display name")] string name,
            [Description("Author")] string author = "",
            [Description("Semver version, e.g. \"1.0.0\"")] string version = "",
            [Description("Description")] string description = "")
        {
            var pack = new JObject
            {
                ["schema"] = "fso-pack/0.1",
                ["engine"] = "tso",
                ["pack"] = new JObject
                {
                    ["id"] = id,
                    ["name"] = name,
                    ["author"] = author,
                    ["version"] = version,
                    ["description"] = description,
                },
                ["objects"] = new JArray(),
            };
            var session = SessionStore.Create(pack);
            return new { ok = true, pack_session_id = session.Id };
        }

        [McpServerTool(Name = "add_object"), Description("Add an object stub to a pack session. Re-adding an object that already has trees, interactions or dialog is rejected — fix individual pieces with add_tree/edit_tree_node/add_interaction/set_dialog_string instead. Pass replace=true only to deliberately start an object over.")]
        public static object AddObject(
            [Description("pack_session_id from create_pack")] string packSessionId,
            [Description("Object id, unique within the pack")] string id,
            [Description("Display name")] string name,
            [Description("Price in simoleons, 0-65535")] int price = 0,
            [Description("Catalog category (see list_vocabulary \"categories\")")] string category = "",
            [Description("Hex GUID e.g. \"0x6B4F0001\"; omit to auto-allocate from the placeholder community range")] string guid = "",
            [Description("Hex GUID of a base-game object to clone sprites from")] string cloneFromGuid = "",
            [Description("JSON string array of per-instance attribute names, e.g. [\"times_gossiped\"]")] string attributesJson = "",
            [Description("Tree name to use as the object's main loop entry point")] string entryMain = "",
            [Description("Tree name to use as the object's init entry point")] string entryInit = "",
            [Description("Discard and rebuild an object that already has trees/interactions/dialog.")] bool replace = false)
        {
            if (!SessionStore.TryGet(packSessionId, out var session)) return Errors.UnknownSession(packSessionId);

            lock (session.Lock)
            {
                string guidHex;
                if (!string.IsNullOrEmpty(guid))
                {
                    guidHex = guid;
                }
                else
                {
                    var packId = (string)session.Pack["pack"]["id"];
                    var allocated = PackCompilerApi.AllocateGuid(packId, id);
                    guidHex = "0x" + allocated.ToString("X8");
                }

                var obj = new JObject
                {
                    ["id"] = id,
                    ["guid"] = guidHex,
                    ["name"] = name,
                    ["price"] = price,
                    ["category"] = category,
                    ["attributes"] = ParseJsonOrEmpty(attributesJson, new JArray()),
                    ["strings"] = new JObject { ["dialog"] = new JObject() },
                    ["interactions"] = new JArray(),
                    ["trees"] = new JObject(),
                };
                if (!string.IsNullOrEmpty(cloneFromGuid))
                    obj["appearance"] = new JObject { ["clone_from_guid"] = cloneFromGuid };
                if (!string.IsNullOrEmpty(entryMain) || !string.IsNullOrEmpty(entryInit))
                {
                    var entry = new JObject();
                    if (!string.IsNullOrEmpty(entryMain)) entry["main"] = entryMain;
                    if (!string.IsNullOrEmpty(entryInit)) entry["init"] = entryInit;
                    obj["entry_points"] = entry;
                }

                var objects = (JArray)session.Pack["objects"];
                var existingIndex = IndexOfObject(objects, id);

                if (existingIndex >= 0)
                {
                    // Re-adding used to silently discard the object's trees, interactions and
                    // dialog. A model that hit a small error reached for this as its only
                    // recovery, destroying a whole build to fix one duplicate name. Refuse
                    // when there is real work to lose, and say what to use instead.
                    var prev = (JObject)objects[existingIndex];
                    var treeCount = ((JObject)prev["trees"])?.Count ?? 0;
                    var interactionCount = ((JArray)prev["interactions"])?.Count ?? 0;
                    var dialogCount = ((JObject)prev["strings"]?["dialog"])?.Count ?? 0;

                    if (!replace && treeCount + interactionCount + dialogCount > 0)
                        return Errors.Make("object_already_built", id, null, null, "id",
                            $"\"{id}\" already has {treeCount} tree(s), {interactionCount} interaction(s) and {dialogCount} dialog string(s). Re-adding it would discard all of that. To change one thing, use add_tree/edit_tree_node, add_interaction (replace=true), or set_dialog_string. Pass replace=true only if you really mean to start this object over.");

                    objects[existingIndex] = obj;
                    return new { ok = true, object_id = id, guid = guidHex, replaced = true };
                }

                objects.Add(obj);
                return new { ok = true, object_id = id, guid = guidHex, replaced = false };
            }
        }

        [McpServerTool(Name = "add_interaction"), Description("Add one interaction (TTAB/TTAs entry) to an object. interactionJson matches SCHEMA.md's interaction shape, e.g. {\"name\":\"Gossip\",\"action\":\"gossip_action\",\"test\":\"gossip_test\",\"allow\":{\"visitors\":true}}. Adding a name that already exists is rejected; pass replace=true to overwrite that one interaction.")]
        public static object AddInteraction(string packSessionId, string objectId, string interactionJson,
            [Description("Overwrite an existing interaction with the same name instead of failing.")] bool replace = false)
        {
            if (!SessionStore.TryGet(packSessionId, out var session)) return Errors.UnknownSession(packSessionId);
            lock (session.Lock)
            {
                var obj = FindObject(session.Pack, objectId);
                if (obj == null) return Errors.UnknownObject(objectId);

                JObject interaction;
                try { interaction = JObject.Parse(interactionJson); }
                catch (System.Exception e) { return Errors.InvalidJson("interactionJson", e.Message); }

                if (interaction["name"] == null) return Errors.MissingField(objectId, null, null, "name", "interaction requires a \"name\" field");

                // Appending blindly let a duplicate sit undetected until validate, at which
                // point the only recovery tool was add_object — which wipes the whole object.
                // Reject here, where the fix is one call and nothing else is lost.
                var name = (string)interaction["name"];
                var existing = (JArray)obj["interactions"];
                for (int i = 0; i < existing.Count; i++)
                {
                    if ((string)existing[i]["name"] != name) continue;
                    if (!replace)
                        return Errors.Make("duplicate_interaction_name", objectId, null, null, "name",
                            $"\"{objectId}\" already has an interaction named \"{name}\". Pass replace=true to overwrite just this interaction — do not re-add the object, which discards its trees and dialog.");
                    existing[i] = interaction;
                    return new { ok = true, object_id = objectId, interaction = name, replaced = true };
                }

                existing.Add(interaction);
                return new { ok = true, object_id = objectId, interaction = name, replaced = false };
            }
        }

        [McpServerTool(Name = "set_dialog_string"), Description("Set one entry in an object's private dialog string table (compiles to STR# 301). index is 1-255, referenced by dialog_private nodes' \"message\" field as an integer.")]
        public static object SetDialogString(
            [Description("pack_session_id from create_pack")] string packSessionId,
            [Description("Object id, must already exist via add_object")] string objectId,
            [Description("String table index, 1-255")] int index,
            [Description("The dialog text")] string text)
        {
            if (!SessionStore.TryGet(packSessionId, out var session)) return Errors.UnknownSession(packSessionId);
            lock (session.Lock)
            {
                var obj = FindObject(session.Pack, objectId);
                if (obj == null) return Errors.UnknownObject(objectId);

                if (index < 1 || index > 255)
                    return Errors.Make("invalid_field_value", objectId, null, null, "index", "dialog string index must be 1-255 (0 = none)");

                var strings = (JObject)obj["strings"];
                var dialog = (JObject)strings["dialog"];
                dialog[index.ToString()] = text;

                return new { ok = true, object_id = objectId, index, text };
            }
        }

        [McpServerTool(Name = "add_tree"), Description("Declare a named tree on an object. Pass nodesJson to supply the whole tree in one call — strongly preferred over adding nodes one at a time, which costs a call per node and is the main reason builds run out of turns. Use edit_tree_node afterwards only to fix individual nodes. Fails if the tree already exists.")]
        public static object AddTree(
            string packSessionId, string objectId, string treeName,
            [Description("JSON string array of arg names")] string argsJson = "",
            [Description("JSON string array of local names")] string localsJson = "",
            [Description("JSON array of SCHEMA.md node objects — the entire tree body, e.g. [{\"id\":\"idle\",\"prim\":\"idle_for_input\",\"ticks_param\":0,\"allow_push\":true,\"then\":\"idle\",\"else\":\"idle\"}]")] string nodesJson = "")
        {
            if (!SessionStore.TryGet(packSessionId, out var session)) return Errors.UnknownSession(packSessionId);
            lock (session.Lock)
            {
                var obj = FindObject(session.Pack, objectId);
                if (obj == null) return Errors.UnknownObject(objectId);

                var trees = (JObject)obj["trees"];
                if (trees[treeName] != null)
                    return Errors.Make("duplicate_tree", objectId, treeName, null, null,
                        $"tree \"{treeName}\" already exists on \"{objectId}\" — use edit_tree_node/remove_tree_node to modify it");

                JArray nodes;
                if (string.IsNullOrEmpty(nodesJson))
                {
                    nodes = new JArray();
                }
                else
                {
                    try { nodes = JArray.Parse(nodesJson); }
                    catch (System.Exception e) { return Errors.InvalidJson("nodesJson", e.Message); }

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        if (nodes[i].Type != JTokenType.Object)
                            return Errors.Make("invalid_field_value", objectId, treeName, null, "nodesJson",
                                $"nodesJson[{i}] is not an object — nodesJson is an array of SCHEMA.md node objects");
                        if (string.IsNullOrEmpty((string)nodes[i]["id"]))
                            return Errors.MissingField(objectId, treeName, null, "id", $"nodesJson[{i}] requires an \"id\" field");
                    }
                }

                trees[treeName] = new JObject
                {
                    ["args"] = ParseJsonOrEmpty(argsJson, new JArray()),
                    ["locals"] = ParseJsonOrEmpty(localsJson, new JArray()),
                    ["nodes"] = nodes,
                };
                return new { ok = true, object_id = objectId, tree_name = treeName, node_count = nodes.Count };
            }
        }

        [McpServerTool(Name = "edit_tree_node"), Description("Add or replace one node (by \"id\") in a declared tree. nodeJson is one SCHEMA.md node object, e.g. {\"id\":\"reward\",\"prim\":\"expression\",\"lhs\":{...},\"op\":\"+=\",\"rhs\":{...},\"then\":\"count_it\",\"else\":\"error\"}.")]
        public static object EditTreeNode(string packSessionId, string objectId, string treeName, string nodeJson)
        {
            if (!SessionStore.TryGet(packSessionId, out var session)) return Errors.UnknownSession(packSessionId);
            lock (session.Lock)
            {
                var obj = FindObject(session.Pack, objectId);
                if (obj == null) return Errors.UnknownObject(objectId);
                var tree = (JObject)obj["trees"]?[treeName];
                if (tree == null) return Errors.UnknownTree(objectId, treeName);

                JObject node;
                try { node = JObject.Parse(nodeJson); }
                catch (System.Exception e) { return Errors.InvalidJson("nodeJson", e.Message); }

                var nodeId = (string)node["id"];
                if (string.IsNullOrEmpty(nodeId))
                    return Errors.MissingField(objectId, treeName, null, "id", "node requires an \"id\" field");

                var nodes = (JArray)tree["nodes"];
                var idx = -1;
                for (int i = 0; i < nodes.Count; i++)
                    if ((string)nodes[i]["id"] == nodeId) { idx = i; break; }

                if (idx >= 0) nodes[idx] = node; else nodes.Add(node);
                return new { ok = true, object_id = objectId, tree_name = treeName, node_id = nodeId, replaced = idx >= 0 };
            }
        }

        [McpServerTool(Name = "remove_tree_node"), Description("Delete a node from a tree by id. Warns (does not error) if another node's then/else still points at it.")]
        public static object RemoveTreeNode(string packSessionId, string objectId, string treeName, string nodeId)
        {
            if (!SessionStore.TryGet(packSessionId, out var session)) return Errors.UnknownSession(packSessionId);
            lock (session.Lock)
            {
                var obj = FindObject(session.Pack, objectId);
                if (obj == null) return Errors.UnknownObject(objectId);
                var tree = (JObject)obj["trees"]?[treeName];
                if (tree == null) return Errors.UnknownTree(objectId, treeName);

                var nodes = (JArray)tree["nodes"];
                var idx = -1;
                for (int i = 0; i < nodes.Count; i++)
                    if ((string)nodes[i]["id"] == nodeId) { idx = i; break; }
                if (idx < 0) return Errors.Make("unknown_node", objectId, treeName, nodeId, null, $"no node \"{nodeId}\" in tree \"{treeName}\"");

                nodes.RemoveAt(idx);

                var result = new ToolResult { Ok = true };
                foreach (var n in nodes)
                {
                    var then = (string)n["then"];
                    var els = (string)n["else"];
                    if (then == nodeId || els == nodeId)
                    {
                        result.Warnings.Add(new ToolError
                        {
                            Code = "dangling_reference",
                            ObjectId = objectId,
                            TreeName = treeName,
                            NodeId = (string)n["id"],
                            Message = $"node \"{n["id"]}\" still points at removed node \"{nodeId}\"",
                        });
                    }
                }
                return result;
            }
        }

        [McpServerTool(Name = "read_pack"), Description("Return the current working pack JSON for a session.")]
        public static object ReadPack(string packSessionId)
        {
            if (!SessionStore.TryGet(packSessionId, out var session)) return Errors.UnknownSession(packSessionId);
            lock (session.Lock)
            {
                return new { ok = true, pack_session_id = packSessionId, pack = ToPlain(session.Pack) };
            }
        }

        [McpServerTool(Name = "validate"), Description("Static checks without emitting .iff — tree size, locals, label resolution, enum values, GUID collisions. Cheap, side-effect-free.")]
        public static object Validate(string packSessionId)
        {
            if (!SessionStore.TryGet(packSessionId, out var session)) return Errors.UnknownSession(packSessionId);
            lock (session.Lock)
            {
                var path = WriteTemp(session);
                var compileResult = PackCompilerApi.Validate(path);
                return DiagnosticMapper.Map(compileResult.Diagnostics, session.Pack);
            }
        }

        [McpServerTool(Name = "compile"), Description("Emit one .iff per object plus a build report (tree ids, GUIDs, warnings). Refuses to emit if there are any errors.")]
        public static object Compile(string packSessionId)
        {
            if (!SessionStore.TryGet(packSessionId, out var session)) return Errors.UnknownSession(packSessionId);
            lock (session.Lock)
            {
                var path = WriteTemp(session);
                var outDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, "out");
                // Pass the game dir when we have one so appearance.clone_from_guid copies real
                // sprites — without it the object compiles fine but renders invisible in the client.
                var compileResult = PackCompilerApi.Build(path, outDir, GameContent.ResolveDir());
                var envelope = DiagnosticMapper.Map(compileResult.Diagnostics, session.Pack);
                if (!envelope.Ok) return envelope;

                return new
                {
                    ok = true,
                    errors = envelope.Errors,
                    warnings = envelope.Warnings,
                    out_dir = outDir,
                    // BuildReport/ObjectReport (FSO.PackCompiler) expose public fields, which
                    // System.Text.Json ignores by default — round-trip through Newtonsoft
                    // (which does serialize fields) into the plain-object form first.
                    report = ToPlain(JObject.FromObject(compileResult.Report)),
                };
            }
        }

        [McpServerTool(Name = "list_vocabulary"), Description("Return a primitive/scope/operator/motive/etc enum table as structured data, so an agent can self-serve valid names instead of guessing. kind: one of primitives, scopes, motives, operators, categories, goto_relative_locations, goto_relative_directions, animation_sources, slot_scopes, push_priorities, dialog_types, dialog_icons, create_object_positions, suit_scopes, balloon_groups.")]
        public static object ListVocabulary(string kind)
        {
            var data = Vocabulary.Get(kind);
            if (data == null)
                return Errors.Make("unknown_vocabulary_kind", null, null, null, "kind",
                    $"unknown kind \"{kind}\"", expected: new System.Collections.Generic.List<string>(Vocabulary.Kinds));
            return new { ok = true, kind, data };
        }

        [McpServerTool(Name = "find_base_object"), Description("Search the base game's objects by name and return real GUIDs usable as appearance.clone_from_guid. Use this before setting clone_from_guid — never guess a GUID, and never omit appearance because no GUID was known: an object without appearance compiles successfully and is invisible in game. Query is plain words, e.g. \"garden gnome\", \"floor lamp\", \"dining chair\".")]
        public static object FindBaseObject(
            [Description("Plain-word search, e.g. \"garden gnome\". All words must match.")] string query,
            [Description("Max candidates to return (default 8).")] int limit = 8,
            [Description("Override the TSO content directory. Defaults to the installed game.")] string gameLocation = "")
        {
            if (string.IsNullOrWhiteSpace(query))
                return Errors.Make("invalid_query", null, null, null, "query", "query is empty — pass words to search for, e.g. \"garden gnome\"");

            var gameDir = GameContent.ResolveDir(string.IsNullOrEmpty(gameLocation) ? null : gameLocation);
            if (gameDir == null)
                return Errors.Make("game_content_missing", null, null, null, "game_location",
                    $"no TSO content directory found (looked in \"{GameContent.DefaultPathForMessage()}\") — set FSO_VM_GAME_LOCATION or pass game_location");

            var tablePath = BaseObjectIndex.TablePath(gameDir);
            if (!System.IO.File.Exists(tablePath))
                return Errors.Make("game_content_missing", null, null, null, "game_location",
                    $"object table not found at \"{tablePath}\" — is \"{gameDir}\" a TSO content directory?");

            var boundedLimit = limit < 1 ? 1 : limit;
            // Search wider than requested, because the unclonable-candidate filter below
            // removes some hits — asking Search for exactly `limit` would silently return
            // fewer than the caller wanted whenever a filtered hit was in that top slice.
            var rawHits = BaseObjectIndex.Search(BaseObjectIndex.Load(gameDir), query, boundedLimit * 4);

            var objectsDir = FindObjectsDir(gameDir);
            var hits = new System.Collections.Generic.List<BaseObjectIndex.Entry>();
            var droppedUnclonable = 0;
            foreach (var h in rawHits)
            {
                if (hits.Count >= boundedLimit) break;
                // A hit here only proves the object is in the base game's index, not that it's
                // clonable: some entries' .iff has BaseGraphicID == 0 (no draw group), so
                // clone_from_guid would silently produce another invisible object — exactly the
                // failure this tool exists to prevent. Drop those rather than return them.
                if (objectsDir != null && !HasDrawableAppearance(objectsDir, h.File))
                {
                    droppedUnclonable++;
                    continue;
                }
                hits.Add(h);
            }

            if (hits.Count == 0)
            {
                // A miss must not leave "skip appearance" as the easy way out — that path
                // produces an object that compiles clean and renders as nothing.
                var reason = droppedUnclonable > 0
                    ? $"\"{query}\" matched {droppedUnclonable} base-game object(s), but every match has no drawable appearance (BaseGraphicID == 0) and would clone into another invisible object."
                    : $"nothing in the base game matched \"{query}\".";
                return Errors.Make("no_base_object_match", null, null, null, "query",
                    $"{reason} Retry with a broader or more generic word (\"gnome\" rather than \"garden gnome statue\", \"lamp\" rather than \"art deco floor lamp\"), or search for whatever common object is the closest shape. If nothing matches, use appearance.generated instead — do NOT leave the object without appearance, which compiles successfully and renders invisible.");
            }

            return new
            {
                ok = true,
                query,
                candidates = hits.Select(h => new
                {
                    guid = "0x" + h.Guid.ToString("X8"),
                    name = h.Name,
                    file = h.File,
                }).ToList(),
                usage = "Set appearance: {\"clone_from_guid\": \"<guid>\"} on the object to reuse that object's sprites.",
            };
        }

        // ---- appearance-clonability check (BaseGraphicID == 0 guard) ------------------
        //
        // Duplicates AppearanceCloner's FAR-reading path rather than calling into it,
        // deliberately: FSO.PackCompiler is out of scope for this change (owned elsewhere),
        // and this check only needs OBJD.BaseGraphicID, not a full clone.

        private static string FindObjectsDir(string gameDir)
        {
            foreach (var dir in Directory.GetDirectories(gameDir))
            {
                if (!string.Equals(Path.GetFileName(dir), "objectdata", System.StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var sub in Directory.GetDirectories(dir))
                    if (string.Equals(Path.GetFileName(sub), "objects", System.StringComparison.OrdinalIgnoreCase)) return sub;
            }
            return null;
        }

        private static bool HasDrawableAppearance(string objectsDir, string fileName)
        {
            var farPath = Path.Combine(objectsDir, "objiff.far");
            if (!File.Exists(farPath)) return true; // can't check — don't block on a missing archive

            var archive = new FAR1Archive(farPath, true); // v1b: true for TSO archives
            try
            {
                var entry = archive.GetAllFarEntries()
                    .FirstOrDefault(e => string.Equals(e.Filename, fileName + ".iff", System.StringComparison.OrdinalIgnoreCase));
                if (entry == null) return true; // can't check — don't block on a lookup miss

                var bytes = archive.GetEntry(entry);
                var iff = new IffFile();
                using (var stream = new MemoryStream(bytes)) iff.Read(stream);
                var objd = iff.List<OBJD>()?.FirstOrDefault();
                return objd != null && objd.BaseGraphicID != 0;
            }
            finally
            {
                archive.Close();
            }
        }

        [McpServerTool(Name = "test_in_vm"), Description("Compile the session's pack and run one object in FSO.VMHarness: place it, push an interaction, tick, and report a trace plus final state. scenarioJson matches MCP-DESIGN.md §3, e.g. {\"place_object\":\"gossip_gnome\",\"push_interaction\":\"Gossip\",\"max_ticks\":200,\"assertions\":[{\"type\":\"motive_at_least\",\"target\":\"sim\",\"motive\":\"social\",\"value\":10}]}. Requires a game content checkout on disk (see game_location) and FSO.VMHarness already built.")]
        public static object TestInVm(string packSessionId, string scenarioJson = "")
        {
            if (!SessionStore.TryGet(packSessionId, out var session)) return Errors.UnknownSession(packSessionId);
            lock (session.Lock)
            {
                return VmHarnessRunner.Run(session, scenarioJson);
            }
        }

        [McpServerTool(Name = "decompile_object"), Description("Turn an existing compiler-emitted .iff into pack JSON, for remixing objects made with this toolchain. Takes a filesystem path, not a base-game GUID.")]
        public static object DecompileObject(string guidOrPath)
        {
            var outDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fso-modserver-decompile");
            System.IO.Directory.CreateDirectory(outDir);
            var outPath = System.IO.Path.Combine(outDir, System.IO.Path.GetFileNameWithoutExtension(guidOrPath) + ".json");

            var result = PackCompilerApi.Decompile(guidOrPath, outPath);
            var envelope = DiagnosticMapper.Map(result.Diagnostics, null);
            if (!envelope.Ok) return envelope;

            var pack = JObject.Parse(System.IO.File.ReadAllText(outPath));
            return new { ok = true, errors = envelope.Errors, warnings = envelope.Warnings, pack = ToPlain(pack) };
        }

        // ---- helpers ----

        private static JObject FindObject(JObject pack, string objectId)
        {
            var objects = (JArray)pack["objects"];
            var idx = IndexOfObject(objects, objectId);
            return idx >= 0 ? (JObject)objects[idx] : null;
        }

        private static int IndexOfObject(JArray objects, string id)
        {
            for (int i = 0; i < objects.Count; i++)
                if ((string)objects[i]["id"] == id) return i;
            return -1;
        }

        private static JToken ParseJsonOrEmpty(string json, JToken fallback)
        {
            if (string.IsNullOrWhiteSpace(json)) return fallback;
            return JToken.Parse(json);
        }

        internal static string WriteTemp(PackSession session)
        {
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fso-modserver", session.Id);
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, "pack.json");
            System.IO.File.WriteAllText(path, session.Pack.ToString());
            return path;
        }

        // Newtonsoft's JObject can't be handed straight to a caller that then serializes it
        // with System.Text.Json (the MCP SDK's return-value serializer) — it'd serialize
        // JObject's own CLR shape, not the JSON it represents. Converting to a plain
        // Dictionary/List/primitive graph makes the result serializer-agnostic.
        internal static object ToPlain(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var dict = new System.Collections.Generic.Dictionary<string, object>();
                    foreach (var prop in ((JObject)token).Properties()) dict[prop.Name] = ToPlain(prop.Value);
                    return dict;
                case JTokenType.Array:
                    var list = new System.Collections.Generic.List<object>();
                    foreach (var item in (JArray)token) list.Add(ToPlain(item));
                    return list;
                case JTokenType.Integer: return token.Value<long>();
                case JTokenType.Float: return token.Value<double>();
                case JTokenType.Boolean: return token.Value<bool>();
                case JTokenType.Null: return null;
                default: return token.ToString();
            }
        }
    }
}
