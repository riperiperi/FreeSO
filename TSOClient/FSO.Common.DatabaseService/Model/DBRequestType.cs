namespace FSO.Common.DatabaseService.Model
{
    public enum DBRequestType
    {
        GetHouseThumbByID,
        GetLotAndObjects,
        GetLotList,
        GetMaxPlayerPerLot,
        GetNeighborhoods,
        GetShardVersion,
        GetTopList,
        GetTopResultSetByID,
        InsertBookmarks,
        InsertGenericLog,
        InsertGenericTask,
        InsertNeighborhoods,
        InsertNewAvatar,
        InsertNewFriendshipComment,
        InsertPendingRoomateInv,
        InsertSpotlightTextByLotID,
        MoveOutByAvatarID,
        LoadAvatarByID,
        MoveLotByID,
        PrtControlToggleByAvatarID,
        RejectPendingRoomateInv,
        ReleaseAvatarLease,
        RenewAvatarLease,
        SaveAvatarByID,
        SaveLotAndObjectBlobByID,
        Search,
        SearchExactMatch,
        SellObject,
        SetFriendshipComment,
        SetHouseThumbByID,
        SetLotDesc,
        SetLotHoursVisitedByID,
        SetLotName,
        SetMoneyFields,
        StockDress,
        UpdateBadgeByID,
        UpdateCharDescByID,
        UpdateDataServiceLotAdmitInfo_AddAdmittedID,
        UpdateDataServiceLotAdmitInfo_AddBannedID,
        UpdateDataServiceLotAdmitInfo_RemoveAdmittedID,
        UpdateDataServiceLotAdmitInfo_RemoveBannedID,
        UpdateDataServiceLotAdmitInfo_SetAdmitMode,
        UpdateLotValueByID,
        UpdateTaskStatus,
        GetSpotlightLotList,
        GetFinancialDetail,
        GetOnlineJobLot,
        OnlineJobLotDesactivate,
        OnlineJobLotRequestDesactivation,
        OnlineJobOccupantDesactivate,
        UpdatePrivacyModeByID,
        GetDataUpdateEventsLastSeqID,
        GetDataUpdateEvents,
        GetNeighborhoodInfo,
        CallCreateFriends,
        CallDecayRelationships,
        UpdateRelationshipLastContact,
        UpdatePreferedLanguageByID,
        RenameAvatar,
        AcceptPendingRoomateInv,
        AcquireAvatarLease,
        BuyDress,
        BuyLotByAvatarID,
        BuyObject,
        DeleteAttributeByAvatarID,
        DeleteAvatarByID,
        DeleteBookmarks,
        DeleteObject,
        DeleteSpotlightTextByLotID,
        GetAvatarIDByName,
        GetAvatarInventoryByID,
        GetBookmarks,
        GetCharByID,
        GetCityType,
        GetDataServiceBuildableMapInfoByXY,
        GetDataServiceLotAdmitInfo,
        GetDataServiceLotInfoByXY,
        GetDebitCreditTask,
        GetGenericFlash,
        GetHouseLeaderByLotID,
        GetObjectTuningVariables,
        GetObjectsAndCatalog,
        CreateObject,
        GetSelector,
        GetGameConstants,
        RecordFeedEvent,
        DebitCredit,
        GetAllRelationshipsByID,
        GetDataServiceAvatarBudgetByID,
        GetDataServiceAvatarSkillsByID,
        GetFriendshipWebCommentByID,
        InsertGenericFlash,
        SecureTrade,
        SetDataServiceAvatarVarSkillLockByID,
        SetLotCategory,
        SetLotNeighborhoodID,
        SetNeighborhoodCenterGridXY,
        InventoryTransfer,
        GetSelectorApproval,
        DebitCreditSharedAccount,
        ExternalBankTransaction,



        Unknown
    }

    public static class DBRequestTypeUtils
    {
        public static uint GetRequestID(this DBRequestType request)
        {
            switch (request)
            {
                case DBRequestType.GetHouseThumbByID:
                    return 0x9BF18F10;
                case DBRequestType.GetLotAndObjects:
                    return 0x0BFD89E3;
                case DBRequestType.GetLotList:
                    return 0x5BEEB701;
                case DBRequestType.GetMaxPlayerPerLot:
                    return 0x5F0A0561;
                case DBRequestType.GetNeighborhoods:
                    return 0x8AE0FD8C;
                case DBRequestType.GetShardVersion:
                    return 0x5E209378;
                case DBRequestType.GetTopList:
                    return 0x3D8787DA;
                case DBRequestType.GetTopResultSetByID:
                    return 0xBCD038AC;
                case DBRequestType.InsertBookmarks:
                    return 0x09CBE333;
                case DBRequestType.InsertGenericLog:
                    return 0x3D03D5F7;
                case DBRequestType.InsertGenericTask:
                    return 0xC98B6799;
                case DBRequestType.InsertNeighborhoods:
                    return 0xAAE1247E;
                case DBRequestType.InsertNewAvatar:
                    return 0x8A3BE831;
                case DBRequestType.InsertNewFriendshipComment:
                    return 0x6AE8E1ED;
                case DBRequestType.InsertPendingRoomateInv:
                    return 0x3CE98067;
                case DBRequestType.InsertSpotlightTextByLotID:
                    return 0x8B8AE566;
                case DBRequestType.MoveOutByAvatarID:
                    return 0x4CEEB62C;
                case DBRequestType.LoadAvatarByID:
                    return 0x2ADF7EED;
                case DBRequestType.MoveLotByID:
                    return 0xEB42651A;
                case DBRequestType.PrtControlToggleByAvatarID:
                    return 0x8A53F433;
                case DBRequestType.RejectPendingRoomateInv:
                    return 0xDCE98959;
                case DBRequestType.ReleaseAvatarLease:
                    return 0x6B9EAECD;
                case DBRequestType.RenewAvatarLease:
                    return 0xCB9EAF2F;
                case DBRequestType.SaveAvatarByID:
                    return 0x2ADDE378;
                case DBRequestType.SaveLotAndObjectBlobByID:
                    return 0xCBFD89A7;
                case DBRequestType.Search:
                    return 0x89483786;
                case DBRequestType.SearchExactMatch:
                    return 0xA952742D;
                case DBRequestType.SellObject:
                    return 0x8BFD896A;
                case DBRequestType.SetFriendshipComment:
                    return 0x0AE0AEB8;
                case DBRequestType.SetHouseThumbByID:
                    return 0xFBF6E364;
                case DBRequestType.SetLotDesc:
                    return 0x8A70B952;
                case DBRequestType.SetLotHoursVisitedByID:
                    return 0x7C02938C;
                case DBRequestType.SetLotName:
                    return 0x6A70B931;
                case DBRequestType.SetMoneyFields:
                    return 0x5CF147E8;
                case DBRequestType.StockDress:
                    return 0x2B4510AC;
                case DBRequestType.UpdateBadgeByID:
                    return 0xCAFB30AA;
                case DBRequestType.UpdateCharDescByID:
                    return 0xAA3FEDA1;
                case DBRequestType.UpdateDataServiceLotAdmitInfo_AddAdmittedID:
                    return 0xCA26E9CF;
                case DBRequestType.UpdateDataServiceLotAdmitInfo_AddBannedID:
                    return 0xEA26E9F8;
                case DBRequestType.UpdateDataServiceLotAdmitInfo_RemoveAdmittedID:
                    return 0xEA26E9E4;
                case DBRequestType.UpdateDataServiceLotAdmitInfo_RemoveBannedID:
                    return 0x0A26EA0C;
                case DBRequestType.UpdateDataServiceLotAdmitInfo_SetAdmitMode:
                    return 0xCA26E9BD;
                case DBRequestType.UpdateLotValueByID:
                    return 0xDC17FB0E;
                case DBRequestType.UpdateTaskStatus:
                    return 0xA92AF562;
                case DBRequestType.GetSpotlightLotList:
                    return 0xEBD4DDAC;
                case DBRequestType.GetFinancialDetail:
                    return 0x0C23D673;
                case DBRequestType.GetOnlineJobLot:
                    return 0x8C3BBA00;
                case DBRequestType.OnlineJobLotDesactivate:
                    return 0xAC50944B;
                case DBRequestType.OnlineJobLotRequestDesactivation:
                    return 0x0C9D233E;
                case DBRequestType.OnlineJobOccupantDesactivate:
                    return 0xAC96E1AE;
                case DBRequestType.UpdatePrivacyModeByID:
                    return 0xA0C6106C;
                case DBRequestType.GetDataUpdateEventsLastSeqID:
                    return 0xCBB127FD;
                case DBRequestType.GetDataUpdateEvents:
                    return 0x0B5E2124;
                case DBRequestType.GetNeighborhoodInfo:
                    return 0xCB7AD7EE;
                case DBRequestType.CallCreateFriends:
                    return 0x21586E78;
                case DBRequestType.CallDecayRelationships:
                    return 0xE1586F32;
                case DBRequestType.UpdateRelationshipLastContact:
                    return 0x2D33ABF3;
                case DBRequestType.UpdatePreferedLanguageByID:
                    return 0x2D98FAF3;
                case DBRequestType.RenameAvatar:
                    return 0x1060F3A1;
                case DBRequestType.AcceptPendingRoomateInv:
                    return 0xDCE9801A;
                case DBRequestType.AcquireAvatarLease:
                    return 0x6B9D76FC;
                case DBRequestType.BuyDress:
                    return 0x2B44F0C9;
                case DBRequestType.BuyLotByAvatarID:
                    return 0x1D8DD55A;
                case DBRequestType.BuyObject:
                    return 0x4BFD8A1B;
                case DBRequestType.DeleteAttributeByAvatarID:
                    return 0xCB392F06;
                case DBRequestType.DeleteAvatarByID:
                    return 0x6A52E10C;
                case DBRequestType.DeleteBookmarks:
                    return 0x2A3C81DB;
                case DBRequestType.DeleteObject:
                    return 0x0B25CD36;
                case DBRequestType.DeleteSpotlightTextByLotID:
                    return 0x6B8D6F5D;
                case DBRequestType.GetAvatarIDByName:
                    return 0xA95BB3D7;
                case DBRequestType.GetAvatarInventoryByID:
                    return 0x2BFD89FF;
                case DBRequestType.GetBookmarks:
                    return 0x0BA7F82C;
                case DBRequestType.GetCharByID:
                    return 0x7BAE5079;
                case DBRequestType.GetCityType:
                    return 0x2A3927A2;
                case DBRequestType.GetDataServiceBuildableMapInfoByXY:
                    return 0x4A3D75DA;
                case DBRequestType.GetDataServiceLotAdmitInfo:
                    return 0x8A2456E6;
                case DBRequestType.GetDataServiceLotInfoByXY:
                    return 0xE9C81D19;
                case DBRequestType.GetDebitCreditTask:
                    return 0xE98A1700;
                case DBRequestType.GetGenericFlash:
                    return 0x9FE8B670;
                case DBRequestType.GetHouseLeaderByLotID:
                    return 0xDD909124;
                case DBRequestType.GetObjectTuningVariables:
                    return 0xEFAB815F;
                case DBRequestType.GetObjectsAndCatalog:
                    return 0x9525BE9F;
                case DBRequestType.CreateObject:
                    return 0xABCD8986;
                case DBRequestType.GetSelector:
                    return 0x17C62EA8;
                case DBRequestType.GetGameConstants:
                    return 0xCD999E31;
                case DBRequestType.RecordFeedEvent:
                    return 0x688188BA;
                case DBRequestType.DebitCredit:
                    return 0x7C24F627;
                case DBRequestType.GetAllRelationshipsByID:
                    return 0xAB4A6098;
                case DBRequestType.GetDataServiceAvatarBudgetByID:
                    return 0x4AA84714;
                case DBRequestType.GetDataServiceAvatarSkillsByID:
                    return 0x0AA81112;
                case DBRequestType.GetFriendshipWebCommentByID:
                    return 0xEAE9F7E3;
                case DBRequestType.InsertGenericFlash:
                    return 0xFFE8AC61;
                case DBRequestType.SecureTrade:
                    return 0xABFD8986;
                case DBRequestType.SetDataServiceAvatarVarSkillLockByID:
                    return 0xCB9DE627;
                case DBRequestType.SetLotCategory:
                    return 0xAAD4C4D5;
                case DBRequestType.SetLotNeighborhoodID:
                    return 0xEAF53BFB;
                case DBRequestType.SetNeighborhoodCenterGridXY:
                    return 0x8B0847D4;
                case DBRequestType.InventoryTransfer:
                    return 0x2BFDC6D0;
                case DBRequestType.GetSelectorApproval:
                    return 0xA7F41927;
                case DBRequestType.DebitCreditSharedAccount:
                    return 0x1C6A90C3;
                case DBRequestType.ExternalBankTransaction:
                    return 0x51F76976;
            }
            return 0;
        }

        public static DBRequestType FromRequestID(uint requestID)
        {
            switch (requestID)
            {
                case 0x9BF18F10:
                    return DBRequestType.GetHouseThumbByID;
                case 0x0BFD89E3:
                    return DBRequestType.GetLotAndObjects;
                case 0x5BEEB701:
                    return DBRequestType.GetLotList;
                case 0x5F0A0561:
                    return DBRequestType.GetMaxPlayerPerLot;
                case 0x8AE0FD8C:
                    return DBRequestType.GetNeighborhoods;
                case 0x5E209378:
                    return DBRequestType.GetShardVersion;
                case 0x3D8787DA:
                    return DBRequestType.GetTopList;
                case 0xBCD038AC:
                    return DBRequestType.GetTopResultSetByID;
                case 0x09CBE333:
                    return DBRequestType.InsertBookmarks;
                case 0x3D03D5F7:
                    return DBRequestType.InsertGenericLog;
                case 0xC98B6799:
                    return DBRequestType.InsertGenericTask;
                case 0xAAE1247E:
                    return DBRequestType.InsertNeighborhoods;
                case 0x8A3BE831:
                    return DBRequestType.InsertNewAvatar;
                case 0x6AE8E1ED:
                    return DBRequestType.InsertNewFriendshipComment;
                case 0x3CE98067:
                    return DBRequestType.InsertPendingRoomateInv;
                case 0x8B8AE566:
                    return DBRequestType.InsertSpotlightTextByLotID;
                case 0x4CEEB62C:
                    return DBRequestType.MoveOutByAvatarID;
                case 0x2ADF7EED:
                    return DBRequestType.LoadAvatarByID;
                case 0xEB42651A:
                    return DBRequestType.MoveLotByID;
                case 0x8A53F433:
                    return DBRequestType.PrtControlToggleByAvatarID;
                case 0xDCE98959:
                    return DBRequestType.RejectPendingRoomateInv;
                case 0x6B9EAECD:
                    return DBRequestType.ReleaseAvatarLease;
                case 0xCB9EAF2F:
                    return DBRequestType.RenewAvatarLease;
                case 0x2ADDE378:
                    return DBRequestType.SaveAvatarByID;
                case 0xCBFD89A7:
                    return DBRequestType.SaveLotAndObjectBlobByID;
                case 0x89483786:
                    return DBRequestType.Search;
                case 0xA952742D:
                    return DBRequestType.SearchExactMatch;
                case 0x8BFD896A:
                    return DBRequestType.SellObject;
                case 0x0AE0AEB8:
                    return DBRequestType.SetFriendshipComment;
                case 0xFBF6E364:
                    return DBRequestType.SetHouseThumbByID;
                case 0x8A70B952:
                    return DBRequestType.SetLotDesc;
                case 0x7C02938C:
                    return DBRequestType.SetLotHoursVisitedByID;
                case 0x6A70B931:
                    return DBRequestType.SetLotName;
                case 0x5CF147E8:
                    return DBRequestType.SetMoneyFields;
                case 0x2B4510AC:
                    return DBRequestType.StockDress;
                case 0xCAFB30AA:
                    return DBRequestType.UpdateBadgeByID;
                case 0xAA3FEDA1:
                    return DBRequestType.UpdateCharDescByID;
                case 0xCA26E9CF:
                    return DBRequestType.UpdateDataServiceLotAdmitInfo_AddAdmittedID;
                case 0xEA26E9F8:
                    return DBRequestType.UpdateDataServiceLotAdmitInfo_AddBannedID;
                case 0xEA26E9E4:
                    return DBRequestType.UpdateDataServiceLotAdmitInfo_RemoveAdmittedID;
                case 0x0A26EA0C:
                    return DBRequestType.UpdateDataServiceLotAdmitInfo_RemoveBannedID;
                case 0xCA26E9BD:
                    return DBRequestType.UpdateDataServiceLotAdmitInfo_SetAdmitMode;
                case 0xDC17FB0E:
                    return DBRequestType.UpdateLotValueByID;
                case 0xA92AF562:
                    return DBRequestType.UpdateTaskStatus;
                case 0xEBD4DDAC:
                    return DBRequestType.GetSpotlightLotList;
                case 0x0C23D673:
                    return DBRequestType.GetFinancialDetail;
                case 0x8C3BBA00:
                    return DBRequestType.GetOnlineJobLot;
                case 0xAC50944B:
                    return DBRequestType.OnlineJobLotDesactivate;
                case 0x0C9D233E:
                    return DBRequestType.OnlineJobLotRequestDesactivation;
                case 0xAC96E1AE:
                    return DBRequestType.OnlineJobOccupantDesactivate;
                case 0xA0C6106C:
                    return DBRequestType.UpdatePrivacyModeByID;
                case 0xCBB127FD:
                    return DBRequestType.GetDataUpdateEventsLastSeqID;
                case 0x0B5E2124:
                    return DBRequestType.GetDataUpdateEvents;
                case 0xCB7AD7EE:
                    return DBRequestType.GetNeighborhoodInfo;
                case 0x21586E78:
                    return DBRequestType.CallCreateFriends;
                case 0xE1586F32:
                    return DBRequestType.CallDecayRelationships;
                case 0x2D33ABF3:
                    return DBRequestType.UpdateRelationshipLastContact;
                case 0x2D98FAF3:
                    return DBRequestType.UpdatePreferedLanguageByID;
                case 0x1060F3A1:
                    return DBRequestType.RenameAvatar;
                case 0xDCE9801A:
                    return DBRequestType.AcceptPendingRoomateInv;
                case 0x6B9D76FC:
                    return DBRequestType.AcquireAvatarLease;
                case 0x2B44F0C9:
                    return DBRequestType.BuyDress;
                case 0x1D8DD55A:
                    return DBRequestType.BuyLotByAvatarID;
                case 0x4BFD8A1B:
                    return DBRequestType.BuyObject;
                case 0xCB392F06:
                    return DBRequestType.DeleteAttributeByAvatarID;
                case 0x6A52E10C:
                    return DBRequestType.DeleteAvatarByID;
                case 0x2A3C81DB:
                    return DBRequestType.DeleteBookmarks;
                case 0x0B25CD36:
                    return DBRequestType.DeleteObject;
                case 0x6B8D6F5D:
                    return DBRequestType.DeleteSpotlightTextByLotID;
                case 0xA95BB3D7:
                    return DBRequestType.GetAvatarIDByName;
                case 0x2BFD89FF:
                    return DBRequestType.GetAvatarInventoryByID;
                case 0x0BA7F82C:
                    return DBRequestType.GetBookmarks;
                case 0x7BAE5079:
                    return DBRequestType.GetCharByID;
                case 0x2A3927A2:
                    return DBRequestType.GetCityType;
                case 0x4A3D75DA:
                    return DBRequestType.GetDataServiceBuildableMapInfoByXY;
                case 0x8A2456E6:
                    return DBRequestType.GetDataServiceLotAdmitInfo;
                case 0xE9C81D19:
                    return DBRequestType.GetDataServiceLotInfoByXY;
                case 0xE98A1700:
                    return DBRequestType.GetDebitCreditTask;
                case 0x9FE8B670:
                    return DBRequestType.GetGenericFlash;
                case 0xDD909124:
                    return DBRequestType.GetHouseLeaderByLotID;
                case 0xEFAB815F:
                    return DBRequestType.GetObjectTuningVariables;
                case 0x9525BE9F:
                    return DBRequestType.GetObjectsAndCatalog;
                case 0xABCD8986:
                    return DBRequestType.CreateObject;
                case 0x17C62EA8:
                    return DBRequestType.GetSelector;
                case 0xCD999E31:
                    return DBRequestType.GetGameConstants;
                case 0x688188BA:
                    return DBRequestType.RecordFeedEvent;
                case 0x7C24F627:
                    return DBRequestType.DebitCredit;
                case 0xAB4A6098:
                    return DBRequestType.GetAllRelationshipsByID;
                case 0x4AA84714:
                    return DBRequestType.GetDataServiceAvatarBudgetByID;
                case 0x0AA81112:
                    return DBRequestType.GetDataServiceAvatarSkillsByID;
                case 0xEAE9F7E3:
                    return DBRequestType.GetFriendshipWebCommentByID;
                case 0xFFE8AC61:
                    return DBRequestType.InsertGenericFlash;
                case 0xABFD8986:
                    return DBRequestType.SecureTrade;
                case 0xCB9DE627:
                    return DBRequestType.SetDataServiceAvatarVarSkillLockByID;
                case 0xAAD4C4D5:
                    return DBRequestType.SetLotCategory;
                case 0xEAF53BFB:
                    return DBRequestType.SetLotNeighborhoodID;
                case 0x8B0847D4:
                    return DBRequestType.SetNeighborhoodCenterGridXY;
                case 0x2BFDC6D0:
                    return DBRequestType.InventoryTransfer;
                case 0xA7F41927:
                    return DBRequestType.GetSelectorApproval;
                case 0x1C6A90C3:
                    return DBRequestType.DebitCreditSharedAccount;
                case 0x51F76976:
                    return DBRequestType.ExternalBankTransaction;
            }

            return DBRequestType.Unknown;
        }
    }
}
