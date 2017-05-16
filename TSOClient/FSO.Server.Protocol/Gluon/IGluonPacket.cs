using FSO.Common.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Gluon
{
    public interface IGluonPacket : IoBufferDeserializable, IoBufferSerializable
    {
        GluonPacketType GetPacketType();
    }
}
