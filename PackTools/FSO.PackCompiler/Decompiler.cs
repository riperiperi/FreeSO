using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using Newtonsoft.Json.Linq;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Decompiles a compiler-emitted .iff back to pack JSON (SCHEMA.md v0.1).
    /// Inverts the operand serializers in TreeCompiler.cs exactly. Anything the schema
    /// cannot represent (unknown opcodes, unknown flag bits, nonzero operand padding,
    /// global tree calls) is a loud error, never a silent drop.
    /// </summary>
    public class Decompiler
    {
        private readonly Diagnostics D;
        private Dictionary<ushort, string> TreeNames; // BHAV chunk id -> tree name

        public Decompiler(Diagnostics d)
        {
            D = d;
        }

        // reverse name tables; the my_attributes alias is skipped so each byte has one name
        private static readonly Dictionary<byte, string> ScopeNames = Reverse(Names.Scopes, "my_attributes");
        private static readonly Dictionary<byte, string> OperatorNames = Reverse(Names.Operators);
        private static readonly Dictionary<byte, string> MotiveNames = Reverse(Names.Motives);
        private static readonly Dictionary<sbyte, string> LocationNames = Reverse(Names.GotoRelativeLocations);
        private static readonly Dictionary<sbyte, string> DirectionNames = Reverse(Names.GotoRelativeDirections);
        private static readonly Dictionary<byte, string> AnimSourceNames = Reverse(Names.AnimationSources);
        private static readonly Dictionary<ushort, string> SlotScopeNames = Reverse(Names.SlotScopes);
        private static readonly Dictionary<byte, string> PriorityNames = Reverse(Names.PushPriorities);
        private static readonly Dictionary<byte, string> DialogTypeNames = Reverse(Names.DialogTypes);
        private static readonly Dictionary<byte, string> DialogIconNames = Reverse(Names.DialogIcons);
        private static readonly Dictionary<byte, string> PositionNames = Reverse(Names.CreateObjectPositions);
        private static readonly Dictionary<byte, string> SuitScopeNames = Reverse(Names.SuitScopes);
        private static readonly Dictionary<byte, string> BalloonGroupNames = Reverse(Names.BalloonGroups);

        private static Dictionary<TV, TK> Reverse<TK, TV>(Dictionary<TK, TV> dict, params TK[] skip)
        {
            var result = new Dictionary<TV, TK>();
            foreach (var kv in dict)
            {
                if (skip.Contains(kv.Key)) continue;
                if (!result.ContainsKey(kv.Value)) result.Add(kv.Value, kv.Key);
            }
            return result;
        }

        public JObject Decompile(string iffPath)
        {
            var iff = new IffFile(iffPath);
            var objd = iff.List<OBJD>()?.FirstOrDefault();
            if (objd == null)
            {
                D.Error(iffPath, "no OBJD chunk in iff");
                return null;
            }

            var objId = Sanitize(Path.GetFileNameWithoutExtension(iffPath));

            // catalog name: CTSS string 0, falling back to the OBJD chunk label
            var ctss = iff.Get<CTSS>(objd.CatalogStringsID != 0 ? objd.CatalogStringsID : PackBuilder.CTSS_ID);
            var name = ctss?.GetString(0);
            if (string.IsNullOrEmpty(name)) name = objd.ChunkLabel;

            // trees: private BHAVs, in chunk id order (recompile assigns 4096+index in this order)
            var allBhavs = iff.List<BHAV>() ?? new List<BHAV>();
            foreach (var b in allBhavs.Where(x => x.ChunkID < PackBuilder.PRIVATE_TREE_BASE))
                D.Error(iffPath, "BHAV " + b.ChunkID + " is not a private tree (< 4096); not representable in pack JSON");

            // PackBuilder.BuildPlacementInitTree always wires BHAV_Init to a compiler-owned
            // tree (labeled "__placement_init") that sets AllowedHeightFlags then calls the
            // pack's real init tree, if any — see that method for why. It's not user content:
            // decompiling it as an ordinary tree would round-trip it back in as one on
            // recompile, which then collides with a freshly synthesized one at the next id
            // (PackBuilder assumed no user tree could already be named "__placement_init").
            var placementInitBhav = objd.BHAV_Init != 0 ? iff.Get<BHAV>(objd.BHAV_Init) : null;
            var isSyntheticPlacementInit = placementInitBhav?.ChunkLabel == "__placement_init";

            var bhavs = allBhavs
                .Where(x => x.ChunkID >= PackBuilder.PRIVATE_TREE_BASE)
                .Where(x => !isSyntheticPlacementInit || x.ChunkID != placementInitBhav.ChunkID)
                .OrderBy(x => x.ChunkID).ToList();
            if (bhavs.Count == 0) D.Error(iffPath, "no private BHAV chunks (a pack object needs at least one tree)");

            TreeNames = new Dictionary<ushort, string>();
            var usedNames = new HashSet<string>();
            foreach (var b in bhavs)
            {
                var treeName = (b.ChunkLabel != null && b.ChunkLabel != "") ? b.ChunkLabel : ("tree_" + b.ChunkID);
                if (!usedNames.Add(treeName))
                {
                    treeName = treeName + "_" + b.ChunkID;
                    usedNames.Add(treeName);
                }
                TreeNames[b.ChunkID] = treeName;
            }

            var objJson = new JObject
            {
                ["id"] = objId,
                ["guid"] = "0x" + objd.GUID.ToString("X8"),
                ["name"] = name,
            };
            if (objd.Price != 0) objJson["price"] = objd.Price;

            // The original appearance.clone_from_guid/generated choice isn't recoverable from
            // BaseGraphicID/the rendered chunks themselves (those point at copied/rendered
            // data, not at the source GUID or generator that produced it) — but objects built
            // after provenance tracking landed carry it separately, in a reserved STR# chunk
            // (see AppearanceProvenance). Prefer that; only fall back to a placeholder for
            // .iffs that predate it.
            var provenance = AppearanceProvenance.Read(iff);
            if (provenance != null)
            {
                objJson["appearance"] = provenance;
            }
            else
            {
                // Same can't-recover situation as attribute names below. Substitute a
                // placeholder generator so the decompiled pack still compiles (appearance is
                // now required), and warn so nobody mistakes the placeholder for the real
                // appearance — this .iff simply predates provenance tracking, this isn't a
                // recovery failure on a newer object.
                objJson["appearance"] = new JObject { ["generated"] = new JObject { ["generator"] = "chair" } };
                D.Warn(iffPath, "this .iff predates appearance provenance tracking, so the original appearance.clone_from_guid/generated choice isn't recoverable; substituted a placeholder appearance.generated (chair) — replace it by hand");
            }

            if (objd.NumAttributes > 0)
            {
                // attribute names are not stored in the iff; generate stable placeholders
                var attrs = new JArray();
                for (int i = 0; i < objd.NumAttributes; i++) attrs.Add("attr_" + i);
                objJson["attributes"] = attrs;
            }

            var dialogStr = iff.Get<STR>(PackBuilder.DIALOG_STR_ID);
            if (dialogStr != null && dialogStr.Length > 0)
            {
                var dialog = new JObject();
                for (int i = 0; i < dialogStr.Length; i++)
                {
                    var v = dialogStr.GetString(i);
                    if (!string.IsNullOrEmpty(v)) dialog[(i + 1).ToString()] = v; // ids are 1-based
                }
                if (dialog.Count > 0) objJson["strings"] = new JObject { ["dialog"] = dialog };
            }

            var ttab = iff.Get<TTAB>(objd.TreeTableID != 0 ? objd.TreeTableID : PackBuilder.TTAB_ID);
            if (ttab != null && ttab.Interactions.Length > 0)
            {
                var ttas = iff.Get<TTAs>(ttab.ChunkID);
                var interactions = new JArray();
                for (int i = 0; i < ttab.Interactions.Length; i++)
                {
                    interactions.Add(DecompileInteraction(ttab.Interactions[i], i, ttas, iffPath));
                }
                objJson["interactions"] = interactions;
            }

            var trees = new JObject();
            foreach (var b in bhavs)
            {
                trees[TreeNames[b.ChunkID]] = DecompileTree(b, iffPath + " BHAV " + b.ChunkID);
            }
            objJson["trees"] = trees;

            var entry = new JObject();
            if (objd.BHAV_MainID != 0) entry["main"] = EntryName(objd.BHAV_MainID, iffPath + " OBJD BHAV_MainID");
            if (isSyntheticPlacementInit)
            {
                // The synthetic tree's second instruction (if present) is the private-tree-call
                // to the pack's real init — its opcode IS that tree's chunk id, per CompileCall.
                // No second instruction means the pack never declared its own init tree.
                if (placementInitBhav.Instructions.Length > 1)
                {
                    var callToUserInit = placementInitBhav.Instructions[1].Opcode;
                    if (callToUserInit >= PackBuilder.PRIVATE_TREE_BASE)
                        entry["init"] = EntryName(callToUserInit, iffPath + " OBJD BHAV_Init (via __placement_init)");
                }
            }
            else if (objd.BHAV_Init != 0) entry["init"] = EntryName(objd.BHAV_Init, iffPath + " OBJD BHAV_Init");
            if (entry.Count > 0) objJson["entry_points"] = entry;

            return new JObject
            {
                ["schema"] = "fso-pack/0.1",
                ["engine"] = "tso",
                ["pack"] = new JObject
                {
                    ["id"] = objId,
                    ["name"] = name,
                    ["description"] = "Decompiled from " + Path.GetFileName(iffPath),
                },
                ["objects"] = new JArray { objJson },
            };
        }

        private string EntryName(ushort id, string path)
        {
            if (TreeNames.TryGetValue(id, out var treeName)) return treeName;
            D.Error(path, "entry point references BHAV " + id + " which is not a private tree in this iff");
            return "missing_tree_" + id;
        }

        private static string Sanitize(string s)
        {
            var chars = s.ToLowerInvariant().Select(c => (char.IsLetterOrDigit(c) ? c : '_')).ToArray();
            var result = new string(chars);
            return result == "" ? "object" : result;
        }

        // ---- interactions -----------------------------------------------------

        private JObject DecompileInteraction(TTABInteraction inter, int index, TTAs ttas, string path)
        {
            path = path + " TTAB[" + index + "]";
            var result = new JObject();

            var interName = ttas?.GetString((int)inter.TTAIndex);
            result["name"] = string.IsNullOrEmpty(interName) ? ("interaction_" + index) : interName;

            if (inter.TTAIndex != (uint)index)
                D.Error(path, "sparse TTAIndex " + inter.TTAIndex + " (expected " + index + "); not representable — the compiler assigns indices by position");

            if (TreeNames.TryGetValue(inter.ActionFunction, out var action)) result["action"] = action;
            else D.Error(path, "action function " + inter.ActionFunction + " is not a private tree in this iff");
            if (inter.TestFunction != 0)
            {
                if (TreeNames.TryGetValue(inter.TestFunction, out var test)) result["test"] = test;
                else D.Error(path, "test function " + inter.TestFunction + " is not a private tree in this iff");
            }

            // unknown flag bits = unrepresentable
            const int knownTTABFlags = 1 | 2 | 4 | 8 | 128 | 256 | 512 | 1024 | 2048 | 4096 | 0xF0000;
            if (((int)inter.Flags & ~knownTTABFlags) != 0)
                D.Error(path, "TTAB flags 0x" + ((int)inter.Flags).ToString("X") + " contain bits the schema cannot represent");
            const int knownTSOFlags = 0xBF; // all but UnderParentalControl
            if (((int)inter.Flags2 & ~knownTSOFlags) != 0)
                D.Error(path, "TTAB Flags2 0x" + ((int)inter.Flags2).ToString("X") + " contain bits the schema cannot represent");

            var allow = new JObject();
            if (inter.AllowVisitors) allow["visitors"] = true;
            if (inter.AllowObjectOwner) allow["owner"] = true;
            if (inter.AllowRoommates) allow["roommates"] = true;
            if (inter.AllowFriends) allow["friends"] = true;
            if (inter.AllowGhosts) allow["ghosts"] = true;
            if (inter.AllowCSRs) allow["csrs"] = true;
            if (inter.AllowCats) allow["cats"] = true;
            if (inter.AllowDogs) allow["dogs"] = true;
            result["allow"] = allow;

            var flags = new JObject();
            if (inter.Debug) flags["debug"] = true;
            if (inter.AutoFirst) flags["auto_first"] = true;
            if (inter.RunImmediately) flags["run_immediately"] = true;
            if (inter.MustRun) flags["must_run"] = true;
            if (inter.AllowConsecutive) flags["allow_consecutive"] = true;
            if (inter.Joinable) flags["joinable"] = true;
            if (inter.Leapfrog) flags["leapfrog"] = true;
            if (inter.Carrying) flags["carrying"] = true;
            if (inter.Repair) flags["repair"] = true;
            if (inter.AlwaysCheck) flags["always_check"] = true;
            if (inter.WhenDead) flags["when_dead"] = true;
            if (flags.Count > 0) result["flags"] = flags;

            var autonomy = new JObject();
            var motives = new JObject();
            for (int m = 0; m < inter.MotiveEntries.Length; m++)
            {
                var entry = inter.MotiveEntries[m];
                if (entry.EffectRangeMinimum != 0 || entry.PersonalityModifier != 0)
                    D.Error(path, "motive entry " + m + " has EffectRangeMinimum/PersonalityModifier; not representable in schema v0.1");
                if (entry.EffectRangeDelta == 0) continue;
                if (MotiveNames.TryGetValue((byte)m, out var motiveName)) motives[motiveName] = entry.EffectRangeDelta;
                else D.Error(path, "motive entry " + m + " has no schema motive name");
            }
            if (motives.Count > 0) autonomy["advertised_motives"] = motives;
            if (inter.AutonomyThreshold != 0) autonomy["threshold"] = inter.AutonomyThreshold;
            if (inter.AttenuationCode != 0)
            {
                var attenNames = new Dictionary<uint, string> { { 1, "none" }, { 2, "low" }, { 3, "medium" }, { 4, "high" } };
                if (attenNames.TryGetValue(inter.AttenuationCode, out var atten)) autonomy["attenuation"] = atten;
                else D.Error(path, "unknown attenuation code " + inter.AttenuationCode);
            }
            else if (inter.AttenuationValue != 0)
            {
                autonomy["attenuation"] = inter.AttenuationValue;
            }
            if (inter.JoiningIndex != 0) autonomy["joining_index"] = inter.JoiningIndex;
            if (autonomy.Count > 0) result["autonomy"] = autonomy;

            return result;
        }

        // ---- trees ------------------------------------------------------------

        private JObject DecompileTree(BHAV bhav, string path)
        {
            var tree = new JObject();
            if (bhav.Args > 0)
            {
                var args = new JArray();
                for (int i = 0; i < bhav.Args; i++) args.Add("arg_" + i);
                tree["args"] = args;
            }
            if (bhav.Locals > 0)
            {
                var locals = new JArray();
                for (int i = 0; i < bhav.Locals; i++) locals.Add("local_" + i);
                tree["locals"] = locals;
            }
            if (bhav.Instructions.Length > 253)
                D.Error(path, "tree has " + bhav.Instructions.Length + " instructions; max is 253");

            var nodes = new JArray();
            for (int i = 0; i < bhav.Instructions.Length; i++)
            {
                nodes.Add(DecompileNode(bhav.Instructions[i], i, bhav.Instructions.Length, path + " n" + i));
            }
            tree["nodes"] = nodes;
            return tree;
        }

        private JObject DecompileNode(BHAVInstruction inst, int index, int count, string path)
        {
            var node = new JObject { ["id"] = "n" + index };

            if (inst.Opcode >= 256) DecompileCall(inst, node, path);
            else DecompilePrimitive(inst, node, path);

            node["then"] = Branch(inst.TruePointer, count, path + " then");
            node["else"] = Branch(inst.FalsePointer, count, path + " else");
            return node;
        }

        private string Branch(byte pointer, int count, string path)
        {
            switch (pointer)
            {
                case TreeCompiler.POINTER_RETURN_TRUE: return "return true";
                case TreeCompiler.POINTER_RETURN_FALSE: return "return false";
                case TreeCompiler.POINTER_ERROR: return "error";
            }
            if (pointer < count) return "n" + pointer;
            D.Error(path, "branch pointer " + pointer + " is out of range (tree has " + count + " nodes)");
            return "error";
        }

        private void DecompileCall(BHAVInstruction inst, JObject node, string path)
        {
            if (inst.Opcode >= PackBuilder.PRIVATE_TREE_BASE && inst.Opcode < 8192)
            {
                if (TreeNames.TryGetValue(inst.Opcode, out var treeName)) node["call"] = treeName;
                else
                {
                    D.Error(path, "call to private tree " + inst.Opcode + " which is not in this iff");
                    node["call"] = "missing_tree_" + inst.Opcode;
                }
            }
            else
            {
                D.Error(path, "opcode " + inst.Opcode + " is a " + (inst.Opcode >= 8192 ? "semiglobal" : "global") + " tree call; not representable in pack JSON");
                node["call"] = "external_tree_" + inst.Opcode;
            }

            using (var r = Reader(inst.Operand))
            {
                var args = new short[4];
                for (int i = 0; i < 4; i++) args[i] = r.ReadInt16();
                int last = 3;
                while (last >= 0 && args[last] == 0) last--;
                if (last >= 0)
                {
                    var arr = new JArray();
                    for (int i = 0; i <= last; i++) arr.Add(args[i]);
                    node["args"] = arr;
                }
            }
        }

        // ---- primitives (inverse of TreeCompiler serializers) -----------------

        private void DecompilePrimitive(BHAVInstruction inst, JObject node, string path)
        {
            using (var r = Reader(inst.Operand))
            {
                switch (inst.Opcode)
                {
                    case 0x02: node["prim"] = "expression"; Expression(r, node, path); break;
                    case 0x00: node["prim"] = "sleep"; node["ticks_param"] = r.ReadInt16(); break;
                    case 0x11: node["prim"] = "idle_for_input"; IdleForInput(r, node, path); break;
                    case 0x2C: node["prim"] = "animate"; Animate(r, node, path); break;
                    case 0x17: node["prim"] = "play_sound"; PlaySound(r, node, path); break;
                    case 0x24: node["prim"] = "dialog_private"; Dialog(r, node, path); break;
                    case 0x26: node["prim"] = "dialog_global"; Dialog(r, node, path); break;
                    case 0x27: node["prim"] = "dialog_semiglobal"; Dialog(r, node, path); break;
                    case 0x1B: node["prim"] = "goto_relative"; GotoRelative(r, node, path); break;
                    case 0x2D: node["prim"] = "goto_routing_slot"; GotoRoutingSlot(r, node, path); break;
                    case 0x0D: node["prim"] = "push_interaction"; PushInteraction(r, node, path); break;
                    case 0x0E: node["prim"] = "find_best_object_for_function"; node["function"] = r.ReadUInt16(); break;
                    case 0x1D: node["prim"] = "set_motive_deltas"; SetMotiveDeltas(r, node, path); break;
                    case 0x20: node["prim"] = "test_object_type"; TestObjectType(r, node, path); break;
                    case 0x08: node["prim"] = "random_number"; RandomNumber(r, node, path); break;
                    case 0x12: node["prim"] = "remove_object_instance"; RemoveObjectInstance(r, node, path); break;
                    case 0x2A: node["prim"] = "create_object_instance"; CreateObjectInstance(r, node, path); break;
                    case 0x06: node["prim"] = "change_suit_or_accessory"; ChangeSuitOrAccessory(r, node, path); break;
                    case 0x15: node["prim"] = "show_string"; ShowString(r, node, path); break;
                    case 0x29: node["prim"] = "set_balloon_headline"; SetBalloonHeadline(r, node, path); break;
                    default:
                        D.Error(path, "unsupported primitive opcode 0x" + inst.Opcode.ToString("X2") + "; not representable in pack JSON v0.1");
                        node["prim"] = "unsupported_0x" + inst.Opcode.ToString("X2");
                        return;
                }
                CheckPadding(r, path);
            }
        }

        private static BinaryReader Reader(byte[] operand)
        {
            return new BinaryReader(new MemoryStream(operand));
        }

        private void CheckPadding(BinaryReader r, string path)
        {
            var stream = r.BaseStream;
            while (stream.Position < stream.Length)
            {
                var pos = stream.Position;
                var b = r.ReadByte();
                if (b != 0) D.Error(path, "nonzero operand padding at byte " + pos + " (0x" + b.ToString("X2") + "); not representable");
            }
        }

        private JObject Scoped(byte scope, short data, string path)
        {
            if (!ScopeNames.TryGetValue(scope, out var scopeName))
            {
                D.Error(path, "unknown variable scope " + scope);
                scopeName = "literal";
            }
            return new JObject { ["scope"] = scopeName, ["value"] = data };
        }

        private string Lookup<TK>(Dictionary<TK, string> table, TK key, string what, string path)
        {
            if (table.TryGetValue(key, out var name)) return name;
            D.Error(path, "unknown " + what + " value " + key);
            return null;
        }

        private void CheckFlagBits(byte flags, int knownMask, string path)
        {
            if ((flags & ~knownMask) != 0)
                D.Error(path, "flags 0x" + flags.ToString("X2") + " contain bits the schema cannot represent");
        }

        private void Expression(BinaryReader r, JObject node, string path)
        {
            var lhsData = r.ReadInt16();
            var rhsData = r.ReadInt16();
            var signed = r.ReadByte();
            var op = r.ReadByte();
            var lhsScope = r.ReadByte();
            var rhsScope = r.ReadByte();

            node["lhs"] = Scoped(lhsScope, lhsData, path + ".lhs");
            node["op"] = Lookup(OperatorNames, op, "operator", path + ".op");
            node["rhs"] = Scoped(rhsScope, rhsData, path + ".rhs");
            if (signed == 1) node["signed"] = true;
            else if (signed != 0) D.Error(path, "IsSigned byte " + signed + " is not 0/1; not representable");
        }

        private void IdleForInput(BinaryReader r, JObject node, string path)
        {
            node["ticks_param"] = r.ReadInt16();
            var allowPush = r.ReadUInt16();
            if (allowPush == 1) node["allow_push"] = true;
            else if (allowPush != 0) D.Error(path, "AllowPush value " + allowPush + " is not 0/1; not representable");
        }

        private void Animate(BinaryReader r, JObject node, string path)
        {
            var animId = r.ReadUInt16();
            var eventLocal = r.ReadByte();
            var pad = r.ReadByte();
            if (pad != 0) D.Error(path, "animate pad byte is nonzero; not representable");
            var source = r.ReadByte();
            var flags = r.ReadByte();
            var count = r.ReadByte();

            if (animId != 0 || source != 0)
            {
                node["animation"] = new JObject
                {
                    ["source"] = Lookup(AnimSourceNames, source, "animation source", path + ".animation.source"),
                    ["id"] = animId,
                };
            }
            if (eventLocal != 0) node["event_local"] = eventLocal;

            var mode = (flags & 1) | ((flags >> 3) & 2);
            if (mode == 2) node["mode"] = "stop_carry_play_and_wait";
            else if (mode != 0) D.Error(path, "animate mode " + mode + " has no schema name");
            if ((flags & 2) != 0) node["play_backwards"] = true;
            if ((flags & 4) != 0) node["id_from_param"] = true;
            if ((flags & 32) != 0) node["store_frame_in_local"] = true;
            if ((flags & 64) != 0) node["hurryable"] = true;
            CheckFlagBits(flags, 1 | 2 | 4 | 16 | 32 | 64, path);

            if (count != 0) node["expected_event_count"] = count;
        }

        private void PlaySound(BinaryReader r, JObject node, string path)
        {
            node["event_id"] = r.ReadUInt16();
            var sampleRate = r.ReadUInt16();
            if (sampleRate != 0) D.Error(path, "nonzero SampleRate " + sampleRate + "; not representable");
            var flags = r.ReadByte();
            var volume = r.ReadByte();

            if ((flags & 1) != 0) node["loop"] = true;
            if ((flags & 2) != 0) node["stack_obj_as_source"] = true;
            if ((flags & 4) != 0) node["no_zoom"] = true;
            if ((flags & 8) != 0) node["no_pan"] = true;
            if ((flags & 16) != 0) node["auto_vary"] = true;
            if ((flags & 32) != 0) node["sim_speed_affects"] = true;
            CheckFlagBits(flags, 0x3F, path);
            if (volume != 100) node["volume"] = volume; // compiler default is 100
        }

        private void Dialog(BinaryReader r, JObject node, string path)
        {
            var cancel = r.ReadByte();
            var iconName = r.ReadByte();
            var message = r.ReadByte();
            var yes = r.ReadByte();
            var no = r.ReadByte();
            var type = r.ReadByte();
            var title = r.ReadByte();
            var flags = r.ReadByte();

            if (message != 0) node["message"] = message;
            if (title != 0) node["title"] = title;
            if (yes != 0) node["yes"] = yes;
            if (no != 0) node["no"] = no;
            if (cancel != 0) node["cancel"] = cancel;
            if (iconName != 0) node["icon_name"] = iconName;
            if (type != 0) node["type"] = Lookup(DialogTypeNames, type, "dialog type", path + ".type");

            if ((flags & 1) != 0) node["continue"] = true;
            var icon = (byte)((flags >> 1) & 7);
            if (icon != 0)
            {
                var iconType = Lookup(DialogIconNames, icon, "dialog icon type", path + ".icon");
                if (iconType != null) node["icon"] = iconType;
            }
            if ((flags & (1 << 4)) != 0) node["use_temp_xl"] = true;
            if ((flags & (1 << 5)) != 0) node["use_temp_1"] = true;
            if ((flags & (1 << 6)) != 0) node["filter_profanity"] = true;
            if ((flags & (1 << 7)) != 0) node["new_engage_continue"] = true;
            // all 8 bits are covered above; no unknown-bit check needed
        }

        private void GotoRelative(BinaryReader r, JObject node, string path)
        {
            var oldTrap = r.ReadUInt16();
            var location = (sbyte)r.ReadByte();
            var direction = (sbyte)r.ReadByte();
            var routeCount = r.ReadUInt16();
            var flags = r.ReadByte();

            node["location"] = Lookup(LocationNames, location, "location", path + ".location");
            node["direction"] = Lookup(DirectionNames, direction, "direction", path + ".direction");
            if (oldTrap != 0) node["old_trap_count"] = oldTrap;
            if (routeCount != 0) node["route_count"] = routeCount;
            if ((flags & 1) != 0) node["allow_diff_alt"] = true;
            if ((flags & 2) != 0) node["no_failure_trees"] = true;
            CheckFlagBits(flags, 3, path);
        }

        private void GotoRoutingSlot(BinaryReader r, JObject node, string path)
        {
            var data = r.ReadUInt16();
            var type = r.ReadUInt16();
            var flags = r.ReadByte();

            node["slot"] = new JObject
            {
                ["type"] = Lookup(SlotScopeNames, type, "slot type", path + ".slot.type"),
                ["value"] = data,
            };
            if ((flags & 1) != 0) node["no_failure_trees"] = true;
            CheckFlagBits(flags, 1, path);
        }

        private void PushInteraction(BinaryReader r, JObject node, string path)
        {
            var interaction = r.ReadByte();
            var objLoc = r.ReadByte();
            var priority = r.ReadByte();
            var flags = r.ReadByte();
            var iconLoc = r.ReadByte();

            if (interaction == 254) node["interaction"] = "from_temp";
            else node["interaction"] = interaction; // TTAB index; the name is not recoverable here

            if ((flags & 2) != 0)
                node["object_var"] = new JObject { ["scope"] = "local", ["value"] = objLoc };
            else if (objLoc != 0)
                node["object_var"] = new JObject { ["scope"] = "parameters", ["value"] = objLoc };

            if (priority != 0) node["priority"] = Lookup(PriorityNames, priority, "push priority", path + ".priority");
            if ((flags & 1) != 0) node["use_custom_icon"] = true;
            if ((flags & 4) != 0) node["push_head_continuation"] = true;
            if ((flags & 128) != 0) node["push_tail_continuation"] = true;
            CheckFlagBits(flags, 1 | 2 | 4 | 128, path);
            if (iconLoc != 0) node["icon_location"] = iconLoc;
        }

        private void SetMotiveDeltas(BinaryReader r, JObject node, string path)
        {
            var deltaOwner = r.ReadByte();
            var maxOwner = r.ReadByte();
            var motive = r.ReadByte();
            var flags = r.ReadByte();
            var deltaData = r.ReadInt16();
            var maxData = r.ReadInt16();

            var clearAll = (flags & 1) != 0;
            if (!(clearAll && motive == 0))
                node["motive"] = Lookup(MotiveNames, motive, "motive", path + ".motive");
            node["delta"] = Scoped(deltaOwner, deltaData, path + ".delta");
            node["max"] = Scoped(maxOwner, maxData, path + ".max");
            if (clearAll) node["clear_all"] = true;
            if ((flags & 2) != 0) node["once"] = true;
            CheckFlagBits(flags, 3, path);
        }

        private void TestObjectType(BinaryReader r, JObject node, string path)
        {
            var guid = r.ReadUInt32();
            var idData = r.ReadInt16();
            var idOwner = r.ReadByte();

            node["guid"] = "0x" + guid.ToString("X8");
            node["object_id"] = Scoped(idOwner, idData, path + ".object_id");
        }

        private void RandomNumber(BinaryReader r, JObject node, string path)
        {
            var destData = r.ReadInt16();
            var destScope = r.ReadUInt16();
            var rangeData = r.ReadInt16();
            var rangeScope = r.ReadUInt16();

            if (destScope > 255 || rangeScope > 255)
                D.Error(path, "random_number scope out of byte range; not representable");
            node["destination"] = Scoped((byte)destScope, destData, path + ".destination");
            node["range"] = Scoped((byte)rangeScope, rangeData, path + ".range");
        }

        private void RemoveObjectInstance(BinaryReader r, JObject node, string path)
        {
            var target = r.ReadInt16();
            var flags = r.ReadByte();

            if (target == 1) node["target"] = "stack_object";
            else if (target != 0) D.Error(path, "remove_object_instance target " + target + " has no schema name (expected 0=me, 1=stack_object)");
            if ((flags & 1) != 0) node["return_immediately"] = true;
            if ((flags & 2) != 0) node["cleanup_all"] = true;
            CheckFlagBits(flags, 3, path);
        }

        private void CreateObjectInstance(BinaryReader r, JObject node, string path)
        {
            var guid = r.ReadUInt32();
            var position = r.ReadByte();
            var flags = r.ReadByte();
            var localToUse = r.ReadByte();
            var callback = r.ReadByte();

            node["guid"] = "0x" + guid.ToString("X8");
            if (position != 0) node["position"] = Lookup(PositionNames, position, "create position", path + ".position");
            if ((flags & 1) != 0) node["no_duplicate"] = true;
            if ((flags & 2) != 0) node["pass_object_ids"] = true;
            if ((flags & 4) != 0) node["use_neighbor"] = true;
            if ((flags & 8) != 0) node["fail_if_non_empty"] = true;
            if ((flags & 16) != 0) node["pass_temp_0"] = true;
            if ((flags & 32) != 0) node["face_stack_obj_dir"] = true;
            CheckFlagBits(flags, 0x3F, path);
            if (localToUse != 0) node["local"] = localToUse;
            if (callback != 0) node["interaction_callback"] = callback;
        }

        private void ChangeSuitOrAccessory(BinaryReader r, JObject node, string path)
        {
            var suit = r.ReadByte();
            var suitScope = r.ReadByte();
            var flags = r.ReadUInt16();

            node["suit"] = suit;
            if (suitScope != Names.SuitScopes["person"]) // compiler default
                node["scope"] = Lookup(SuitScopeNames, suitScope, "suit scope", path + ".scope");
            if ((flags & 1) != 0) node["remove"] = true;
            if ((flags & 2) != 0) node["use_temp"] = true;
            if ((flags & 4) != 0) node["update"] = true;
            if ((flags & ~7) != 0) D.Error(path, "suit flags 0x" + flags.ToString("X") + " contain bits the schema cannot represent");
        }

        private void ShowString(BinaryReader r, JObject node, string path)
        {
            var table = r.ReadUInt16();
            var stringId = r.ReadUInt16();
            var flags = r.ReadByte();

            if (table != 300) node["table"] = table; // compiler default
            node["string_id"] = stringId;
            if ((flags & 1) != 0) node["no_history"] = true;
            CheckFlagBits(flags, 1, path);
        }

        private void SetBalloonHeadline(BinaryReader r, JObject node, string path)
        {
            var flags2 = r.ReadUInt16();
            var index = (sbyte)r.ReadByte();
            var group = r.ReadByte();
            var duration = r.ReadInt16();
            var type = r.ReadByte();
            var flags = r.ReadByte();

            if (group != Names.BalloonGroups["balloon"]) // compiler default
                node["group"] = Lookup(BalloonGroupNames, group, "balloon group", path + ".group");
            if (index != 0) node["index"] = index;
            node["duration"] = duration;
            if ((flags2 & 1) != 0) node["of_stack_obj"] = true;
            var algorithmic = flags2 >> 1;
            if (algorithmic != 0) node["algorithmic"] = algorithmic;
            if (type != 0) node["type"] = type;
            if ((flags & 1) != 0) node["inactive"] = true;
            if ((flags & 2) != 0) node["crossed"] = true;
            if ((flags & 4) != 0) node["backwards"] = true;
            if ((flags & 8) != 0) node["duration_in_loops"] = true;
            if ((flags & 16) != 0) node["indexed"] = true;
            CheckFlagBits(flags, 0x1F, path);
        }
    }
}
