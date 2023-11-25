using System;

namespace FSO.Server.Protocol.Electron
{
    public enum ElectronPacketType : ushort
    {
        CreateASimResponse = 1,
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
        ChangeRoommateResponse,
        ModerationRequest,
        FSOVMProtocolMessage,
        AvatarRetireRequest,
        MailRequest,
        MailResponse,
        NhoodRequest,
        NhoodResponse,
        NhoodCandidateList,
        BulletinRequest,
        BulletinResponse,
        GlobalTuningUpdate,
        Unknown = 0xFFFF
    }

    public static class ElectronPacketTypeUtils
    {
        public static ElectronPacketType FromPacketCode(ushort code)
        {
            var result = (ElectronPacketType)code;
            if (Enum.IsDefined(typeof(ElectronPacketType), result))
                return result;
            else
                return ElectronPacketType.Unknown;
        }

        public static ushort GetPacketCode(this ElectronPacketType type)
        {
            return (ushort)type;
        }
    }
}
