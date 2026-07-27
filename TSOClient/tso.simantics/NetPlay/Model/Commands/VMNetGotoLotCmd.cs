using FSO.Common.Model;
using FSO.LotView.Model;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Model.TSOPlatform;
using Microsoft.Xna.Framework;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetGotoLotCmd : VMNetCommandBodyAbstract
    {
        public ushort Interaction;
        public short Param0;

        public short x;
        public short y;
        public sbyte level;

        public uint LotLocation;

        private static uint TRANSITION_GUID = 0x746ED02B;

        private Point GetLotRelativeDirection(VM vm)
        {
            var myLocation = vm.TSOState.LotID;
            var myX = (short)(myLocation >> 16);
            var myY = (short)(myLocation);

            var targX = (short)(LotLocation >> 16);
            var targY = (short)(LotLocation);

            var cityRelative = new Point(targX - myX, targY - myY);

            return LotTransitionInfo.RelativeChangeCityToLot(cityRelative);
        }

        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (caller == null) return false;
            if (caller.Thread.Queue.Count >= VMThread.MAX_USER_ACTIONS) return false;

            // Try find an edge to place the transition destination
            var relativeDir = GetLotRelativeDirection(vm);

            LotTilePos target;

            if (relativeDir.X != 0 && relativeDir.Y != 0)
            {
                // This is a corner. There's only one place to go.

                short x = (short)(relativeDir.X == -1 ? 0 : vm.Context.Architecture.Width - 1);
                short y = (short)(relativeDir.Y == -1 ? 0 : vm.Context.Architecture.Height - 1);

                target = LotTilePos.FromBigTile(x, y, 1);
            }
            else
            {
                // Determine target point along the edge.
                bool isYEdge = relativeDir.Y != 0;
                bool isNegativeEdge = relativeDir.X == -1 || relativeDir.Y == -1;

                int myPerpAxis = isYEdge ? caller.Position.y : caller.Position.x;
                int perpLotSize = isYEdge ? vm.Context.Architecture.Height : vm.Context.Architecture.Width;
                int myDistToEdge = isNegativeEdge ? myPerpAxis : ((perpLotSize << 4) - myPerpAxis);

                int targPerpAxis = isYEdge ? y : x;
                int targDistToEdge = isNegativeEdge ? ((perpLotSize << 4) - targPerpAxis) : targPerpAxis;

                int perpAxisTarget = isNegativeEdge ? 8 : (perpLotSize << 4) - 8;

                int myAlongAxis = isYEdge ? caller.Position.x : caller.Position.y;
                int targAlongAxis = isYEdge ? x : y;

                float perpDistProportion = myDistToEdge / (float)(myDistToEdge + targDistToEdge);
                int alongAxisTarget = (int)(myAlongAxis * (1 - perpDistProportion) + targAlongAxis * perpDistProportion);

                target = new LotTilePos((short)(isYEdge ? alongAxisTarget : perpAxisTarget), (short)(isYEdge ? perpAxisTarget : alongAxisTarget), 1);
            }

            VMEntity callee = vm.Context.CreateObjectInstance(TRANSITION_GUID, target, Direction.NORTH).Objects[0];

            if (callee?.Position == LotTilePos.OUT_OF_WORLD)
            {
                callee.Delete(true, vm.Context);
                return false;
            }
            if (callee == null) return false;

            // Copy requested destination to the object

            callee.SetAttribute(1, (short)LotLocation); // lot id (low)
            callee.SetAttribute(2, (short)(LotLocation >> 16)); // lot id (high)
            callee.SetAttribute(3, x); // dest x
            callee.SetAttribute(4, y); // dest y

            callee.PushUserInteraction(Interaction, caller, vm.Context, false, new short[] { Param0, 0, 0, 0 });

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (!vm.TSOState.Flags.HasFlag(VMTSOLotStateFlags.AllowFreeRoam))
            {
                return false;
            }

            // Ensure the target lot is in range.
            var relativeDir = GetLotRelativeDirection(vm);

            if (relativeDir.X == 0 && relativeDir.Y == 0)
            {
                // That's this lot...
                return false;
            }

            if (Math.Abs(relativeDir.X) > 1 || Math.Abs(relativeDir.Y) > 1)
            {
                // That's too far away...
                return false;
            }

            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Interaction);
            writer.Write(Param0);
            writer.Write(x);
            writer.Write(y);
            writer.Write(level);
            writer.Write(LotLocation);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Interaction = reader.ReadUInt16();
            Param0 = reader.ReadInt16();
            x = reader.ReadInt16();
            y = reader.ReadInt16();
            level = reader.ReadSByte();
            LotLocation = reader.ReadUInt32();
        }

        #endregion
    }
}
