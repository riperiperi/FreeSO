using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Electron
{
    public enum ElectronPacketType
    {
        CreateASimResponse,
        Unknown
    }

    public static class ElectronPacketTypeUtils
    {
        public static ElectronPacketType FromPacketCode(ushort code)
        {
            switch (code)
            {
                case 0x0001:
                    return ElectronPacketType.CreateASimResponse;

                default:
                    return ElectronPacketType.Unknown;
            }
        }

        public static ushort GetPacketCode(this ElectronPacketType type)
        {
            switch (type)
            {
                case ElectronPacketType.CreateASimResponse:
                    return 0x0001;
            }

            return 0xFFFF;
        }
    }
}
