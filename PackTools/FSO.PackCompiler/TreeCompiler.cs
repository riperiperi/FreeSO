using System;
using System.Collections.Generic;
using System.IO;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Compiles one pack tree into BHAV instructions. Operand byte layouts follow
    /// PackTools/simantics-vocabulary.md, verified against the operand Read() methods in
    /// TSOClient/tso.simantics/Primitives/*.cs. All operands are 8 bytes, little-endian.
    /// </summary>
    public class TreeCompiler
    {
        public const byte POINTER_ERROR = 253;
        public const byte POINTER_RETURN_TRUE = 254;
        public const byte POINTER_RETURN_FALSE = 255;

        private readonly Diagnostics D;
        private readonly PackObject Obj;
        private readonly Dictionary<string, ushort> TreeIds;        // tree name -> BHAV chunk id (4096+)
        private readonly Dictionary<string, int> InteractionIndex;  // interaction name -> TTAIndex

        private PackTree Tree;
        private Dictionary<string, byte> NodeIndex;

        public TreeCompiler(Diagnostics d, PackObject obj, Dictionary<string, ushort> treeIds, Dictionary<string, int> interactionIndex)
        {
            D = d;
            Obj = obj;
            TreeIds = treeIds;
            InteractionIndex = interactionIndex;
        }

        public BHAVInstruction[] Compile(PackTree tree)
        {
            Tree = tree;
            NodeIndex = new Dictionary<string, byte>();
            for (int i = 0; i < tree.Nodes.Count && i < 253; i++)
            {
                var id = tree.Nodes[i].Id;
                if (id != null && !NodeIndex.ContainsKey(id)) NodeIndex[id] = (byte)i;
            }

            var instructions = new BHAVInstruction[Math.Min(tree.Nodes.Count, 253)];
            for (int i = 0; i < instructions.Length; i++)
            {
                var node = tree.Nodes[i];
                var inst = new BHAVInstruction
                {
                    TruePointer = ResolveBranch(node.Then, node.Path + ".then"),
                    FalsePointer = ResolveBranch(node.Else, node.Path + ".else"),
                };

                if (node.Call != null)
                {
                    inst.Opcode = CompileCall(node, out var operand);
                    inst.Operand = operand;
                }
                else
                {
                    inst.Opcode = CompilePrimitive(node, out var operand);
                    inst.Operand = operand;
                }
                node.Fields.Done(); // any field not consumed by the operand builder = unknown field error
                instructions[i] = inst;
            }
            return instructions;
        }

        private byte ResolveBranch(string target, string path)
        {
            switch (target)
            {
                case null: return POINTER_ERROR; // missing; already reported by parser
                case "return true": return POINTER_RETURN_TRUE;
                case "return false": return POINTER_RETURN_FALSE;
                case "error": return POINTER_ERROR;
            }
            if (NodeIndex.TryGetValue(target, out var idx)) return idx;
            D.Error(path, "unresolved label \"" + target + "\" (not a node id in this tree, and not \"return true\"/\"return false\"/\"error\")");
            return POINTER_ERROR;
        }

        // ---- tree calls -------------------------------------------------------

        private ushort CompileCall(PackNode node, out byte[] operand)
        {
            operand = new byte[8];
            ushort opcode = 0;
            if (!TreeIds.TryGetValue(node.Call, out opcode))
            {
                D.Error(node.Path + ".call", "unresolved tree name \"" + node.Call + "\"");
            }

            // VMSubRoutineOperand: four int16 args. -1 = pass temp[i] (when any of args 1-3 nonzero).
            var args = new short[4];
            var arr = node.Fields.OptArr("args");
            if (arr != null)
            {
                if (arr.Count > 4) D.Error(node.Path + ".args", "tree calls take at most 4 args");
                for (int i = 0; i < arr.Count && i < 4; i++)
                {
                    if (arr[i].Type != Newtonsoft.Json.Linq.JTokenType.Integer)
                    {
                        D.Error(node.Path + ".args[" + i + "]", "expected an integer");
                        continue;
                    }
                    var v = (long)arr[i];
                    if (v < short.MinValue || v > short.MaxValue)
                        D.Error(node.Path + ".args[" + i + "]", "value out of int16 range");
                    else args[i] = (short)v;
                }
            }

            using (var io = new BinaryWriter(new MemoryStream(operand)))
            {
                io.Write(args[0]);
                io.Write(args[1]);
                io.Write(args[2]);
                io.Write(args[3]);
            }
            return opcode;
        }

        // ---- primitives -------------------------------------------------------

        private ushort CompilePrimitive(PackNode node, out byte[] operand)
        {
            operand = new byte[8];
            using (var io = new BinaryWriter(new MemoryStream(operand)))
            {
                switch (node.Prim)
                {
                    case "expression": Expression(node, io); return 0x02;
                    case "sleep": Sleep(node, io); return 0x00;
                    case "idle_for_input": IdleForInput(node, io); return 0x11;
                    case "animate": Animate(node, io); return 0x2C;
                    case "play_sound": PlaySound(node, io); return 0x17;
                    case "dialog_private": Dialog(node, io); return 0x24;
                    case "dialog_global": Dialog(node, io); return 0x26;
                    case "dialog_semiglobal": Dialog(node, io); return 0x27;
                    case "goto_relative": GotoRelative(node, io); return 0x1B;
                    case "goto_routing_slot": GotoRoutingSlot(node, io); return 0x2D;
                    case "push_interaction": PushInteraction(node, io); return 0x0D;
                    case "find_best_object_for_function": FindBestObjectForFunction(node, io); return 0x0E;
                    case "set_motive_deltas": SetMotiveDeltas(node, io); return 0x1D;
                    case "test_object_type": TestObjectType(node, io); return 0x20;
                    case "random_number": RandomNumber(node, io); return 0x08;
                    case "remove_object_instance": RemoveObjectInstance(node, io); return 0x12;
                    case "create_object_instance": CreateObjectInstance(node, io); return 0x2A;
                    case "change_suit_or_accessory": ChangeSuitOrAccessory(node, io); return 0x06;
                    case "show_string": ShowString(node, io); return 0x15;
                    case "set_balloon_headline": SetBalloonHeadline(node, io); return 0x29;
                    default:
                        D.Error(node.Path + ".prim", "unknown primitive \"" + node.Prim + "\"");
                        node.Fields.MarkAllUsed();
                        return 0;
                }
            }
        }

        // ---- scoped values ----------------------------------------------------

        /// <summary>
        /// Reads a { "scope": ..., "value": N } / { "scope": ..., "name": "..." } object and
        /// resolves it to a (scope, data) pair. Names resolve via attributes/motives/locals/args.
        /// </summary>
        private (byte Scope, short Data) ScopedValue(PackNode node, string field, bool required)
        {
            var o = required ? node.Fields.ReqObj(field) : node.Fields.OptObj(field);
            if (o == null) return (Names.SCOPE_LITERAL, 0);

            byte scope = Names.SCOPE_LITERAL;
            var scopeName = o.ReqString("scope");
            if (scopeName != null && !Names.Scopes.TryGetValue(scopeName, out scope))
            {
                D.Error(o.Path + ".scope", "unknown scope \"" + scopeName + "\"");
                scope = Names.SCOPE_LITERAL;
            }

            short data = 0;
            var hasValue = o.Has("value");
            var hasName = o.Has("name");
            if (hasValue && hasName)
            {
                D.Error(o.Path, "give either \"value\" or \"name\", not both");
            }

            if (hasValue)
            {
                var v = o.OptInt("value");
                if (v < short.MinValue || v > short.MaxValue) D.Error(o.Path + ".value", "value out of int16 range");
                else data = (short)v;
            }
            else if (hasName)
            {
                var name = o.OptString("name");
                if (name != null) data = ResolveName(scope, name, o.Path + ".name");
            }
            else if (scope == Names.SCOPE_LITERAL)
            {
                D.Error(o.Path, "literal scope requires \"value\"");
            }
            // other scopes without value/name default to index 0 (engine convention: unset = 0)

            o.Done();
            return (scope, data);
        }

        private short ResolveName(byte scope, string name, string path)
        {
            switch (scope)
            {
                case Names.SCOPE_MY_ATTRIBUTES:
                case Names.SCOPE_STACK_OBJECT_ATTRIBUTES:
                    var attrIdx = Obj.Attributes.IndexOf(name);
                    if (attrIdx < 0) D.Error(path, "unresolved attribute \"" + name + "\" (declare it in the object's \"attributes\" list)");
                    return (short)Math.Max(0, attrIdx);
                case Names.SCOPE_MY_MOTIVES:
                case Names.SCOPE_STACK_OBJECT_MOTIVES:
                    if (Names.Motives.TryGetValue(name, out var motive)) return motive;
                    D.Error(path, "unknown motive \"" + name + "\" (expected one of: " + Names.List(Names.Motives) + ")");
                    return 0;
                case Names.SCOPE_LOCAL:
                    var localIdx = Tree.Locals.IndexOf(name);
                    if (localIdx < 0) D.Error(path, "unresolved local \"" + name + "\" (declare it in the tree's \"locals\" list)");
                    return (short)Math.Max(0, localIdx);
                case Names.SCOPE_PARAMETERS:
                    var argIdx = Tree.Args.IndexOf(name);
                    if (argIdx < 0) D.Error(path, "unresolved arg \"" + name + "\" (declare it in the tree's \"args\" list)");
                    return (short)Math.Max(0, argIdx);
                default:
                    D.Error(path, "scope does not support named references; use \"value\"");
                    return 0;
            }
        }

        /// <summary>Param reference: an arg name or a raw parameter index.</summary>
        private short ParamRef(PackNode node, string field)
        {
            var t = node.Fields.Opt(field);
            if (t == null)
            {
                D.Error(node.Path, "missing required field \"" + field + "\"");
                return 0;
            }
            if (t.Type == Newtonsoft.Json.Linq.JTokenType.Integer) return (short)(int)t;
            if (t.Type == Newtonsoft.Json.Linq.JTokenType.String)
            {
                var name = (string)t;
                var idx = Tree.Args.IndexOf(name);
                if (idx < 0)
                {
                    D.Error(node.Path + "." + field, "unresolved arg \"" + name + "\"");
                    return 0;
                }
                return (short)idx;
            }
            D.Error(node.Path + "." + field, "expected an arg name or parameter index");
            return 0;
        }

        private short LocalRef(JsonObj fields, string field, string path, short def = 0)
        {
            var t = fields.Opt(field);
            if (t == null) return def;
            if (t.Type == Newtonsoft.Json.Linq.JTokenType.Integer) return (short)(int)t;
            if (t.Type == Newtonsoft.Json.Linq.JTokenType.String)
            {
                var name = (string)t;
                var idx = Tree.Locals.IndexOf(name);
                if (idx < 0)
                {
                    D.Error(path + "." + field, "unresolved local \"" + name + "\"");
                    return def;
                }
                return (short)idx;
            }
            D.Error(path + "." + field, "expected a local name or index");
            return def;
        }

        private byte EnumByte(PackNode node, string field, Dictionary<string, byte> table, string def)
        {
            var name = node.Fields.OptString(field, def);
            if (name == null) return 0;
            if (table.TryGetValue(name, out var v)) return v;
            D.Error(node.Path + "." + field, "unknown value \"" + name + "\" (expected one of: " + Names.List(table) + ")");
            return 0;
        }

        private byte ByteField(PackNode node, string field, int def = 0)
        {
            var v = node.Fields.OptInt(field, def);
            if (v < 0 || v > 255)
            {
                D.Error(node.Path + "." + field, "value out of byte range 0-255");
                return 0;
            }
            return (byte)v;
        }

        private ushort UShortField(PackNode node, string field, int def = 0)
        {
            var v = node.Fields.OptInt(field, def);
            if (v < 0 || v > ushort.MaxValue)
            {
                D.Error(node.Path + "." + field, "value out of range 0-65535");
                return 0;
            }
            return (ushort)v;
        }

        private short ShortField(PackNode node, string field, int def = 0)
        {
            var v = node.Fields.OptInt(field, def);
            if (v < short.MinValue || v > short.MaxValue)
            {
                D.Error(node.Path + "." + field, "value out of int16 range");
                return 0;
            }
            return (short)v;
        }

        private byte StringIdField(PackNode node, string field)
        {
            var v = ByteField(node, field);
            if (v != 0 && !Obj.DialogStrings.ContainsKey(v))
                D.Warn(node.Path + "." + field, "dialog string id " + v + " is not defined in strings.dialog");
            return v;
        }

        // ---- operand writers (layouts per simantics-vocabulary.md) ------------

        // VMExpressionOperand: int16 LhsData, int16 RhsData, byte IsSigned, byte Operator, byte LhsOwner, byte RhsOwner
        private void Expression(PackNode node, BinaryWriter io)
        {
            var lhs = ScopedValue(node, "lhs", true);
            var opName = node.Fields.ReqString("op");
            byte op = 0;
            if (opName != null && !Names.Operators.TryGetValue(opName, out op))
                D.Error(node.Path + ".op", "unknown operator \"" + opName + "\" (expected one of: " + Names.List(Names.Operators) + ")");
            var rhs = ScopedValue(node, "rhs", true);
            var signed = node.Fields.OptBool("signed");

            if (Names.MutationOperators.Contains(op) &&
                (lhs.Scope == Names.SCOPE_LITERAL || lhs.Scope == Names.SCOPE_TUNING))
                D.Warn(node.Path, "expression assigns to an unwritable scope (literal/tuning); the VM will fail this silently");

            io.Write(lhs.Data);
            io.Write(rhs.Data);
            io.Write((byte)(signed ? 1 : 0));
            io.Write(op);
            io.Write(lhs.Scope);
            io.Write(rhs.Scope);
        }

        // VMSleepOperand: int16 StackVarToDec (parameter index holding tick count)
        private void Sleep(PackNode node, BinaryWriter io)
        {
            io.Write(ParamRef(node, "ticks_param"));
        }

        // VMIdleForInputOperand: int16 StackVarToDec, uint16 AllowPush
        private void IdleForInput(PackNode node, BinaryWriter io)
        {
            io.Write(ParamRef(node, "ticks_param"));
            io.Write((ushort)(node.Fields.OptBool("allow_push") ? 1 : 0));
        }

        // VMAnimateSimOperand: uint16 AnimationID, byte LocalEventNumber, byte pad, byte Source, byte Flags, byte ExpectedEventCount
        private void Animate(PackNode node, BinaryWriter io)
        {
            ushort animId = 0;
            byte source = 0;
            var anim = node.Fields.OptObj("animation");
            if (anim != null)
            {
                var sourceName = anim.ReqString("source");
                if (sourceName != null && !Names.AnimationSources.TryGetValue(sourceName, out source))
                    D.Error(anim.Path + ".source", "unknown animation source \"" + sourceName + "\" (expected one of: " + Names.List(Names.AnimationSources) + ")");
                var id = anim.OptInt("id");
                if (id < 0 || id > ushort.MaxValue) D.Error(anim.Path + ".id", "animation id out of range 0-65535");
                else animId = (ushort)id;
                anim.Done();
            }
            // animation omitted (or id 0) = clear/reset animation

            var eventLocal = LocalRef(node.Fields, "event_local", node.Path);
            if (eventLocal < 0 || eventLocal > 255) D.Error(node.Path + ".event_local", "local index out of byte range");

            // Flags bits (VMAnimateSim.cs property accessors): bit0+bit4 Mode, bit1 PlayBackwards,
            // bit2 IDFromParam, bit5 StoreFrameInLocal, bit6 Hurryable
            byte flags = 0;
            var mode = node.Fields.OptString("mode", "play_and_wait");
            switch (mode)
            {
                case "play_and_wait": break; // Mode 0
                case "stop_carry_play_and_wait": flags |= 1 << 4; break; // Mode 2: bit stored at (value & 2) << 3
                default: D.Error(node.Path + ".mode", "unknown mode \"" + mode + "\" (expected play_and_wait, stop_carry_play_and_wait)"); break;
            }
            if (node.Fields.OptBool("play_backwards")) flags |= 2;
            if (node.Fields.OptBool("id_from_param")) flags |= 4;
            if (node.Fields.OptBool("store_frame_in_local")) flags |= 32;
            if (node.Fields.OptBool("hurryable")) flags |= 64;

            io.Write(animId);
            io.Write((byte)eventLocal);
            io.Write((byte)0);
            io.Write(source);
            io.Write(flags);
            io.Write(ByteField(node, "expected_event_count"));
        }

        // VMPlaySoundOperand: uint16 EventID, uint16 SampleRate, byte Flags, byte Volume
        private void PlaySound(PackNode node, BinaryWriter io)
        {
            byte flags = 0;
            if (node.Fields.OptBool("loop")) flags |= 1;
            if (node.Fields.OptBool("stack_obj_as_source")) flags |= 2;
            if (node.Fields.OptBool("no_zoom")) flags |= 4;
            if (node.Fields.OptBool("no_pan")) flags |= 8;
            if (node.Fields.OptBool("auto_vary")) flags |= 16;
            if (node.Fields.OptBool("sim_speed_affects")) flags |= 32;

            io.Write(UShortField(node, "event_id"));
            io.Write((ushort)0); // SampleRate, effectively unused
            io.Write(flags);
            io.Write(ByteField(node, "volume", 100));
        }

        // VMDialogOperand: bytes Cancel, IconName, Message, Yes, No, Type, Title, Flags
        private void Dialog(PackNode node, BinaryWriter io)
        {
            var cancel = StringIdField(node, "cancel");
            var iconName = StringIdField(node, "icon_name");
            var message = StringIdField(node, "message");
            var yes = StringIdField(node, "yes");
            var no = StringIdField(node, "no");
            var title = StringIdField(node, "title");
            var type = EnumByte(node, "type", Names.DialogTypes, "message");

            // VMDialogFlags: bit0 Continue, bits1-3 icon type, bit4 UseTempXL, bit5 UseTemp1,
            // bit6 FilterProfanity, bit7 NewEngageContinue
            byte flags = 0;
            if (node.Fields.OptBool("continue")) flags |= 1;
            flags |= (byte)(EnumByte(node, "icon", Names.DialogIcons, "auto") << 1);
            if (node.Fields.OptBool("use_temp_xl")) flags |= 1 << 4;
            if (node.Fields.OptBool("use_temp_1")) flags |= 1 << 5;
            if (node.Fields.OptBool("filter_profanity")) flags |= 1 << 6;
            if (node.Fields.OptBool("new_engage_continue")) flags |= 1 << 7;

            io.Write(cancel);
            io.Write(iconName);
            io.Write(message);
            io.Write(yes);
            io.Write(no);
            io.Write(type);
            io.Write(title);
            io.Write(flags);
        }

        // VMGotoRelativePositionOperand: uint16 OldTrapCount, sbyte Location, sbyte Direction, uint16 RouteCount, byte Flags
        private void GotoRelative(PackNode node, BinaryWriter io)
        {
            sbyte location = 0;
            var locName = node.Fields.OptString("location", "in_front_of");
            if (!Names.GotoRelativeLocations.TryGetValue(locName, out location))
                D.Error(node.Path + ".location", "unknown location \"" + locName + "\" (expected one of: " + string.Join(", ", Names.GotoRelativeLocations.Keys) + ")");

            sbyte direction = -1;
            var dirName = node.Fields.OptString("direction", "any_direction");
            if (!Names.GotoRelativeDirections.TryGetValue(dirName, out direction))
                D.Error(node.Path + ".direction", "unknown direction \"" + dirName + "\" (expected one of: " + string.Join(", ", Names.GotoRelativeDirections.Keys) + ")");

            byte flags = 0;
            if (node.Fields.OptBool("allow_diff_alt")) flags |= 1;
            if (node.Fields.OptBool("no_failure_trees")) flags |= 2;

            io.Write(UShortField(node, "old_trap_count"));
            io.Write((byte)location);
            io.Write((byte)direction);
            io.Write(UShortField(node, "route_count"));
            io.Write(flags);
        }

        // VMGotoRoutingSlotOperand: uint16 Data, uint16 Type (VMSlotScope), byte Flags
        private void GotoRoutingSlot(PackNode node, BinaryWriter io)
        {
            ushort data = 0;
            ushort type = 1; // literal
            var slot = node.Fields.ReqObj("slot");
            if (slot != null)
            {
                var typeName = slot.OptString("type", "literal");
                if (!Names.SlotScopes.TryGetValue(typeName, out type))
                {
                    D.Error(slot.Path + ".type", "unknown slot type \"" + typeName + "\" (expected one of: " + string.Join(", ", Names.SlotScopes.Keys) + ")");
                    type = 1;
                }
                var v = slot.OptInt("value");
                if (v < 0 || v > ushort.MaxValue) D.Error(slot.Path + ".value", "value out of range 0-65535");
                else data = (ushort)v;
                slot.Done();
            }

            byte flags = 0;
            if (node.Fields.OptBool("no_failure_trees")) flags |= 1;

            io.Write(data);
            io.Write(type);
            io.Write(flags);
        }

        // VMPushInteractionOperand: byte Interaction, byte ObjectLocation, byte Priority, byte Flags, byte IconLocation
        private void PushInteraction(PackNode node, BinaryWriter io)
        {
            byte interaction = 0;
            var it = node.Fields.Opt("interaction");
            if (it == null)
            {
                D.Error(node.Path, "missing required field \"interaction\"");
            }
            else if (it.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
            {
                var v = (int)it;
                if (v < 0 || v > 255) D.Error(node.Path + ".interaction", "value out of byte range");
                else interaction = (byte)v;
            }
            else if (it.Type == Newtonsoft.Json.Linq.JTokenType.String)
            {
                var name = (string)it;
                if (name == "from_temp") interaction = 254; // sentinel: interaction id in temp
                else if (InteractionIndex.TryGetValue(name, out var idx)) interaction = (byte)idx;
                else D.Error(node.Path + ".interaction", "unresolved interaction \"" + name + "\" (not an interaction on this object)");
            }
            else D.Error(node.Path + ".interaction", "expected an interaction name, \"from_temp\", or a TTAB index");

            // Flags: bit0 UseCustomIcon, bit1 ObjectInLocal, bit2 PushHeadContinuation, bit7 PushTailContinuation
            byte flags = 0;
            byte objectLocation = 0;
            var objVar = node.Fields.OptObj("object_var");
            if (objVar != null)
            {
                var scopeName = objVar.ReqString("scope");
                short idx = 0;
                if (scopeName == "local")
                {
                    flags |= 2;
                    var name = objVar.OptString("name");
                    if (name != null) idx = ResolveName(Names.SCOPE_LOCAL, name, objVar.Path + ".name");
                    else idx = (short)objVar.OptInt("value");
                }
                else if (scopeName == "parameters")
                {
                    var name = objVar.OptString("name");
                    if (name != null) idx = ResolveName(Names.SCOPE_PARAMETERS, name, objVar.Path + ".name");
                    else idx = (short)objVar.OptInt("value");
                }
                else if (scopeName != null)
                {
                    D.Error(objVar.Path + ".scope", "push_interaction object_var scope must be \"local\" or \"parameters\"");
                }
                if (idx < 0 || idx > 255) D.Error(objVar.Path, "variable index out of byte range");
                else objectLocation = (byte)idx;
                objVar.Done();
            }

            if (node.Fields.OptBool("use_custom_icon")) flags |= 1;
            if (node.Fields.OptBool("push_head_continuation")) flags |= 4;
            if (node.Fields.OptBool("push_tail_continuation")) flags |= 128;

            io.Write(interaction);
            io.Write(objectLocation);
            io.Write(EnumByte(node, "priority", Names.PushPriorities, "inherited"));
            io.Write(flags);
            io.Write(ByteField(node, "icon_location"));
        }

        // VMFindBestObjectForFunctionOperand: uint16 Function
        private void FindBestObjectForFunction(PackNode node, BinaryWriter io)
        {
            io.Write(UShortField(node, "function"));
        }

        // VMSetMotiveChangeOperand: byte DeltaOwner, byte MaxOwner, byte Motive, byte Flags, int16 DeltaData, int16 MaxData
        private void SetMotiveDeltas(PackNode node, BinaryWriter io)
        {
            byte motive = 0;
            var motiveName = node.Fields.OptString("motive");
            if (motiveName != null && !Names.Motives.TryGetValue(motiveName, out motive))
                D.Error(node.Path + ".motive", "unknown motive \"" + motiveName + "\" (expected one of: " + Names.List(Names.Motives) + ")");

            var clearAll = node.Fields.OptBool("clear_all");
            if (motiveName == null && !clearAll)
                D.Error(node.Path, "missing required field \"motive\" (or set clear_all)");

            var delta = ScopedValue(node, "delta", false);
            var max = ScopedValue(node, "max", false);

            byte flags = 0;
            if (clearAll) flags |= 1;
            if (node.Fields.OptBool("once")) flags |= 2;

            io.Write(delta.Scope);
            io.Write(max.Scope);
            io.Write(motive);
            io.Write(flags);
            io.Write(delta.Data);
            io.Write(max.Data);
        }

        // VMTestObjectTypeOperand: uint32 GUID, int16 IdData, byte IdOwner
        // Field is "object_id", not "id" — "id" is taken by the node label.
        private void TestObjectType(PackNode node, BinaryWriter io)
        {
            var guid = node.Fields.OptGuid("guid");
            if (guid == null) D.Error(node.Path, "missing required field \"guid\"");
            var id = ScopedValue(node, "object_id", true);

            io.Write(guid ?? 0);
            io.Write(id.Data);
            io.Write(id.Scope);
        }

        // VMRandomNumberOperand: int16 DestData, uint16 DestScope, int16 RangeData, uint16 RangeScope
        // Note: scopes are 2 bytes here, unlike expression.
        private void RandomNumber(PackNode node, BinaryWriter io)
        {
            var dest = ScopedValue(node, "destination", true);
            var range = ScopedValue(node, "range", true);

            if (dest.Scope == Names.SCOPE_LITERAL)
                D.Warn(node.Path + ".destination", "random_number writes its result; literal destination will fail silently");

            io.Write(dest.Data);
            io.Write((ushort)dest.Scope);
            io.Write(range.Data);
            io.Write((ushort)range.Scope);
        }

        // VMRemoveObjectInstanceOperand: int16 Target (0 = me, else stack object), byte Flags
        private void RemoveObjectInstance(PackNode node, BinaryWriter io)
        {
            short target = 0;
            var targetName = node.Fields.OptString("target", "me");
            switch (targetName)
            {
                case "me": target = 0; break;
                case "stack_object": target = 1; break;
                default: D.Error(node.Path + ".target", "unknown target \"" + targetName + "\" (expected me, stack_object)"); break;
            }

            byte flags = 0;
            if (node.Fields.OptBool("return_immediately")) flags |= 1;
            if (node.Fields.OptBool("cleanup_all")) flags |= 2;

            io.Write(target);
            io.Write(flags);
        }

        // VMCreateObjectInstanceOperand: uint32 GUID, byte Position, byte Flags, byte LocalToUse, byte InteractionCallback
        private void CreateObjectInstance(PackNode node, BinaryWriter io)
        {
            var guid = node.Fields.OptGuid("guid");
            if (guid == null) D.Error(node.Path, "missing required field \"guid\"");

            byte position = 0;
            var posName = node.Fields.OptString("position", "in_front_of_me");
            if (!Names.CreateObjectPositions.TryGetValue(posName, out position))
                D.Error(node.Path + ".position", "unknown position \"" + posName + "\" (expected one of: " + string.Join(", ", Names.CreateObjectPositions.Keys) + ")");

            byte flags = 0;
            if (node.Fields.OptBool("no_duplicate")) flags |= 1;
            if (node.Fields.OptBool("pass_object_ids")) flags |= 2;
            if (node.Fields.OptBool("use_neighbor")) flags |= 4;
            if (node.Fields.OptBool("fail_if_non_empty")) flags |= 8;
            if (node.Fields.OptBool("pass_temp_0")) flags |= 16;
            if (node.Fields.OptBool("face_stack_obj_dir")) flags |= 32;

            var local = LocalRef(node.Fields, "local", node.Path);
            if (local < 0 || local > 255) D.Error(node.Path + ".local", "local index out of byte range");

            io.Write(guid ?? 0);
            io.Write(position);
            io.Write(flags);
            io.Write((byte)local);
            io.Write(ByteField(node, "interaction_callback"));
        }

        // VMChangeSuitOrAccessoryOperand: byte SuitData, byte SuitScope, uint16 Flags
        private void ChangeSuitOrAccessory(PackNode node, BinaryWriter io)
        {
            ushort flags = 0;
            if (node.Fields.OptBool("remove")) flags |= 1;
            if (node.Fields.OptBool("use_temp")) flags |= 2;
            if (node.Fields.OptBool("update")) flags |= 4;

            io.Write(ByteField(node, "suit"));
            io.Write(EnumByte(node, "scope", Names.SuitScopes, "person"));
            io.Write(flags);
        }

        // VMShowStringOperand: uint16 StringTable, uint16 StringID, byte Flags
        private void ShowString(PackNode node, BinaryWriter io)
        {
            byte flags = 0;
            if (node.Fields.OptBool("no_history")) flags |= 1;

            io.Write(UShortField(node, "table", 300));
            io.Write(UShortField(node, "string_id"));
            io.Write(flags);
        }

        // VMSetBalloonHeadlineOperand: uint16 Flags2, sbyte Index, byte Group, int16 Duration, byte Type, byte Flags
        private void SetBalloonHeadline(PackNode node, BinaryWriter io)
        {
            ushort flags2 = 0;
            if (node.Fields.OptBool("of_stack_obj")) flags2 |= 1;
            var algorithmic = LocalRef(node.Fields, "algorithmic", node.Path);
            flags2 |= (ushort)(algorithmic << 1);

            var index = node.Fields.OptInt("index");
            if (index < sbyte.MinValue || index > sbyte.MaxValue)
            {
                D.Error(node.Path + ".index", "value out of sbyte range");
                index = 0;
            }

            byte flags = 0;
            if (node.Fields.OptBool("inactive")) flags |= 1;
            if (node.Fields.OptBool("crossed")) flags |= 2;
            if (node.Fields.OptBool("backwards")) flags |= 4;
            if (node.Fields.OptBool("duration_in_loops")) flags |= 8;
            if (node.Fields.OptBool("indexed")) flags |= 16;

            io.Write(flags2);
            io.Write((sbyte)index);
            io.Write(EnumByte(node, "group", Names.BalloonGroups, "balloon"));
            io.Write(ShortField(node, "duration"));
            io.Write(ByteField(node, "type"));
            io.Write(flags);
        }
    }
}
