using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.files.utils;
using tso.simantics.engine.utils;
using tso.simantics.engine.scopes;
using Microsoft.Xna.Framework;

namespace tso.simantics.engine.primitives
{
    public class VMGotoRoutingSlot : VMPrimitiveHandler {
        public override VMPrimitiveExitCode Execute(VMStackFrame context)
        {
            var operand = context.GetCurrentOperand<VMGotoRoutingSlotOperand>();
            var slot = VMMemory.GetSlot(context, operand.Type, operand.Data);
            var obj = (VMGameObject)context.Callee;
            var avatar = (VMAvatar)context.Caller;

            //slot.Rsflags = tso.files.formats.iff.chunks.SLOTFlags.WEST;
            
            /**
             * Very little is kown about SLOTs so for now this is a place to dump comments
             * 
             * Slots measure proximity in units of 16. 16 = 1 tile away from the object.
             * Global slots are in global.iff in a slot table with ID 100.
             * global.iff also has a string table #257 which provides labels for the SLOTs
             */

            //Dont really know what 3 means, maybe relative to object?
            if (slot.Type == 3){
                var tilePosition = new Vector2(obj.Position.X, obj.Position.Y);
                var min = slot.MinProximity;
                var max = slot.MaxProximity;
                var desired = slot.OptimalProximity;

                if(max == 0){ max = min; }
                if(desired == 0){ desired = min; }

                var possibleTargets = VMRouteFinder.FindAvaliableLocations(tilePosition, slot.Rsflags, min, max, desired);
                if (possibleTargets.Count == 0){
                    return VMPrimitiveExitCode.GOTO_FALSE;
                }

                //TODO: Route finding and pick best route
                var target = possibleTargets[0];
                avatar.Position = new Vector3(target.Position.X + 0.5f, target.Position.Y + 0.5f, 0.0f);
            }

            return VMPrimitiveExitCode.GOTO_TRUE_NEXT_TICK;
        }
    }

    public class VMGotoRoutingSlotOperand : VMPrimitiveOperand
    {
        public ushort Data;
        public VMSlotScope Type;
        public byte Flags;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                Data = io.ReadUInt16();
                Type = (VMSlotScope)io.ReadUInt16();
                Flags = io.ReadByte();
            }
        }
        #endregion

        public override string ToString()
        {
            return "Go To Routing Slot (" + Type.ToString() + ": #" + Data + ")";
        }
    }

}
