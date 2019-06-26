using FSO.Common.Model;
using FSO.Common.Serialization;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class GlobalTuningUpdate : AbstractElectronPacket
    {
        public DynamicTuning Tuning;
        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            var dataLen = input.GetInt32(); //TODO: limits? 4MB is probably reasonable.
            var data = new byte[dataLen];
            input.Get(data, 0, dataLen);
            using (var mem = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(mem))
                {
                    Tuning = new DynamicTuning(reader);
                }
            }
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
        }
    }
}
