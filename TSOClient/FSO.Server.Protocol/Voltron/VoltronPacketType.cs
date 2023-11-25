namespace FSO.Server.Protocol.Voltron
{
    public enum VoltronPacketType
    {
        AlertHandledPDU,
        AlertMsgPDU,
        AlertMsgResponsePDU,
        AnnouncementMsgResponsePDU,
        AnnouncementMsgPDU,
        ClientByePDU,
        ServerByePDU,
        ChatMsgFailedPDU,
        ChatMsgPDU,
        ClientOnlinePDU,
        CreateAndJoinRoomFailedPDU,
        CreateAndJoinRoomPDU,
        CreateRoomPDU,
        CreateRoomResponsePDU,
        DestroyRoomPDU,
        DestroyRoomResponsePDU,
        DetachFromRoomFailedPDU,
        DetachFromRoomPDU,
        EjectOccupantPDU,
        EjectOccupantResponsePDU,
        ErrorPDU,
        ExitRoomFailedPDU,
        ExitRoomPDU,
        FindPlayerPDU,
        FindPlayerResponsePDU,
        FlashMsgResponsePDU,
        FlashMsgPDU,
        HandleAlertPDU,
        HostOfflinePDU,
        HostOnlinePDU,
        InvitationMsgResponsePDU,
        InvitationMsgPDU,
        JoinPlayerFailedPDU,
        JoinPlayerPDU,
        JoinRoomFailedPDU,
        JoinRoomPDU,
        ListOccupantsPDU,
        ListOccupantsResponsePDU,
        ListRoomsPDU,
        ListRoomsResponsePDU,
        LogEventPDU,
        LogEventResponsePDU,
        MessageLostPDU,
        OccupantArrivedPDU,
        OccupantDepartedPDU,
        ReadProfilePDU,
        ReadProfileResponsePDU,
        ReleaseProfilePDU,
        ReleaseProfileResponsePDU,
        SetAcceptAlertsPDU,
        SetAcceptAlertsResponsePDU,
        SetIgnoreListPDU,
        SetIgnoreListResponsePDU,
        SetInvinciblePDU,
        SetInvincibleResponsePDU,
        SetInvisiblePDU,
        SetInvisibleResponsePDU,
        SetRoomNamePDU,
        SetRoomNameResponsePDU,
        UpdateOccupantsPDU,
        UpdatePlayerPDU,
        UpdateProfilePDU,
        UpdateRoomPDU,
        YankPlayerFailedPDU,
        YankPlayerPDU,
        SetAcceptFlashesPDU,
        SetAcceptFlashesResponsePDU,
        SplitBufferPDU,
        ActionRoomNamePDU,
        ActionRoomNameResponsePDU,
        NotifyRoomActionedPDU,
        ModifyProfilePDU,
        ModifyProfileResponsePDU,
        ListBBSFoldersPDU,
        ListBBSFoldersResponsePDU,
        GetBBSMessageListPDU,
        GetBBSMessageListResponsePDU,
        PostBBSMessagePDU,
        PostBBSReplyPDU,
        PostBBSMessageResponsePDU,
        GetMPSMessagesPDU,
        GetMPSMessagesResponsePDU,
        DeleteMPSMessagePDU,
        DeleteMPSMessageResponsePDU,
        BBSMessageDataPDU,
        UpdateRoomAdminListPDU,
        GetRoomAdminListPDU,
        GetRoomAdminListResponsePDU,
        GroupInfoRequestPDU,
        GroupInfoResponsePDU,
        GroupAdminRequestPDU,
        GroupAdminResponsePDU,
        GroupMembershipRequestPDU,
        GroupMembershipResponsePDU,
        FlashGroupPDU,
        FlashGroupResponsePDU,
        UpdateGroupMemberPDU,
        UpdateGroupMemberResponsePDU,
        UpdateGroupAdminPDU,
        UpdateGroupAdminResponsePDU,
        ListGroupsPDU,
        ListGroupsResponsePDU,
        ListJoinedGroupsPDU,
        ListJoinedGroupsResponsePDU,
        GpsChatPDU,
        GpsChatResponsePDU,
        PetitionStatusUpdatePDU,
        LogGPSPetitionPDU,
        LogGPSPetitionResponsePDU,
        List20RoomsPDU,
        List20RoomsResponsePDU,
        UpdateIgnoreListPDU,
        ResetWatchdogPDU,
        ResetWatchdogResponsePDU,
        BroadcastDataBlobPDU,
        TransmitDataBlobPDU,
        DBRequestWrapperPDU,
        TransmitCreateAvatarNotificationPDU,
        BC_PlayerLoginEventPDU,
        BC_PlayerLogoutEventPDU,
        RoomserverUserlistPDU,
        LotEntryRequestPDU,
        ClientConfigPDU,
        KickoutRoommatePDU,
        GenericFlashPDU,
        GenericFlashRequestPDU,
        GenericFlashResponsePDU,
        TransmitGenericGDMPDU,
        EjectAvatarPDU,
        TestPDU,
        HouseSimConstraintsPDU,
        HouseSimConstraintsResponsePDU,
        LoadHouseResponsePDU,
        ComponentVersionRequestPDU,
        ComponentVersionResponsePDU,
        InviteRoommatePDU,
        RoommateInvitationAnswerPDU,
        RoommateGDMPDU,
        HSB_ShutdownSimulatorPDU,
        RoommateGDMResponsePDU,
        RSGZWrapperPDU,
        AvatarHasNewLotIDPDU,
        CheatPDU,
        DataServiceWrapperPDU,
        CsrEjectAvatarPDU,
        CsrEjectAvatarResponsePDU,
        cTSONetMessagePDU,
        LogCsrActionPDU,
        LogAvatarActionPDU,

        Unknown
    }

    public static class VoltronPacketTypeUtils
    {
        public static VoltronPacketType FromPacketCode(ushort code)
        {
            switch (code)
            {
                case 0x0001:
                    return VoltronPacketType.AlertHandledPDU;
                case 0x0002:
                    return VoltronPacketType.AlertMsgPDU;
                case 0x0003:
                    return VoltronPacketType.AlertMsgResponsePDU;
                case 0x0004:
                    return VoltronPacketType.AnnouncementMsgResponsePDU;
                case 0x0005:
                    return VoltronPacketType.AnnouncementMsgPDU;
                case 0x0006:
                    return VoltronPacketType.ClientByePDU;
                case 0x0007:
                    return VoltronPacketType.ServerByePDU;
                case 0x0008:
                    return VoltronPacketType.ChatMsgFailedPDU;
                case 0x0009:
                    return VoltronPacketType.ChatMsgPDU;
                case 0x000a:
                    return VoltronPacketType.ClientOnlinePDU;
                case 0x000b:
                    return VoltronPacketType.CreateAndJoinRoomFailedPDU;
                case 0x000c:
                    return VoltronPacketType.CreateAndJoinRoomPDU;
                case 0x000d:
                    return VoltronPacketType.CreateRoomPDU;
                case 0x000e:
                    return VoltronPacketType.CreateRoomResponsePDU;
                case 0x000f:
                    return VoltronPacketType.DestroyRoomPDU;
                case 0x0010:
                    return VoltronPacketType.DestroyRoomResponsePDU;
                case 0x0011:
                    return VoltronPacketType.DetachFromRoomFailedPDU;
                case 0x0012:
                    return VoltronPacketType.DetachFromRoomPDU;
                case 0x0013:
                    return VoltronPacketType.EjectOccupantPDU;
                case 0x0014:
                    return VoltronPacketType.EjectOccupantResponsePDU;
                case 0x0015:
                    return VoltronPacketType.ErrorPDU;
                case 0x0016:
                    return VoltronPacketType.ExitRoomFailedPDU;
                case 0x0017:
                    return VoltronPacketType.ExitRoomPDU;
                case 0x0018:
                    return VoltronPacketType.FindPlayerPDU;
                case 0x0019:
                    return VoltronPacketType.FindPlayerResponsePDU;
                case 0x001a:
                    return VoltronPacketType.FlashMsgResponsePDU;
                case 0x001b:
                    return VoltronPacketType.FlashMsgPDU;
                case 0x001c:
                    return VoltronPacketType.HandleAlertPDU;
                case 0x001d:
                    return VoltronPacketType.HostOfflinePDU;
                case 0x001e:
                    return VoltronPacketType.HostOnlinePDU;
                case 0x001f:
                    return VoltronPacketType.InvitationMsgResponsePDU;
                case 0x0020:
                    return VoltronPacketType.InvitationMsgPDU;
                case 0x0021:
                    return VoltronPacketType.JoinPlayerFailedPDU;
                case 0x0022:
                    return VoltronPacketType.JoinPlayerPDU;
                case 0x0023:
                    return VoltronPacketType.JoinRoomFailedPDU;
                case 0x0024:
                    return VoltronPacketType.JoinRoomPDU;
                case 0x0025:
                    return VoltronPacketType.ListOccupantsPDU;
                case 0x0026:
                    return VoltronPacketType.ListOccupantsResponsePDU;
                case 0x0027:
                    return VoltronPacketType.ListRoomsPDU;
                case 0x0028:
                    return VoltronPacketType.ListRoomsResponsePDU;
                case 0x0029:
                    return VoltronPacketType.LogEventPDU;
                case 0x002a:
                    return VoltronPacketType.LogEventResponsePDU;
                case 0x002b:
                    return VoltronPacketType.MessageLostPDU;
                case 0x002c:
                    return VoltronPacketType.OccupantArrivedPDU;
                case 0x002d:
                    return VoltronPacketType.OccupantDepartedPDU;
                case 0x002e:
                    return VoltronPacketType.ReadProfilePDU;
                case 0x002f:
                    return VoltronPacketType.ReadProfileResponsePDU;
                case 0x0030:
                    return VoltronPacketType.ReleaseProfilePDU;
                case 0x0031:
                    return VoltronPacketType.ReleaseProfileResponsePDU;
                case 0x0032:
                    return VoltronPacketType.SetAcceptAlertsPDU;
                case 0x0033:
                    return VoltronPacketType.SetAcceptAlertsResponsePDU;
                case 0x0034:
                    return VoltronPacketType.SetIgnoreListPDU;
                case 0x0035:
                    return VoltronPacketType.SetIgnoreListResponsePDU;
                case 0x0036:
                    return VoltronPacketType.SetInvinciblePDU;
                case 0x0037:
                    return VoltronPacketType.SetInvincibleResponsePDU;
                case 0x0038:
                    return VoltronPacketType.SetInvisiblePDU;
                case 0x0039:
                    return VoltronPacketType.SetInvisibleResponsePDU;
                case 0x003a:
                    return VoltronPacketType.SetRoomNamePDU;
                case 0x003b:
                    return VoltronPacketType.SetRoomNameResponsePDU;
                case 0x003c:
                    return VoltronPacketType.UpdateOccupantsPDU;
                case 0x003d:
                    return VoltronPacketType.UpdatePlayerPDU;
                case 0x003e:
                    return VoltronPacketType.UpdateProfilePDU;
                case 0x003f:
                    return VoltronPacketType.UpdateRoomPDU;
                case 0x0040:
                    return VoltronPacketType.YankPlayerFailedPDU;
                case 0x0041:
                    return VoltronPacketType.YankPlayerPDU;
                case 0x0042:
                    return VoltronPacketType.SetAcceptFlashesPDU;
                case 0x0043:
                    return VoltronPacketType.SetAcceptFlashesResponsePDU;
                case 0x0044:
                    return VoltronPacketType.SplitBufferPDU;
                case 0x0045:
                    return VoltronPacketType.ActionRoomNamePDU;
                case 0x0046:
                    return VoltronPacketType.ActionRoomNameResponsePDU;
                case 0x0047:
                    return VoltronPacketType.NotifyRoomActionedPDU;
                case 0x0048:
                    return VoltronPacketType.ModifyProfilePDU;
                case 0x0049:
                    return VoltronPacketType.ModifyProfileResponsePDU;
                case 0x004a:
                    return VoltronPacketType.ListBBSFoldersPDU;
                case 0x004b:
                    return VoltronPacketType.ListBBSFoldersResponsePDU;
                case 0x004c:
                    return VoltronPacketType.GetBBSMessageListPDU;
                case 0x004d:
                    return VoltronPacketType.GetBBSMessageListResponsePDU;
                case 0x004e:
                    return VoltronPacketType.PostBBSMessagePDU;
                case 0x004f:
                    return VoltronPacketType.PostBBSReplyPDU;
                case 0x0050:
                    return VoltronPacketType.PostBBSMessageResponsePDU;
                case 0x0051:
                    return VoltronPacketType.GetMPSMessagesPDU;
                case 0x0052:
                    return VoltronPacketType.GetMPSMessagesResponsePDU;
                case 0x0053:
                    return VoltronPacketType.DeleteMPSMessagePDU;
                case 0x0054:
                    return VoltronPacketType.DeleteMPSMessageResponsePDU;
                case 0x0055:
                    return VoltronPacketType.BBSMessageDataPDU;
                case 0x0056:
                    return VoltronPacketType.UpdateRoomAdminListPDU;
                case 0x0057:
                    return VoltronPacketType.GetRoomAdminListPDU;
                case 0x0058:
                    return VoltronPacketType.GetRoomAdminListResponsePDU;
                case 0x0059:
                    return VoltronPacketType.GroupInfoRequestPDU;
                case 0x005a:
                    return VoltronPacketType.GroupInfoResponsePDU;
                case 0x005b:
                    return VoltronPacketType.GroupAdminRequestPDU;
                case 0x005c:
                    return VoltronPacketType.GroupAdminResponsePDU;
                case 0x005d:
                    return VoltronPacketType.GroupMembershipRequestPDU;
                case 0x005e:
                    return VoltronPacketType.GroupMembershipResponsePDU;
                case 0x005f:
                    return VoltronPacketType.FlashGroupPDU;
                case 0x0060:
                    return VoltronPacketType.FlashGroupResponsePDU;
                case 0x0061:
                    return VoltronPacketType.UpdateGroupMemberPDU;
                case 0x0062:
                    return VoltronPacketType.UpdateGroupMemberResponsePDU;
                case 0x0063:
                    return VoltronPacketType.UpdateGroupAdminPDU;
                case 0x0064:
                    return VoltronPacketType.UpdateGroupAdminResponsePDU;
                case 0x0065:
                    return VoltronPacketType.ListGroupsPDU;
                case 0x0066:
                    return VoltronPacketType.ListGroupsResponsePDU;
                case 0x0067:
                    return VoltronPacketType.ListJoinedGroupsPDU;
                case 0x0068:
                    return VoltronPacketType.ListJoinedGroupsResponsePDU;
                case 0x0069:
                    return VoltronPacketType.GpsChatPDU;
                case 0x006a:
                    return VoltronPacketType.GpsChatResponsePDU;
                case 0x006b:
                    return VoltronPacketType.PetitionStatusUpdatePDU;
                case 0x006c:
                    return VoltronPacketType.LogGPSPetitionPDU;
                case 0x006d:
                    return VoltronPacketType.LogGPSPetitionResponsePDU;
                case 0x006e:
                    return VoltronPacketType.List20RoomsPDU;
                case 0x006f:
                    return VoltronPacketType.List20RoomsResponsePDU;
                case 0x0070:
                    return VoltronPacketType.UpdateIgnoreListPDU;
                case 0x0071:
                    return VoltronPacketType.ResetWatchdogPDU;
                case 0x0072:
                    return VoltronPacketType.ResetWatchdogResponsePDU;
                case 0x2710:
                    return VoltronPacketType.BroadcastDataBlobPDU;
                case 0x2711:
                    return VoltronPacketType.TransmitDataBlobPDU;
                case 0x2712:
                    return VoltronPacketType.DBRequestWrapperPDU;
                case 0x2713:
                    return VoltronPacketType.TransmitCreateAvatarNotificationPDU;
                case 0x2715:
                    return VoltronPacketType.BC_PlayerLoginEventPDU;
                case 0x2716:
                    return VoltronPacketType.BC_PlayerLogoutEventPDU;
                case 0x2718:
                    return VoltronPacketType.RoomserverUserlistPDU;
                case 0x2719:
                    return VoltronPacketType.LotEntryRequestPDU;
                case 0x271a:
                    return VoltronPacketType.ClientConfigPDU;
                case 0x271c:
                    return VoltronPacketType.KickoutRoommatePDU;
                case 0x271d:
                    return VoltronPacketType.GenericFlashPDU;
                case 0x271e:
                    return VoltronPacketType.GenericFlashRequestPDU;
                case 0x271f:
                    return VoltronPacketType.GenericFlashResponsePDU;
                case 0x2722:
                    return VoltronPacketType.TransmitGenericGDMPDU;
                case 0x2723:
                    return VoltronPacketType.EjectAvatarPDU;
                case 0x2724:
                    return VoltronPacketType.TestPDU;
                case 0x2725:
                    return VoltronPacketType.HouseSimConstraintsPDU;
                case 0x2726:
                    return VoltronPacketType.HouseSimConstraintsResponsePDU;
                case 0x2728:
                    return VoltronPacketType.LoadHouseResponsePDU;
                case 0x2729:
                    return VoltronPacketType.ComponentVersionRequestPDU;
                case 0x272a:
                    return VoltronPacketType.ComponentVersionResponsePDU;
                case 0x272b:
                    return VoltronPacketType.InviteRoommatePDU;
                case 0x272c:
                    return VoltronPacketType.RoommateInvitationAnswerPDU;
                case 0x272d:
                    return VoltronPacketType.RoommateGDMPDU;
                case 0x272e:
                    return VoltronPacketType.HSB_ShutdownSimulatorPDU;
                case 0x272f:
                    return VoltronPacketType.RoommateGDMResponsePDU;
                case 0x2730:
                    return VoltronPacketType.RSGZWrapperPDU;
                case 0x2731:
                    return VoltronPacketType.AvatarHasNewLotIDPDU;
                case 0x2733:
                    return VoltronPacketType.CheatPDU;
                case 0x2734:
                    return VoltronPacketType.DataServiceWrapperPDU;
                case 0x2735:
                    return VoltronPacketType.CsrEjectAvatarPDU;
                case 0x2736:
                    return VoltronPacketType.CsrEjectAvatarResponsePDU;
                case 0x2737:
                    return VoltronPacketType.cTSONetMessagePDU;
                case 0x2738:
                    return VoltronPacketType.LogCsrActionPDU;
                case 0x2739:
                    return VoltronPacketType.LogAvatarActionPDU;
                case 0xffff:
                    return VoltronPacketType.Unknown;
            }
            return VoltronPacketType.Unknown;
        }

        public static ushort GetPacketCode(this VoltronPacketType type)
        {
            switch (type)
            {
                case VoltronPacketType.AlertHandledPDU:
                    return 0x0001;
                case VoltronPacketType.AlertMsgPDU:
                    return 0x0002;
                case VoltronPacketType.AlertMsgResponsePDU:
                    return 0x0003;
                case VoltronPacketType.AnnouncementMsgResponsePDU:
                    return 0x0004;
                case VoltronPacketType.AnnouncementMsgPDU:
                    return 0x0005;
                case VoltronPacketType.ClientByePDU:
                    return 0x0006;
                case VoltronPacketType.ServerByePDU:
                    return 0x0007;
                case VoltronPacketType.ChatMsgFailedPDU:
                    return 0x0008;
                case VoltronPacketType.ChatMsgPDU:
                    return 0x0009;
                case VoltronPacketType.ClientOnlinePDU:
                    return 0x000a;
                case VoltronPacketType.CreateAndJoinRoomFailedPDU:
                    return 0x000b;
                case VoltronPacketType.CreateAndJoinRoomPDU:
                    return 0x000c;
                case VoltronPacketType.CreateRoomPDU:
                    return 0x000d;
                case VoltronPacketType.CreateRoomResponsePDU:
                    return 0x000e;
                case VoltronPacketType.DestroyRoomPDU:
                    return 0x000f;
                case VoltronPacketType.DestroyRoomResponsePDU:
                    return 0x0010;
                case VoltronPacketType.DetachFromRoomFailedPDU:
                    return 0x0011;
                case VoltronPacketType.DetachFromRoomPDU:
                    return 0x0012;
                case VoltronPacketType.EjectOccupantPDU:
                    return 0x0013;
                case VoltronPacketType.EjectOccupantResponsePDU:
                    return 0x0014;
                case VoltronPacketType.ErrorPDU:
                    return 0x0015;
                case VoltronPacketType.ExitRoomFailedPDU:
                    return 0x0016;
                case VoltronPacketType.ExitRoomPDU:
                    return 0x0017;
                case VoltronPacketType.FindPlayerPDU:
                    return 0x0018;
                case VoltronPacketType.FindPlayerResponsePDU:
                    return 0x0019;
                case VoltronPacketType.FlashMsgResponsePDU:
                    return 0x001a;
                case VoltronPacketType.FlashMsgPDU:
                    return 0x001b;
                case VoltronPacketType.HandleAlertPDU:
                    return 0x001c;
                case VoltronPacketType.HostOfflinePDU:
                    return 0x001d;
                case VoltronPacketType.HostOnlinePDU:
                    return 0x001e;
                case VoltronPacketType.InvitationMsgResponsePDU:
                    return 0x001f;
                case VoltronPacketType.InvitationMsgPDU:
                    return 0x0020;
                case VoltronPacketType.JoinPlayerFailedPDU:
                    return 0x0021;
                case VoltronPacketType.JoinPlayerPDU:
                    return 0x0022;
                case VoltronPacketType.JoinRoomFailedPDU:
                    return 0x0023;
                case VoltronPacketType.JoinRoomPDU:
                    return 0x0024;
                case VoltronPacketType.ListOccupantsPDU:
                    return 0x0025;
                case VoltronPacketType.ListOccupantsResponsePDU:
                    return 0x0026;
                case VoltronPacketType.ListRoomsPDU:
                    return 0x0027;
                case VoltronPacketType.ListRoomsResponsePDU:
                    return 0x0028;
                case VoltronPacketType.LogEventPDU:
                    return 0x0029;
                case VoltronPacketType.LogEventResponsePDU:
                    return 0x002a;
                case VoltronPacketType.MessageLostPDU:
                    return 0x002b;
                case VoltronPacketType.OccupantArrivedPDU:
                    return 0x002c;
                case VoltronPacketType.OccupantDepartedPDU:
                    return 0x002d;
                case VoltronPacketType.ReadProfilePDU:
                    return 0x002e;
                case VoltronPacketType.ReadProfileResponsePDU:
                    return 0x002f;
                case VoltronPacketType.ReleaseProfilePDU:
                    return 0x0030;
                case VoltronPacketType.ReleaseProfileResponsePDU:
                    return 0x0031;
                case VoltronPacketType.SetAcceptAlertsPDU:
                    return 0x0032;
                case VoltronPacketType.SetAcceptAlertsResponsePDU:
                    return 0x0033;
                case VoltronPacketType.SetIgnoreListPDU:
                    return 0x0034;
                case VoltronPacketType.SetIgnoreListResponsePDU:
                    return 0x0035;
                case VoltronPacketType.SetInvinciblePDU:
                    return 0x0036;
                case VoltronPacketType.SetInvincibleResponsePDU:
                    return 0x0037;
                case VoltronPacketType.SetInvisiblePDU:
                    return 0x0038;
                case VoltronPacketType.SetInvisibleResponsePDU:
                    return 0x0039;
                case VoltronPacketType.SetRoomNamePDU:
                    return 0x003a;
                case VoltronPacketType.SetRoomNameResponsePDU:
                    return 0x003b;
                case VoltronPacketType.UpdateOccupantsPDU:
                    return 0x003c;
                case VoltronPacketType.UpdatePlayerPDU:
                    return 0x003d;
                case VoltronPacketType.UpdateProfilePDU:
                    return 0x003e;
                case VoltronPacketType.UpdateRoomPDU:
                    return 0x003f;
                case VoltronPacketType.YankPlayerFailedPDU:
                    return 0x0040;
                case VoltronPacketType.YankPlayerPDU:
                    return 0x0041;
                case VoltronPacketType.SetAcceptFlashesPDU:
                    return 0x0042;
                case VoltronPacketType.SetAcceptFlashesResponsePDU:
                    return 0x0043;
                case VoltronPacketType.SplitBufferPDU:
                    return 0x0044;
                case VoltronPacketType.ActionRoomNamePDU:
                    return 0x0045;
                case VoltronPacketType.ActionRoomNameResponsePDU:
                    return 0x0046;
                case VoltronPacketType.NotifyRoomActionedPDU:
                    return 0x0047;
                case VoltronPacketType.ModifyProfilePDU:
                    return 0x0048;
                case VoltronPacketType.ModifyProfileResponsePDU:
                    return 0x0049;
                case VoltronPacketType.ListBBSFoldersPDU:
                    return 0x004a;
                case VoltronPacketType.ListBBSFoldersResponsePDU:
                    return 0x004b;
                case VoltronPacketType.GetBBSMessageListPDU:
                    return 0x004c;
                case VoltronPacketType.GetBBSMessageListResponsePDU:
                    return 0x004d;
                case VoltronPacketType.PostBBSMessagePDU:
                    return 0x004e;
                case VoltronPacketType.PostBBSReplyPDU:
                    return 0x004f;
                case VoltronPacketType.PostBBSMessageResponsePDU:
                    return 0x0050;
                case VoltronPacketType.GetMPSMessagesPDU:
                    return 0x0051;
                case VoltronPacketType.GetMPSMessagesResponsePDU:
                    return 0x0052;
                case VoltronPacketType.DeleteMPSMessagePDU:
                    return 0x0053;
                case VoltronPacketType.DeleteMPSMessageResponsePDU:
                    return 0x0054;
                case VoltronPacketType.BBSMessageDataPDU:
                    return 0x0055;
                case VoltronPacketType.UpdateRoomAdminListPDU:
                    return 0x0056;
                case VoltronPacketType.GetRoomAdminListPDU:
                    return 0x0057;
                case VoltronPacketType.GetRoomAdminListResponsePDU:
                    return 0x0058;
                case VoltronPacketType.GroupInfoRequestPDU:
                    return 0x0059;
                case VoltronPacketType.GroupInfoResponsePDU:
                    return 0x005a;
                case VoltronPacketType.GroupAdminRequestPDU:
                    return 0x005b;
                case VoltronPacketType.GroupAdminResponsePDU:
                    return 0x005c;
                case VoltronPacketType.GroupMembershipRequestPDU:
                    return 0x005d;
                case VoltronPacketType.GroupMembershipResponsePDU:
                    return 0x005e;
                case VoltronPacketType.FlashGroupPDU:
                    return 0x005f;
                case VoltronPacketType.FlashGroupResponsePDU:
                    return 0x0060;
                case VoltronPacketType.UpdateGroupMemberPDU:
                    return 0x0061;
                case VoltronPacketType.UpdateGroupMemberResponsePDU:
                    return 0x0062;
                case VoltronPacketType.UpdateGroupAdminPDU:
                    return 0x0063;
                case VoltronPacketType.UpdateGroupAdminResponsePDU:
                    return 0x0064;
                case VoltronPacketType.ListGroupsPDU:
                    return 0x0065;
                case VoltronPacketType.ListGroupsResponsePDU:
                    return 0x0066;
                case VoltronPacketType.ListJoinedGroupsPDU:
                    return 0x0067;
                case VoltronPacketType.ListJoinedGroupsResponsePDU:
                    return 0x0068;
                case VoltronPacketType.GpsChatPDU:
                    return 0x0069;
                case VoltronPacketType.GpsChatResponsePDU:
                    return 0x006a;
                case VoltronPacketType.PetitionStatusUpdatePDU:
                    return 0x006b;
                case VoltronPacketType.LogGPSPetitionPDU:
                    return 0x006c;
                case VoltronPacketType.LogGPSPetitionResponsePDU:
                    return 0x006d;
                case VoltronPacketType.List20RoomsPDU:
                    return 0x006e;
                case VoltronPacketType.List20RoomsResponsePDU:
                    return 0x006f;
                case VoltronPacketType.UpdateIgnoreListPDU:
                    return 0x0070;
                case VoltronPacketType.ResetWatchdogPDU:
                    return 0x0071;
                case VoltronPacketType.ResetWatchdogResponsePDU:
                    return 0x0072;
                case VoltronPacketType.BroadcastDataBlobPDU:
                    return 0x2710;
                case VoltronPacketType.TransmitDataBlobPDU:
                    return 0x2711;
                case VoltronPacketType.DBRequestWrapperPDU:
                    return 0x2712;
                case VoltronPacketType.TransmitCreateAvatarNotificationPDU:
                    return 0x2713;
                case VoltronPacketType.BC_PlayerLoginEventPDU:
                    return 0x2715;
                case VoltronPacketType.BC_PlayerLogoutEventPDU:
                    return 0x2716;
                case VoltronPacketType.RoomserverUserlistPDU:
                    return 0x2718;
                case VoltronPacketType.LotEntryRequestPDU:
                    return 0x2719;
                case VoltronPacketType.ClientConfigPDU:
                    return 0x271a;
                case VoltronPacketType.KickoutRoommatePDU:
                    return 0x271c;
                case VoltronPacketType.GenericFlashPDU:
                    return 0x271d;
                case VoltronPacketType.GenericFlashRequestPDU:
                    return 0x271e;
                case VoltronPacketType.GenericFlashResponsePDU:
                    return 0x271f;
                case VoltronPacketType.TransmitGenericGDMPDU:
                    return 0x2722;
                case VoltronPacketType.EjectAvatarPDU:
                    return 0x2723;
                case VoltronPacketType.TestPDU:
                    return 0x2724;
                case VoltronPacketType.HouseSimConstraintsPDU:
                    return 0x2725;
                case VoltronPacketType.HouseSimConstraintsResponsePDU:
                    return 0x2726;
                case VoltronPacketType.LoadHouseResponsePDU:
                    return 0x2728;
                case VoltronPacketType.ComponentVersionRequestPDU:
                    return 0x2729;
                case VoltronPacketType.ComponentVersionResponsePDU:
                    return 0x272a;
                case VoltronPacketType.InviteRoommatePDU:
                    return 0x272b;
                case VoltronPacketType.RoommateInvitationAnswerPDU:
                    return 0x272c;
                case VoltronPacketType.RoommateGDMPDU:
                    return 0x272d;
                case VoltronPacketType.HSB_ShutdownSimulatorPDU:
                    return 0x272e;
                case VoltronPacketType.RoommateGDMResponsePDU:
                    return 0x272f;
                case VoltronPacketType.RSGZWrapperPDU:
                    return 0x2730;
                case VoltronPacketType.AvatarHasNewLotIDPDU:
                    return 0x2731;
                case VoltronPacketType.CheatPDU:
                    return 0x2733;
                case VoltronPacketType.DataServiceWrapperPDU:
                    return 0x2734;
                case VoltronPacketType.CsrEjectAvatarPDU:
                    return 0x2735;
                case VoltronPacketType.CsrEjectAvatarResponsePDU:
                    return 0x2736;
                case VoltronPacketType.cTSONetMessagePDU:
                    return 0x2737;
                case VoltronPacketType.LogCsrActionPDU:
                    return 0x2738;
                case VoltronPacketType.LogAvatarActionPDU:
                    return 0x2739;
                case VoltronPacketType.Unknown:
                    return 0xffff;
            }
            return 0;
        }
    }
}
