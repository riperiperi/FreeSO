using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetSM64AnimDataCmd : VMNetCommandBodyAbstract
    {
        public byte[] AnimData;

        public override bool AcceptFromClient { get { return false; } }

        public override bool Execute(VM vm, VMAvatar caller)
        {
            LotView.Components.SM64Component.SetAnimData(AnimData);

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return !FromNet;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);

            writer.Write(AnimData.Length);
            writer.Write(AnimData);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            int length = reader.ReadInt32();
            AnimData = reader.ReadBytes(length);
        }

        #endregion
    }
}
