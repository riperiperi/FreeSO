using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class HostOnlinePDU : AbstractVoltronPacket
    {
        public ushort HostReservedWords;
        public ushort HostVersion;
        public ushort ClientBufSize = 4096;

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.HostOnlinePDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            //IoBuffer result = Allocate(6);
            output.PutUInt16(HostReservedWords);
            output.PutUInt16(HostVersion);
            output.PutUInt16(ClientBufSize);
            //return result;
        }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            HostReservedWords = input.GetUInt16();
            HostVersion = input.GetUInt16();
            ClientBufSize = input.GetUInt16();
        }
    }
}
