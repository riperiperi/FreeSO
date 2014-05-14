using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.engine;
using TSO.Files.utils;
using TSO.Simantics.engine.scopes;
using TSO.Simantics.engine.utils;
using TSO.Files.formats.iff.chunks;

namespace TSO.Simantics.primitives
{
    public class VMChangeSuitOrAccessory : VMPrimitiveHandler {
        public override VMPrimitiveExitCode Execute(VMStackFrame context){
            var operand = context.GetCurrentOperand<VMChangeSuitOrAccessoryOperand>();

            var avatar = (VMAvatar)context.Caller;



            if ((operand.Flags & VMChangeSuitOrAccessoryFlags.Update) == VMChangeSuitOrAccessoryFlags.Update)
            { //update outfit with outfit in stringset 304 with index in temp 0
                avatar.BodyOutfit = Convert.ToUInt64(context.Callee.Object.Resource.Get<STR>(304).GetString((context.Thread.TempRegisters[0])), 16);
            } 
            else 
            {
                var suit = VMMemory.GetSuit(context, operand.SuitScope, operand.SuitData);
                if(suit == null){
                    return VMPrimitiveExitCode.GOTO_TRUE;
                }

                if ((operand.Flags & VMChangeSuitOrAccessoryFlags.Remove) == VMChangeSuitOrAccessoryFlags.Remove)
                {
                    avatar.Avatar.RemoveAccessory(suit);
                }
                else
                {
                    avatar.Avatar.AddAccessory(suit);
                }
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMChangeSuitOrAccessoryOperand : VMPrimitiveOperand {

        public byte SuitData;
        public VMSuitScope SuitScope;
        public VMChangeSuitOrAccessoryFlags Flags;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                SuitData = io.ReadByte();
                SuitScope = (VMSuitScope)io.ReadByte();
                Flags = (VMChangeSuitOrAccessoryFlags)io.ReadUInt16();
            }
        }
        #endregion
    }

    [Flags]
    public enum VMChangeSuitOrAccessoryFlags
    {
        Remove = 1,
        Update = 4
    }
}
