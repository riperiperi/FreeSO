using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetAdjHollowSyncCmd : VMNetCommandBodyAbstract
    {
        public byte[][] HollowAdj;

        public override bool AcceptFromClient { get { return false; } }

        public override bool Execute(VM vm)
        {
            vm.HollowAdj = HollowAdj;
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
            writer.Write(HollowAdj.Length);
            foreach (var item in HollowAdj)
            {
                if (item == null) writer.Write(false);
                else
                {
                    writer.Write(true);
                    writer.Write(item.Length);
                    writer.Write(item);
                }
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            HollowAdj = new byte[reader.ReadInt32()][];
            for (int i=0; i<HollowAdj.Length; i++)
            {
                if (reader.ReadBoolean()) HollowAdj[i] = reader.ReadBytes(reader.ReadInt32());
            }
        }

        #endregion
    }
}
