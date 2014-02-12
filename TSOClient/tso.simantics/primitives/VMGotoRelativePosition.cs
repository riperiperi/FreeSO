using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.simantics.engine;
using tso.files.utils;
using Microsoft.Xna.Framework;
using tso.world;

namespace tso.simantics.primitives
{
    public class VMGotoRelativePosition : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context){
            var operand = context.GetCurrentOperand<VMGotoRelativePositionOperand>();
            
            var obj = (VMGameObject)context.Callee;
            var avatar = (VMAvatar)context.Caller;

            /** 
             * Examples for reference
             * Fridge - Have Snack - In front of, facing
             */
            if (operand.Location == VMGotoRelativeLocation.InFrontOf){
                /** Need to work out which side is in front? **/

                //TODO: My positions are wrong, what i call left front etc is wrong. Need to correct this eventually
                var location = obj.Position;
                switch(obj.Direction){
                    case tso.world.model.Direction.LeftFront:
                        location += new Vector3(0.0f, 1.0f, 0.0f);
                        break;
                    case tso.world.model.Direction.LeftBack:
                        location += new Vector3(-1.0f, 0.0f, 0.0f);
                        break;
                    case tso.world.model.Direction.RightFront:
                        location += new Vector3(1.0f, 0.0f, 0.0f);
                        break;
                    case tso.world.model.Direction.RightBack:
                        location += new Vector3(0.0f, -1.0f, 0.0f);
                        break;
                }
                avatar.Position = location + new Vector3(0.5f, 0.5f, 0.0f);
                return VMPrimitiveExitCode.GOTO_TRUE_NEXT_TICK;
            }
            throw new Exception("Unknown goto relative");
        }
    }

    public class VMGotoRelativePositionOperand : VMPrimitiveOperand
    {
        /** How long to meander around objects **/
        public ushort OldTrapCount;
        public VMGotoRelativeLocation Location;
        public VMGotoRelativeDirection Direction;
        public ushort RouteCount;
        public VMGotoRelativeFlags Flags;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes){
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                OldTrapCount = io.ReadUInt16();
                Location = (VMGotoRelativeLocation)((sbyte)io.ReadByte());
                Direction = (VMGotoRelativeDirection)((sbyte)io.ReadByte());
                RouteCount = io.ReadUInt16();
                Flags = (VMGotoRelativeFlags)io.ReadByte();
            }
        }
        #endregion
    }

    public enum VMGotoRelativeDirection
    {
        Facing = -2,
        AnyDirection = -1,
        SameDirection = 0,
        FortyFiveDegreesRightOfSameDirection = 1,
        NinetyDegreesRightOfSameDirection = 2,
        FortyFiveDegreesLeftOfOpposingDirection = 3,
        OpposingDirection = 4,
        FortyFiveDegreesRightOfOpposingDirection = 5,
        NinetyDegreesRightOfOpposingDirection = 6,
        FortyFiveDegreesLeftOfSameDirection = 7
    }

    public enum VMGotoRelativeLocation {
        OnTopOf = -2,
        AnywhereNear = -1,
        InFrontOf = 0,
        FrontAndToRightOf = 1,
        ToTheRightOf = 2,
        BehindAndToRightOf = 3,
        Behind = 4,
        BehindAndToTheLeftOf = 5,
        ToTheLeftOf = 6,
        InFrontAndToTheLeftOf = 7
    }

    [Flags]
    public enum VMGotoRelativeFlags
    {
        RequireSameAltitude = 0x2
    }
}
