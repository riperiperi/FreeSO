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

            var rels = obj1.MeToObject;
            var targId = (ushort)obj2.ObjectID;

            //check if exists
            if (!rels.ContainsKey(targId))
            {
                if (operand.FailIfTooSmall) return VMPrimitiveExitCode.GOTO_FALSE;
                else rels.Add(targId, new List<short>());
            }
            if (rels[targId].Count <= operand.RelVar)
            {
                if (operand.FailIfTooSmall) return VMPrimitiveExitCode.GOTO_FALSE;
                else {
                    while (rels[targId].Count <= operand.RelVar) rels[targId].Add(0);
                }
            }

            if (operand.SetMode == 1)
            { //todo, special system for server persistent avatars and pets
                var value = VMMemory.GetVariable(context, operand.VarScope, operand.VarData);
                rels[targId][operand.RelVar] = value;
            }
            else if (operand.SetMode == 2)
            {
                var value = VMMemory.GetVariable(context, operand.VarScope, operand.VarData);
                rels[targId][operand.RelVar] += value;
            }
            else if (operand.SetMode == 0)
            {
                VMMemory.SetVariable(context, operand.VarScope, operand.VarData, rels[targId][operand.RelVar]);
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
            get { return (Flags & 1) == 1; }
            set
            {
                if (value) Flags |= 1;
                else Flags &= unchecked((byte)~1);
            }
        }

        public virtual bool FailIfTooSmall
        {
            get { return (Flags & 8) == 8; }
            set
            {
                if (value) Flags |= 8;
                else Flags &= unchecked((byte)~8);
            }
        }

        public virtual int SetMode
        { 
            get { return (Flags >> 1) & 3; }
            set
            {
                Flags &= unchecked((byte)~6);
                Flags |= (byte)(value << 1);
            }
        }
    }
}
