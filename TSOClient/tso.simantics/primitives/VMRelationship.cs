/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using FSO.SimAntics.Engine.Utils;
using FSO.SimAntics.Engine.Scopes;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMRelationship : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMRelationshipOperand)args;

            VMEntity obj1;
            VMEntity obj2;

            switch (operand.Mode)
            {
                case 0: //from me to stack object
                    obj1 = context.Caller;
                    obj2 = context.StackObject;
                    break;
                case 1: //from stack object to me
                    obj1 = context.StackObject;
                    obj2 = context.Caller;
                    break;
                case 2: //from stack object to object in local/stack param
                    obj1 = context.StackObject;
                    obj2 = context.VM.GetObjectById((operand is VMOldRelationshipOperand) ? context.Args[0] : context.Locals[operand.Local]);
                    break;
                case 3: //from object in local/stack param to stack object
                    obj1 = context.VM.GetObjectById((operand is VMOldRelationshipOperand) ? context.Args[0] : context.Locals[operand.Local]);
                    obj2 = context.StackObject;
                    break;
                default:
                    throw new VMSimanticsException("Invalid relationship type!", context);
            }

            var ts1 = context.VM.TS1;
            if ((obj1 == null || obj2 == null) && !(ts1 && operand.UseNeighbor)) return VMPrimitiveExitCode.GOTO_TRUE;
            List<short> relToTarg;

            var myNID = (obj1 as VMAvatar)?.GetPersonData(Model.VMPersonDataVariable.NeighborId) ?? 0;
            var targNID = (obj2 as VMAvatar)?.GetPersonData(Model.VMPersonDataVariable.NeighborId) ?? 0;

            if (!ts1 && (obj2.PersistID > 0))
            {
                //use persist matrix whenever possible.
                //ignores use neighbour flag so we can use str/ltr.
                var rels = obj1.MeToPersist;
                var targId = obj2.PersistID;
                if (!rels.ContainsKey(targId))
                {
                    if (operand.FailIfTooSmall) return VMPrimitiveExitCode.GOTO_FALSE;
                    else rels.Add(targId, new List<short>());
                }
                if (operand.SetMode > 0) obj1.ChangedRels.Add(targId);
                relToTarg = rels[targId];
            }
            else if (ts1 && (operand.UseNeighbor || (myNID > 0 && targNID > 0)))
            {
                //ts1 neighbour matrix
                if (operand.UseNeighbor)
                {
                    switch (operand.Mode)
                    {
                        case 0: //from me to stack object
                            myNID = ((VMAvatar)context.Caller).GetPersonData(Model.VMPersonDataVariable.NeighborId);
                            targNID = context.StackObjectID;
                            break;
                        case 1: //from stack object to me
                            myNID = context.StackObjectID;
                            targNID = ((VMAvatar)context.Caller).GetPersonData(Model.VMPersonDataVariable.NeighborId);
                            break;
                        case 2: //from stack object to object in local/stack param
                            myNID = context.StackObjectID;
                            targNID = (operand is VMOldRelationshipOperand) ? context.Args[0] : context.Locals[operand.Local];
                            break;
                        case 3: //from object in local/stack param to stack object
                            myNID = (operand is VMOldRelationshipOperand) ? context.Args[0] : context.Locals[operand.Local];
                            targNID = context.StackObjectID;
                            break;
                        default:
                            throw new VMSimanticsException("Invalid relationship type!", context);
                    }
                }

                var rels = Content.Content.Get().Neighborhood.GetNeighborByID(myNID).Relationships;
                if (!rels.ContainsKey(targNID))
                {
                    if (operand.FailIfTooSmall) return VMPrimitiveExitCode.GOTO_FALSE;
                    else rels.Add(targNID, new List<short>());
                }
                relToTarg = rels[targNID];
            }
            else
            {
                var rels = obj1.MeToObject;
                var targId = (ushort)obj2.ObjectID;
                //check if exists
                if (!rels.ContainsKey(targId))
                {
                    if (operand.FailIfTooSmall) return VMPrimitiveExitCode.GOTO_FALSE;
                    else rels.Add(targId, new List<short>());
                }
                relToTarg = rels[targId];
            }

            if (relToTarg.Count <= operand.RelVar)
            {
                if (operand.FailIfTooSmall) return VMPrimitiveExitCode.GOTO_FALSE;
                else
                {
                    while (relToTarg.Count <= operand.RelVar) relToTarg.Add(0);
                }
            }

            //todo: move to tuning?
            var diffMultiplier = context.VM.Tuning.GetTuning("category_mul", 0, context.VM.TSOState.PropertyCategory) ?? 1f; //0: relationship, 1: skill/money, 2: visitor hour scale

            if (operand.SetMode == 0)
            {
                VMMemory.SetVariable(context, operand.VarScope, operand.VarData, relToTarg[operand.RelVar]);
            }
            else if (operand.SetMode == 1)
            {
                var value = VMMemory.GetVariable(context, operand.VarScope, operand.VarData);
                relToTarg[operand.RelVar] = Math.Max((short)-100, Math.Min((short)100, value));
            }
            else if (operand.SetMode == 2)
            {
                var value = VMMemory.GetVariable(context, operand.VarScope, operand.VarData);
                relToTarg[operand.RelVar] += (short)(value * diffMultiplier);
                relToTarg[operand.RelVar] = Math.Max((short)-100, Math.Min((short)100, relToTarg[operand.RelVar]));
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMOldRelationshipOperand : VMRelationshipOperand
    {
        private byte GetSet;
        //clever tricks to avoid coding the same thing twice ;)
        public override void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                GetSet = io.ReadByte();
                RelVar = io.ReadByte();
                VarScope = VMVariableScope.Parameters;
                VarData = io.ReadByte(); //parameter number
                Mode = io.ReadByte();
                Flags = io.ReadByte();
            }
        }

        public override void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(GetSet);
                io.Write(RelVar);
                io.Write((byte)VarData);
                io.Write(Mode);
                io.Write(Flags);
            }
        }

        public override bool UseNeighbor
        {
            get { return (Flags & 2) == 2; }
            set
            {
                if (value) Flags |= 2;
                else Flags &= unchecked((byte)~2);
            }
        }

        public override bool FailIfTooSmall
        {
            get { return (Flags & 1) == 1; }
            set
            {
                if (value) Flags |= 1;
                else Flags &= unchecked((byte)~1);
            }
        }

        public override int SetMode
        {
            get { return GetSet; }
            set
            {
                GetSet = (byte)value;
            }
        }
    }

    public class VMRelationshipOperand : VMPrimitiveOperand
    {
        public byte RelVar { get; set; }
        public byte Mode { get; set; }
        public byte Flags;
        public byte Local { get; set; }
        public VMVariableScope VarScope { get; set; }
        public short VarData { get; set; }

        #region VMPrimitiveOperand Members
        public virtual void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                RelVar = io.ReadByte();
                Mode = io.ReadByte();
                Flags = io.ReadByte();
                Local = io.ReadByte();
                VarScope = (VMVariableScope)io.ReadUInt16();
                VarData = io.ReadInt16();
            }
        }

        public virtual void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(RelVar);
                io.Write(Mode);
                io.Write(Flags);
                io.Write(Local);
                io.Write((ushort)VarScope);
                io.Write(VarData);
            }
        }
        #endregion

        public virtual bool UseNeighbor
        {
            get { return (Flags & 2) == 2; }
            set
            {
                if (value) Flags |= 2;
                else Flags &= unchecked((byte)~2);
            }
        }

        public virtual bool FailIfTooSmall
        {
            get { return (Flags & 1) == 1; }
            set
            {
                if (value) Flags |= 1;
                else Flags &= unchecked((byte)~1);
            }
        }

        public virtual int SetMode
        { 
            get { return ((Flags >> 2) & 1) | ((Flags >> 4)&2); }
            set
            {
                Flags &= unchecked((byte)~36);
                Flags |= (byte)((value&1) << 1);
                Flags |= (byte)((value & 2) << 4);
            }
        }
    }
}
