using System.IO;
using FSO.LotView.Model;
using FSO.SimAntics.Engine;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetGotoCmd : VMNetCommandBodyAbstract
    {
        public ushort Interaction;
        public short Param0;

        public short x;
        public short y;
        public sbyte level;

        private static uint GOTO_GUID = 0x000007C4;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (caller == null) return false;
            if (caller.Thread.Queue.Count >= VMThread.MAX_USER_ACTIONS) return false;
            VMEntity callee = vm.Context.CreateObjectInstance(GOTO_GUID, new LotTilePos(x, y, level), Direction.NORTH).Objects[0];
            if (callee?.Position == LotTilePos.OUT_OF_WORLD) callee.Delete(true, vm.Context);
            if (callee == null) return false;
            callee.PushUserInteraction(Interaction, caller, vm.Context, false, new short[] { Param0, 0, 0, 0 });

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
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Interaction = reader.ReadUInt16();
            Param0 = reader.ReadInt16();
            x = reader.ReadInt16();
            y = reader.ReadInt16();
            level = reader.ReadSByte();
        }

        #endregion
    }
}
