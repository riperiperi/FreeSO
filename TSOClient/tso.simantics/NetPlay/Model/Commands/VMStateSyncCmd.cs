using FSO.SimAntics.Marshals;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMStateSyncCmd : VMNetCommandBodyAbstract
    {
        public VMMarshal State;

        public override bool Execute(VM vm)
        {
            vm.Load(State);
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return !FromNet;
        }

        #region VMSerializable Members
        public override void Deserialize(BinaryReader reader)
        {
            State = new VMMarshal();
            using (var decomp = new GZipStream(reader.BaseStream, CompressionMode.Decompress))
            {
                using (var bin = new BinaryReader(decomp))
                {
                    State.Deserialize(bin);
                }
            }
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            using (var comp = new GZipStream(writer.BaseStream, CompressionMode.Compress))
            {
                using (var bin = new BinaryWriter(comp))
                {
                    State.SerializeInto(bin);
                }
            }
        }
        #endregion
    }
}
