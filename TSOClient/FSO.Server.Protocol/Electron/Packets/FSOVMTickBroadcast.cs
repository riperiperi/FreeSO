using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class FSOVMTickBroadcast : AbstractElectronPacket
    {
        public byte[] Data;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            var dataLen = input.GetInt32(); //TODO: limits? 4MB is probably reasonable.
            Data = new byte[dataLen];
            input.Get(Data, 0, dataLen);
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.FSOVMTickBroadcast;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutInt32(Data.Length);
            output.Put(Data, 0, Data.Length);
        }
    }
}
