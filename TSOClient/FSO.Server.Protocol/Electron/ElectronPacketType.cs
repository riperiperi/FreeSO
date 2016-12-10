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
        PurchaseLotRequest,
        PurchaseLotResponse,
        InstantMessage,
        FindLotRequest,
        FindLotResponse,
        FSOVMTickBroadcast,
        FSOVMDirectToClient,
        FSOVMCommand,
        FindAvatarRequest,
        FindAvatarResponse,
        ChangeRoommateRequest,
        KeepAlive,
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
                case 0x0002:
                    return ElectronPacketType.PurchaseLotRequest;
                case 0x0003:
                    return ElectronPacketType.PurchaseLotResponse;
                case 0x0004:
                    return ElectronPacketType.InstantMessage;
                case 0x0005:
                    return ElectronPacketType.FindLotRequest;
                case 0x0006:
                    return ElectronPacketType.FindLotResponse;
                case 0x0007:
                    return ElectronPacketType.FSOVMTickBroadcast;
                case 0x0008:
                    return ElectronPacketType.FSOVMDirectToClient;
                case 0x0009:
                    return ElectronPacketType.FSOVMCommand;
                case 0x000A:
                    return ElectronPacketType.FindAvatarRequest;
                case 0x000B:
                    return ElectronPacketType.FindAvatarResponse;
                case 0x000C:
                    return ElectronPacketType.ChangeRoommateRequest;
                case 0x000D:
                    return ElectronPacketType.KeepAlive;
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
                case ElectronPacketType.PurchaseLotRequest:
                    return 0x0002;
                case ElectronPacketType.PurchaseLotResponse:
                    return 0x0003;
                case ElectronPacketType.InstantMessage:
                    return 0x0004;
                case ElectronPacketType.FindLotRequest:
                    return 0x0005;
                case ElectronPacketType.FindLotResponse:
                    return 0x0006;
                case ElectronPacketType.FSOVMTickBroadcast:
                    return 0x0007;
                case ElectronPacketType.FSOVMDirectToClient:
                    return 0x0008;
                case ElectronPacketType.FSOVMCommand:
                    return 0x0009;
                case ElectronPacketType.FindAvatarRequest:
                    return 0x000A;
                case ElectronPacketType.FindAvatarResponse:
                    return 0x000B;
                case ElectronPacketType.ChangeRoommateRequest:
                    return 0x000C;
                case ElectronPacketType.KeepAlive:
                    return 0x000D;
            }

            return 0xFFFF;
        }
    }
}
