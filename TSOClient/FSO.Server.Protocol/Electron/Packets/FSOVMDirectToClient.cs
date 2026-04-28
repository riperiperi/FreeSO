using System;
using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class FSOVMDirectToClient : AbstractElectronPacket
    {
        public byte[] Data;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            var dataLen = input.GetInt32();
            if (dataLen < 0 || dataLen > 4 * 1024 * 1024)
                throw new Exception("FSOVMDirectToClient data too large: " + dataLen);
            Data = new byte[dataLen];
            input.Get(Data, 0, dataLen);
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.FSOVMDirectToClient;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutInt32(Data.Length);
            output.Put(Data, 0, Data.Length);
        }
    }
}
