using FSO.Server.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Electron
{
    public interface IElectronPacket : IoBufferDeserializable, IoBufferSerializable
    {
        ElectronPacketType GetPacketType();
    }
}
