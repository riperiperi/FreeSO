using FSO.Common.Model;
using FSO.Common.Serialization;
using Mina.Core.Buffer;
using System;
using System.IO;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class GlobalTuningUpdate : AbstractElectronPacket
    {
        public DynamicTuning Tuning;
        public byte[] ObjectUpgrades;
        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            
            var dataLen = input.GetInt32();
            if (dataLen > 4000000 || dataLen > input.Remaining) throw new Exception("Tuning too long");
            var data = new byte[dataLen];
            input.Get(data, 0, dataLen);
            using (var mem = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(mem))
                {
                    Tuning = new DynamicTuning(reader);
                }
            }
            var upgLen = input.GetInt32();
            if (upgLen > 10000000 || upgLen > input.Remaining) throw new Exception("Upgrades too long");
            ObjectUpgrades = new byte[upgLen];
            input.Get(ObjectUpgrades, 0, upgLen);
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.GlobalTuningUpdate;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            using (var mem = new MemoryStream())
            {
                using (var writer = new BinaryWriter(mem))
                {
                    Tuning.SerializeInto(writer);
                    var result = mem.ToArray();
                    output.PutInt32(result.Length);
                    output.Put(result, 0, result.Length);
                }
            }
            output.PutInt32(ObjectUpgrades.Length);
            output.Put(ObjectUpgrades, 0, ObjectUpgrades.Length);
        }
    }
}
