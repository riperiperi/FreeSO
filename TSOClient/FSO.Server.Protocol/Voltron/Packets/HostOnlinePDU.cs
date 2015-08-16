using FSO.Server.Protocol.Utils;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public override IoBuffer Serialize()
        {
            IoBuffer result = Allocate(6);
            result.PutUInt16(HostReservedWords);
            result.PutUInt16(HostVersion);
            result.PutUInt16(ClientBufSize);
            return result;
        }

        public override void Deserialize(IoBuffer input)
        {
            HostReservedWords = input.GetUInt16();
            HostVersion = input.GetUInt16();
            ClientBufSize = input.GetUInt16();
        }
    }
}
