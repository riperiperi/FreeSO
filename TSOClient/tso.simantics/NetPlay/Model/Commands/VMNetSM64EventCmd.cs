using FSO.LotView.Components;
using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    internal class VMNetSM64EventCmd : VMNetCommandBodyAbstract
    {
        public int EventType;
        public uint EventValue;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            // Tell the SM64 component about this sim's mario instance.
            if (caller == null || caller.WorldUI == null || !(caller.WorldUI is AvatarComponent)) return false;

            if (caller.PersistID != vm.MyUID)
            {
                vm.Context.Blueprint.SM64?.PlaySound((AvatarComponent)caller.WorldUI, EventValue);
            }

            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);

            writer.Write(EventType);
            writer.Write(EventValue);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            EventType = reader.ReadInt32();
            EventValue = reader.ReadUInt32();
        }

        #endregion
    }
}
