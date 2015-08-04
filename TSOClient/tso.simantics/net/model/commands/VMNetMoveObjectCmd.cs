using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using tso.world.model;
using TSO.Simantics.model;

namespace TSO.Simantics.net.model.commands
{
    public class VMNetMoveObjectCmd : VMNetCommandBodyAbstract
    {
        public short ObjectID;
        public short x;
        public short y;
        public sbyte level;
        public Direction dir;

        public override bool Execute(VM vm)
        {
            VMEntity obj = vm.GetObjectById(ObjectID);
            if (obj == null || (obj is VMAvatar)) return false;
            var result = obj.SetPosition(new LotTilePos(x, y, level), dir, vm.Context);
            if (result == VMPlacementError.Success)
            {
                obj.MultitileGroup.ExecuteEntryPoint(11, vm.Context); //User Placement
                return true;
            } else
            {
                return false;
            }
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(x);
            writer.Write(y);
            writer.Write(level);
            writer.Write((byte)dir);
        }

        public override void Deserialize(BinaryReader reader)
        {
            ObjectID = reader.ReadInt16();
            x = reader.ReadInt16();
            y = reader.ReadInt16();
            level = reader.ReadSByte();
            dir = (Direction)reader.ReadByte();
        }

        #endregion
    }
}
