using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron
{
    public abstract class AbstractElectronPacket : IElectronPacket
    {
        public static IoBuffer Allocate(int size)
        {
            IoBuffer buffer = IoBuffer.Allocate(size);
            buffer.Order = ByteOrder.BigEndian;
            return buffer;
        }

        public abstract ElectronPacketType GetPacketType();
        public abstract void Deserialize(IoBuffer input);
        public abstract IoBuffer Serialize();
    }
}
