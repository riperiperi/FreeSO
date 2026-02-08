using FSO.SimAntics.Utils;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public enum VMHollowAdjType : byte
    {
        None = 0,
        Reuse = 1,
        Terrain = 2,
        Hollow = 3,
    }

    public struct VMHollowAdjEntry
    {
        public VMHollowAdjType Type;
        public byte[] Data;

        public VMHollowAdjEntry(VMHollowAdjType type, byte[] data)
        {
            Type = type;
            Data = data;
        }

        public VMHollowAdjEntry(VMHollowAdjType type)
        {
            Type = type;
            Data = null;
        }
    }

    public class VMNetAdjHollowSyncCmd : VMNetCommandBodyAbstract
    {
        public VMHollowAdjEntry[] HollowAdj;

        public override bool AcceptFromClient { get { return false; } }

        public override bool Execute(VM vm)
        {
            if (vm.HollowAdj == null)
            {
                vm.HollowAdj = HollowAdj;
            }
            else
            {
                bool changed = false;
                for (int i = 0; i < HollowAdj.Length; i++)
                {
                    var toCopy = HollowAdj[i];
                    ref var target = ref vm.HollowAdj[i];

                    if (toCopy.Type == VMHollowAdjType.Hollow)
                    {
                        target = toCopy;
                        changed = true;
                    }
                }

                if (VM.UseWorld && vm.Ready && !vm.FSOVAsyncLoading && !vm.Driver.RunningCatchup && changed && vm.Context.Blueprint.SubWorlds.Count != 0)
                {
                    VMLotTerrainRestoreTools.RestoreSurroundings(vm, HollowAdj);
                }
            }
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
                writer.Write((byte)item.Type);
                if (item.Type >= VMHollowAdjType.Hollow)
                {
                    writer.Write(item.Data.Length);
                    writer.Write(item.Data);
                }
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            HollowAdj = new VMHollowAdjEntry[reader.ReadInt32()];
            for (int i=0; i<HollowAdj.Length; i++)
            {
                var type = (VMHollowAdjType)reader.ReadByte();
                var data = type >= VMHollowAdjType.Hollow ? reader.ReadBytes(reader.ReadInt32()) : null;

                HollowAdj[i] = new VMHollowAdjEntry(type, data);
            }
        }

        #endregion
    }
}
