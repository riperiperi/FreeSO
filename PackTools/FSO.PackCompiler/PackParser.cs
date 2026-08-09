using System.Collections.Generic;
using System.Linq;
using FSO.PackCompiler.ArtGen;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Parses pack JSON (SCHEMA.md v0.1) into the pack model. Strict: unknown fields,
    /// bad types, bad enum names all become errors with their JSON path.
    /// </summary>
    public class PackParser
    {
        public Diagnostics D;

        public PackParser(Diagnostics d)
        {
            D = d;
        }

        public PackFile Parse(string json)
        {
            JObject root;
            try
            {
                root = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            }
            catch (JsonException e)
            {
                D.Error("$", "invalid JSON: " + e.Message);
                return null;
            }

            var pack = new PackFile();
            var o = new JsonObj(root, "$", D);

            pack.Schema = o.ReqString("schema");
            if (pack.Schema != null && pack.Schema != "fso-pack/0.1")
                D.Error("$.schema", "unsupported schema \"" + pack.Schema + "\" (expected \"fso-pack/0.1\")");

            pack.Engine = o.ReqString("engine");
            if (pack.Engine != null && pack.Engine != "tso")
                D.Error("$.engine", "unsupported engine \"" + pack.Engine + "\" (v0.1 supports \"tso\" only)");

            var meta = o.ReqObj("pack");
            if (meta != null)
            {
                pack.Meta.Id = meta.ReqString("id");
                pack.Meta.Name = meta.ReqString("name");
                pack.Meta.Author = meta.OptString("author");
                pack.Meta.Version = meta.OptString("version");
                pack.Meta.Description = meta.OptString("description");
                meta.Done();
            }

            var objects = o.OptArr("objects");
            if (objects == null || objects.Count == 0)
            {
                D.Error("$.objects", "pack must contain at least one object");
            }
            else
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    var obj = ParseObject(JsonObj.From(objects[i], "$.objects[" + i + "]", D));
                    if (obj != null) pack.Objects.Add(obj);
                }
            }

            o.Done();

            // GUID collisions within the pack
            var guids = new Dictionary<uint, string>();
            foreach (var obj in pack.Objects)
            {
                if (guids.TryGetValue(obj.Guid, out var other))
                    D.Error(obj.Path + ".guid", "GUID collision with object \"" + other + "\"");
                else guids[obj.Guid] = obj.Id;
            }

            return pack;
        }

        private PackObject ParseObject(JsonObj o)
        {
            var obj = new PackObject { Path = o.Path };

            obj.Id = o.ReqString("id");
            obj.Name = o.ReqString("name");

            var guid = o.OptGuid("guid");
            if (guid == null && !o.Has("guid")) D.Error(o.Path, "missing required field \"guid\"");
            obj.Guid = guid ?? 0;

            obj.Price = o.OptInt("price");
            if (obj.Price < 0 || obj.Price > ushort.MaxValue)
                D.Error(o.Path + ".price", "price out of range 0-65535");

            obj.Category = o.OptString("category");
            if (obj.Category != null && !Names.Categories.ContainsKey(obj.Category))
                D.Error(o.Path + ".category", "unknown category \"" + obj.Category + "\" (expected one of: " + string.Join(", ", Names.Categories.Keys) + ")");

            var tags = o.OptArr("tags");
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    if (tags[i].Type != JTokenType.String)
                    {
                        D.Error(o.Path + ".tags[" + i + "]", "expected a string");
                        continue;
                    }
                    obj.Tags.Add((string)tags[i]);
                }
            }

            var appearance = o.OptObj("appearance");
            if (appearance != null)
            {
                obj.CloneFromGuid = appearance.OptGuid("clone_from_guid");
                var generated = appearance.OptObj("generated");
                if (generated != null) obj.Generated = ParseGeneratedAppearance(generated);
                appearance.Done();

                if (obj.CloneFromGuid != null && obj.Generated != null)
                    D.Error(appearance.Path, "appearance.clone_from_guid and appearance.generated are mutually exclusive");
            }

            // An object with no appearance at all compiles "successfully" but is invisible in
            // the client with no signal anywhere — the same silent-failure class clone_from_guid
            // without a game dir already guards against (that path at least leaves a build-report
            // note). Fail loud instead, per SCHEMA.md's rationale: the VM doesn't care an object
            // has no graphics, so nothing else will catch this.
            if (obj.CloneFromGuid == null && obj.Generated == null)
                D.Error(o.Path, "object has no appearance (\"appearance.clone_from_guid\" or \"appearance.generated\") — it would be invisible in the client");

            var attrs = o.OptArr("attributes");
            if (attrs != null)
            {
                for (int i = 0; i < attrs.Count; i++)
                {
                    if (attrs[i].Type != JTokenType.String)
                    {
                        D.Error(o.Path + ".attributes[" + i + "]", "expected a string");
                        continue;
                    }
                    var name = (string)attrs[i];
                    if (obj.Attributes.Contains(name))
                        D.Error(o.Path + ".attributes[" + i + "]", "duplicate attribute \"" + name + "\"");
                    else obj.Attributes.Add(name);
                }
                if (obj.Attributes.Count > ushort.MaxValue) D.Error(o.Path + ".attributes", "too many attributes");
            }

            var strings = o.OptObj("strings");
            if (strings != null)
            {
                var dialog = strings.OptObj("dialog");
                if (dialog != null)
                {
                    foreach (var prop in dialog.Properties())
                    {
                        var path = dialog.Path + "." + prop.Name;
                        if (!int.TryParse(prop.Name, out var id) || id < 1 || id > 255)
                        {
                            D.Error(path, "dialog string ids must be integers 1-255 (0 = none)");
                            continue;
                        }
                        if (prop.Value.Type != JTokenType.String)
                        {
                            D.Error(path, "expected a string");
                            continue;
                        }
                        obj.DialogStrings[id] = (string)prop.Value;
                    }
                    dialog.MarkAllUsed();
                    dialog.Done();
                }
                strings.Done();
            }

            var interactions = o.OptArr("interactions");
            if (interactions != null)
            {
                for (int i = 0; i < interactions.Count; i++)
                {
                    var inter = ParseInteraction(JsonObj.From(interactions[i], o.Path + ".interactions[" + i + "]", D));
                    if (inter != null)
                    {
                        if (obj.Interactions.Any(x => x.Name == inter.Name))
                            D.Error(inter.Path + ".name", "duplicate interaction name \"" + inter.Name + "\"");
                        obj.Interactions.Add(inter);
                    }
                }
            }

            var trees = o.OptObj("trees");
            if (trees == null)
            {
                D.Error(o.Path, "missing required field \"trees\"");
            }
            else
            {
                foreach (var prop in trees.Properties())
                {
                    var tree = ParseTree(prop.Name, JsonObj.From(prop.Value, trees.Path + "." + prop.Name, D));
                    obj.Trees.Add(tree);
                }
                trees.MarkAllUsed();
                trees.Done();
            }

            var entry = o.OptObj("entry_points");
            if (entry != null)
            {
                obj.EntryMain = entry.OptString("main");
                obj.EntryInit = entry.OptString("init");
                entry.Done();
            }

            o.Done();

            // referenced tree names must exist
            var treeNames = new HashSet<string>(obj.Trees.Select(t => t.Name));
            if (obj.EntryMain != null && !treeNames.Contains(obj.EntryMain))
                D.Error(o.Path + ".entry_points.main", "unresolved tree name \"" + obj.EntryMain + "\"");
            if (obj.EntryInit != null && !treeNames.Contains(obj.EntryInit))
                D.Error(o.Path + ".entry_points.init", "unresolved tree name \"" + obj.EntryInit + "\"");
            foreach (var inter in obj.Interactions)
            {
                if (inter.ActionTree != null && !treeNames.Contains(inter.ActionTree))
                    D.Error(inter.Path + ".action", "unresolved tree name \"" + inter.ActionTree + "\"");
                if (inter.TestTree != null && !treeNames.Contains(inter.TestTree))
                    D.Error(inter.Path + ".test", "unresolved tree name \"" + inter.TestTree + "\"");
            }

            return obj;
        }

        private static readonly HashSet<string> KnownGenerators = new HashSet<string> { "chair" };

        private PackGeneratedAppearance ParseGeneratedAppearance(JsonObj o)
        {
            var gen = new PackGeneratedAppearance();
            gen.Generator = o.ReqString("generator");
            var p = o.OptObj("params");

            if (gen.Generator != null && !KnownGenerators.Contains(gen.Generator))
            {
                D.Error(o.Path + ".generator", "unknown generator \"" + gen.Generator + "\" (expected one of: " + string.Join(", ", KnownGenerators) + ")");
            }
            else if (gen.Generator == "chair")
            {
                gen.ChairParams = ParseChairParams(p ?? JsonObj.From(new JObject(), o.Path + ".params", D));
            }

            o.Done();
            return gen;
        }

        private ChairGenerator.Params ParseChairParams(JsonObj p)
        {
            var d = new ChairGenerator.Params(); // defaults
            var result = new ChairGenerator.Params
            {
                SeatWidth = p.OptDouble("seat_width", d.SeatWidth),
                SeatDepth = p.OptDouble("seat_depth", d.SeatDepth),
                SeatHeight = p.OptDouble("seat_height", d.SeatHeight),
                SeatThickness = p.OptDouble("seat_thickness", d.SeatThickness),
                BackHeight = p.OptDouble("back_height", d.BackHeight),
                BackThickness = p.OptDouble("back_thickness", d.BackThickness),
                BackAngleDeg = p.OptDouble("back_angle_deg", d.BackAngleDeg),
                LegTopWidth = p.OptDouble("leg_top_width", d.LegTopWidth),
                LegBottomWidth = p.OptDouble("leg_bottom_width", d.LegBottomWidth),
                Arms = p.OptBool("arms", d.Arms),
                ArmHeight = p.OptDouble("arm_height", d.ArmHeight),
                ArmThickness = p.OptDouble("arm_thickness", d.ArmThickness),
                WoodColor = ParseColor(p, "wood_color", d.WoodColor),
                UpholsteryColor = ParseColor(p, "upholstery_color", d.UpholsteryColor),
            };

            // Physically nonsensical geometry (<= 0) produces a degenerate/garbage mesh with
            // no error from the renderer, so the compiler must catch it here instead.
            void Positive(double v, string field)
            {
                if (v <= 0) D.Error(p.Path + "." + field, "must be > 0");
            }
            Positive(result.SeatWidth, "seat_width");
            Positive(result.SeatDepth, "seat_depth");
            Positive(result.SeatHeight, "seat_height");
            Positive(result.SeatThickness, "seat_thickness");
            Positive(result.BackHeight, "back_height");
            Positive(result.BackThickness, "back_thickness");
            Positive(result.LegTopWidth, "leg_top_width");
            Positive(result.LegBottomWidth, "leg_bottom_width");
            if (result.Arms)
            {
                Positive(result.ArmHeight, "arm_height");
                Positive(result.ArmThickness, "arm_thickness");
            }

            p.Done();
            return result;
        }

        private (byte, byte, byte) ParseColor(JsonObj p, string name, (byte, byte, byte) def)
        {
            var arr = p.OptArr(name);
            if (arr == null) return def;
            if (arr.Count != 3)
            {
                D.Error(p.Path + "." + name, "expected an array of 3 integers 0-255 [r, g, b]");
                return def;
            }
            var vals = new byte[3];
            for (int i = 0; i < 3; i++)
            {
                if (arr[i].Type != JTokenType.Integer || (int)arr[i] < 0 || (int)arr[i] > 255)
                {
                    D.Error(p.Path + "." + name + "[" + i + "]", "expected an integer 0-255");
                    continue;
                }
                vals[i] = (byte)(int)arr[i];
            }
            return (vals[0], vals[1], vals[2]);
        }

        private PackInteraction ParseInteraction(JsonObj o)
        {
            var inter = new PackInteraction { Path = o.Path };
            inter.Name = o.ReqString("name");
            inter.ActionTree = o.ReqString("action");
            inter.TestTree = o.OptString("test");

            var allow = o.OptObj("allow");
            if (allow != null)
            {
                inter.HasAllow = true;
                inter.AllowVisitors = allow.OptBool("visitors");
                inter.AllowOwner = allow.OptBool("owner");
                inter.AllowRoommates = allow.OptBool("roommates");
                inter.AllowFriends = allow.OptBool("friends");
                inter.AllowGhosts = allow.OptBool("ghosts");
                inter.AllowCSRs = allow.OptBool("csrs");
                inter.AllowCats = allow.OptBool("cats");
                inter.AllowDogs = allow.OptBool("dogs");
                allow.Done();
            }

            var flags = o.OptObj("flags");
            if (flags != null)
            {
                inter.FlagDebug = flags.OptBool("debug");
                inter.FlagAutoFirst = flags.OptBool("auto_first");
                inter.FlagRunImmediately = flags.OptBool("run_immediately");
                inter.FlagMustRun = flags.OptBool("must_run");
                inter.FlagAllowConsecutive = flags.OptBool("allow_consecutive");
                inter.FlagJoinable = flags.OptBool("joinable");
                inter.FlagLeapfrog = flags.OptBool("leapfrog");
                inter.FlagCarrying = flags.OptBool("carrying");
                inter.FlagRepair = flags.OptBool("repair");
                inter.FlagAlwaysCheck = flags.OptBool("always_check");
                inter.FlagWhenDead = flags.OptBool("when_dead");
                flags.Done();
            }

            var autonomy = o.OptObj("autonomy");
            if (autonomy != null)
            {
                var motives = autonomy.OptObj("advertised_motives");
                if (motives != null)
                {
                    foreach (var prop in motives.Properties())
                    {
                        var path = motives.Path + "." + prop.Name;
                        if (!Names.Motives.TryGetValue(prop.Name, out var motiveIdx))
                        {
                            D.Error(path, "unknown motive \"" + prop.Name + "\" (expected one of: " + Names.List(Names.Motives) + ")");
                            continue;
                        }
                        if (prop.Value.Type != JTokenType.Integer)
                        {
                            D.Error(path, "expected an integer");
                            continue;
                        }
                        var v = (int)prop.Value;
                        if (v < short.MinValue || v > short.MaxValue)
                        {
                            D.Error(path, "value out of int16 range");
                            continue;
                        }
                        inter.AdvertisedMotives[motiveIdx] = (short)v;
                    }
                    motives.MarkAllUsed();
                    motives.Done();
                }

                var threshold = autonomy.OptInt("threshold");
                if (threshold < 0) D.Error(autonomy.Path + ".threshold", "threshold must be >= 0");
                else inter.AutonomyThreshold = (uint)threshold;

                var atten = autonomy.Opt("attenuation");
                if (atten != null)
                {
                    if (atten.Type == JTokenType.String)
                    {
                        var name = (string)atten;
                        var codes = new Dictionary<string, uint> { { "none", 1 }, { "low", 2 }, { "medium", 3 }, { "high", 4 } };
                        if (codes.TryGetValue(name, out var code))
                        {
                            inter.AttenuationCode = code;
                            inter.AttenuationValue = FSO.Files.Formats.IFF.Chunks.TTAB.AttenuationValues[code];
                        }
                        else D.Error(autonomy.Path + ".attenuation", "unknown attenuation \"" + name + "\" (expected none, low, medium, high, or a number)");
                    }
                    else if (atten.Type == JTokenType.Float || atten.Type == JTokenType.Integer)
                    {
                        inter.AttenuationCode = 0; // custom
                        inter.AttenuationValue = (float)atten;
                    }
                    else D.Error(autonomy.Path + ".attenuation", "expected a name or number");
                }

                inter.JoiningIndex = autonomy.OptInt("joining_index");
                autonomy.Done();
            }

            o.Done();
            return inter;
        }

        private PackTree ParseTree(string name, JsonObj o)
        {
            var tree = new PackTree { Name = name, Path = o.Path };

            foreach (var field in new[] { "args", "locals" })
            {
                var arr = o.OptArr(field);
                var list = (field == "args") ? tree.Args : tree.Locals;
                if (arr == null) continue;
                for (int i = 0; i < arr.Count; i++)
                {
                    if (arr[i].Type != JTokenType.String)
                    {
                        D.Error(o.Path + "." + field + "[" + i + "]", "expected a string");
                        continue;
                    }
                    var n = (string)arr[i];
                    if (list.Contains(n)) D.Error(o.Path + "." + field + "[" + i + "]", "duplicate name \"" + n + "\"");
                    else list.Add(n);
                }
            }

            if (tree.Locals.Count > 255)
                D.Error(o.Path + ".locals", "too many locals (" + tree.Locals.Count + " > 255; TSO BHAV stores locals as a byte)");
            if (tree.Args.Count > 255)
                D.Error(o.Path + ".args", "too many args (" + tree.Args.Count + " > 255)");

            var nodes = o.OptArr("nodes");
            if (nodes == null || nodes.Count == 0)
            {
                D.Error(o.Path, "tree must have a non-empty \"nodes\" array");
            }
            else
            {
                if (nodes.Count > 253)
                    D.Error(o.Path + ".nodes", "tree has " + nodes.Count + " nodes; max is 253 (pointers 253-255 are reserved)");
                for (int i = 0; i < nodes.Count; i++)
                {
                    var no = JsonObj.From(nodes[i], o.Path + ".nodes[" + i + "]", D);
                    var node = new PackNode { Path = no.Path, Fields = no };
                    node.Id = no.ReqString("id");
                    if (no.Has("call"))
                    {
                        node.Call = no.OptString("call");
                    }
                    else
                    {
                        node.Prim = no.ReqString("prim");
                    }
                    node.Then = no.ReqString("then");
                    node.Else = no.ReqString("else");
                    if (node.Id != null && tree.Nodes.Any(x => x.Id == node.Id))
                        D.Error(no.Path + ".id", "duplicate node id \"" + node.Id + "\"");
                    tree.Nodes.Add(node);
                    // remaining fields consumed by the operand compiler; Done() called there.
                }
            }

            o.Done();
            return tree;
        }
    }
}
