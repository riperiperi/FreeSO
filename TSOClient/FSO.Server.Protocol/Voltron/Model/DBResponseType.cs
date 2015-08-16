using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Server.Protocol.Voltron.Model
{
    public enum DBResponseType
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
        ReleaseAvatarLease,
        RenewAvatarLease,
        SaveAvatarByID,
        SaveLotAndObjectBlobByID,
        Search,
        SearchExactMatch,
        SellObject,
        SetFriendshipComment,
        SetLotDesc,
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

    public static class DBResponseTypeUtils
    {
        public static uint GetResponseID(this DBResponseType type)
        {
            switch (type)
            {
                case DBResponseType.GetHouseThumbByID:
                    return 0x9BF19573;
                case DBResponseType.GetLotAndObjects:
                    return 0x8C6B0A49;
                case DBResponseType.GetLotList:
                    return 0xDBEECD65;
                case DBResponseType.GetMaxPlayerPerLot:
                    return 0xDF0A0528;
                case DBResponseType.GetNeighborhoods:
                    return 0x6AE0FD6E;
                case DBResponseType.GetShardVersion:
                    return 0x9E2095BE;
                case DBResponseType.GetTopList:
                    return 0xA928455B;
                case DBResponseType.GetTopResultSetByID:
                    return 0xFCD03A1B;
                case DBResponseType.InsertBookmarks:
                    return 0x69CBE384;
                case DBResponseType.InsertGenericTask:
                    return 0xA98B6783;
                case DBResponseType.InsertNeighborhoods:
                    return 0x4AE12821;
                case DBResponseType.InsertNewAvatar:
                    return 0x0A3C127F;
                case DBResponseType.InsertNewFriendshipComment:
                    return 0xEAE8E26F;
                case DBResponseType.InsertPendingRoomateInv:
                    return 0xBCE989F6;
                case DBResponseType.InsertSpotlightTextByLotID:
                    return 0x0B8AE5E6;
                case DBResponseType.MoveOutByAvatarID:
                    return 0x8CEEB761;
                case DBResponseType.LoadAvatarByID:
                    return 0x2ADF8FF5;
                case DBResponseType.MoveLotByID:
                    return 0xAB4266CE;
                case DBResponseType.PrtControlToggleByAvatarID:
                    return 0x6A53F3F6;
                case DBResponseType.ReleaseAvatarLease:
                    return 0xAB9EB010;
                case DBResponseType.RenewAvatarLease:
                    return 0x2B9EB082;
                case DBResponseType.SaveAvatarByID:
                    return 0x2ADDE27E;
                case DBResponseType.SaveLotAndObjectBlobByID:
                    return 0xCBFD899C;
                case DBResponseType.Search:
                    return 0xC94837CC;
                case DBResponseType.SearchExactMatch:
                    return 0x89527401;
                case DBResponseType.SellObject:
                    return 0x8BFD895F;
                case DBResponseType.SetFriendshipComment:
                    return 0xAAE0AE66;
                case DBResponseType.SetLotDesc:
                    return 0x4A70B913;
                case DBResponseType.SetLotName:
                    return 0xAA70B969;
                case DBResponseType.SetMoneyFields:
                    return 0xFCF14801;
                case DBResponseType.StockDress:
                    return 0x4B4510D5;
                case DBResponseType.UpdateBadgeByID:
                    return 0xCAFB31A4;
                case DBResponseType.UpdateCharDescByID:
                    return 0x6A3FED66;
                case DBResponseType.UpdateDataServiceLotAdmitInfo_AddAdmittedID:
                    return 0xAA270399;
                case DBResponseType.UpdateDataServiceLotAdmitInfo_AddBannedID:
                    return 0xCA2703AF;
                case DBResponseType.UpdateDataServiceLotAdmitInfo_RemoveAdmittedID:
                    return 0xCA2703C0;
                case DBResponseType.UpdateDataServiceLotAdmitInfo_RemoveBannedID:
                    return 0xEA2703DD;
                case DBResponseType.UpdateDataServiceLotAdmitInfo_SetAdmitMode:
                    return 0x0A2703F4;
                case DBResponseType.GetSpotlightLotList:
                    return 0xCBD4DD94;
                case DBResponseType.GetFinancialDetail:
                    return 0x4C23D6C1;
                case DBResponseType.GetOnlineJobLot:
                    return 0x2C3BC494;
                case DBResponseType.OnlineJobLotDesactivate:
                    return 0x8C509438;
                case DBResponseType.OnlineJobLotRequestDesactivation:
                    return 0x4C9D236B;
                case DBResponseType.OnlineJobOccupantDesactivate:
                    return 0xEC96E1E7;
                case DBResponseType.UpdatePrivacyModeByID:
                    return 0x20C61C89;
                case DBResponseType.GetDataUpdateEventsLastSeqID:
                    return 0x0BB12839;
                case DBResponseType.GetDataUpdateEvents:
                    return 0x0B5E202B;
                case DBResponseType.GetNeighborhoodInfo:
                    return 0x0B7ADA36;
                case DBResponseType.CallCreateFriends:
                    return 0x61586EB4;
                case DBResponseType.CallDecayRelationships:
                    return 0xA1586F52;
                case DBResponseType.UpdateRelationshipLastContact:
                    return 0xCD33BF75;
                case DBResponseType.RenameAvatar:
                    return 0xB061033E;
                case DBResponseType.AcceptPendingRoomateInv:
                    return 0xDCE98A01;
                case DBResponseType.AcquireAvatarLease:
                    return 0x4B9D77CA;
                case DBResponseType.BuyDress:
                    return 0xCB44F065;
                case DBResponseType.BuyLotByAvatarID:
                    return 0xBD8DDB9B;
                case DBResponseType.BuyObject:
                    return 0x4BFD8A12;
                case DBResponseType.DeleteAttributeByAvatarID:
                    return 0xCB393006;
                case DBResponseType.DeleteAvatarByID:
                    return 0x4A52E0E6;
                case DBResponseType.DeleteBookmarks:
                    return 0x69CBE292;
                case DBResponseType.DeleteObject:
                    return 0x2B219B40;
                case DBResponseType.DeleteSpotlightTextByLotID:
                    return 0x2B8D6F0B;
                case DBResponseType.GetAvatarIDByName:
                    return 0xE95BB42F;
                case DBResponseType.GetAvatarInventoryByID:
                    return 0x2BFD89F5;
                case DBResponseType.GetBookmarks:
                    return 0x3D8F9003;
                case DBResponseType.GetCharByID:
                    return 0x1BAE532A;
                case DBResponseType.GetCityType:
                    return 0x4A3929BB;
                case DBResponseType.GetDataServiceBuildableMapInfoByXY:
                    return 0x6A3D7A09;
                case DBResponseType.GetDataServiceLotAdmitInfo:
                    return 0xEA245646;
                case DBResponseType.GetDataServiceLotInfoByXY:
                    return 0xE9C81D0A;
                case DBResponseType.GetDebitCreditTask:
                    return 0x898A168C;
                case DBResponseType.GetGenericFlash:
                    return 0x3FE8B5A4;
                case DBResponseType.GetHouseLeaderByLotID:
                    return 0xBD90911F;
                case DBResponseType.GetObjectTuningVariables:
                    return 0xD6327954;
                case DBResponseType.GetObjectsAndCatalog:
                    return 0x6E8EAC4E;
                case DBResponseType.CreateObject:
                    return 0xABCD897C;
                case DBResponseType.GetSelector:
                    return 0x2C371266;
                case DBResponseType.GetGameConstants:
                    return 0xE1299B7A;
                case DBResponseType.RecordFeedEvent:
                    return 0x41C22C14;
                case DBResponseType.DebitCredit:
                    return 0x3C24F6BC;
                case DBResponseType.GetAllRelationshipsByID:
                    return 0xCB4A60AC;
                case DBResponseType.GetDataServiceAvatarBudgetByID:
                    return 0x4AA84819;
                case DBResponseType.GetDataServiceAvatarSkillsByID:
                    return 0x8AA81196;
                case DBResponseType.GetFriendshipWebCommentByID:
                    return 0x0AE9F8F1;
                case DBResponseType.InsertGenericFlash:
                    return 0x3FF83299;
                case DBResponseType.SecureTrade:
                    return 0xABFD897C;
                case DBResponseType.SetDataServiceAvatarVarSkillLockByID:
                    return 0xAB9DE815;
                case DBResponseType.SetLotCategory:
                    return 0xCAD4C4F8;
                case DBResponseType.SetLotNeighborhoodID:
                    return 0x0AF53C18;
                case DBResponseType.SetNeighborhoodCenterGridXY:
                    return 0xAB0848EF;
                case DBResponseType.InventoryTransfer:
                    return 0x4BFDC6DB;
                case DBResponseType.GetSelectorApproval:
                    return 0xA46A18AD;
                case DBResponseType.DebitCreditSharedAccount:
                    return 0xBCD04D22;
                case DBResponseType.ExternalBankTransaction:
                    return 0xF898CE34;
            }
            return 0;
        }

        public static DBResponseType FromResponseID(uint id)
        {
            switch (id)
            {
                case 0x9BF19573:
                    return DBResponseType.GetHouseThumbByID;
                case 0x8C6B0A49:
                    return DBResponseType.GetLotAndObjects;
                case 0xDBEECD65:
                    return DBResponseType.GetLotList;
                case 0xDF0A0528:
                    return DBResponseType.GetMaxPlayerPerLot;
                case 0x6AE0FD6E:
                    return DBResponseType.GetNeighborhoods;
                case 0x9E2095BE:
                    return DBResponseType.GetShardVersion;
                case 0xA928455B:
                    return DBResponseType.GetTopList;
                case 0xFCD03A1B:
                    return DBResponseType.GetTopResultSetByID;
                case 0x69CBE384:
                    return DBResponseType.InsertBookmarks;
                case 0xA98B6783:
                    return DBResponseType.InsertGenericTask;
                case 0x4AE12821:
                    return DBResponseType.InsertNeighborhoods;
                case 0x0A3C127F:
                    return DBResponseType.InsertNewAvatar;
                case 0xEAE8E26F:
                    return DBResponseType.InsertNewFriendshipComment;
                case 0xBCE989F6:
                    return DBResponseType.InsertPendingRoomateInv;
                case 0x0B8AE5E6:
                    return DBResponseType.InsertSpotlightTextByLotID;
                case 0x8CEEB761:
                    return DBResponseType.MoveOutByAvatarID;
                case 0x2ADF8FF5:
                    return DBResponseType.LoadAvatarByID;
                case 0xAB4266CE:
                    return DBResponseType.MoveLotByID;
                case 0x6A53F3F6:
                    return DBResponseType.PrtControlToggleByAvatarID;
                case 0xAB9EB010:
                    return DBResponseType.ReleaseAvatarLease;
                case 0x2B9EB082:
                    return DBResponseType.RenewAvatarLease;
                case 0x2ADDE27E:
                    return DBResponseType.SaveAvatarByID;
                case 0xCBFD899C:
                    return DBResponseType.SaveLotAndObjectBlobByID;
                case 0xC94837CC:
                    return DBResponseType.Search;
                case 0x89527401:
                    return DBResponseType.SearchExactMatch;
                case 0x8BFD895F:
                    return DBResponseType.SellObject;
                case 0xAAE0AE66:
                    return DBResponseType.SetFriendshipComment;
                case 0x4A70B913:
                    return DBResponseType.SetLotDesc;
                case 0xAA70B969:
                    return DBResponseType.SetLotName;
                case 0xFCF14801:
                    return DBResponseType.SetMoneyFields;
                case 0x4B4510D5:
                    return DBResponseType.StockDress;
                case 0xCAFB31A4:
                    return DBResponseType.UpdateBadgeByID;
                case 0x6A3FED66:
                    return DBResponseType.UpdateCharDescByID;
                case 0xAA270399:
                    return DBResponseType.UpdateDataServiceLotAdmitInfo_AddAdmittedID;
                case 0xCA2703AF:
                    return DBResponseType.UpdateDataServiceLotAdmitInfo_AddBannedID;
                case 0xCA2703C0:
                    return DBResponseType.UpdateDataServiceLotAdmitInfo_RemoveAdmittedID;
                case 0xEA2703DD:
                    return DBResponseType.UpdateDataServiceLotAdmitInfo_RemoveBannedID;
                case 0x0A2703F4:
                    return DBResponseType.UpdateDataServiceLotAdmitInfo_SetAdmitMode;
                case 0xCBD4DD94:
                    return DBResponseType.GetSpotlightLotList;
                case 0x4C23D6C1:
                    return DBResponseType.GetFinancialDetail;
                case 0x2C3BC494:
                    return DBResponseType.GetOnlineJobLot;
                case 0x8C509438:
                    return DBResponseType.OnlineJobLotDesactivate;
                case 0x4C9D236B:
                    return DBResponseType.OnlineJobLotRequestDesactivation;
                case 0xEC96E1E7:
                    return DBResponseType.OnlineJobOccupantDesactivate;
                case 0x20C61C89:
                    return DBResponseType.UpdatePrivacyModeByID;
                case 0x0BB12839:
                    return DBResponseType.GetDataUpdateEventsLastSeqID;
                case 0x0B5E202B:
                    return DBResponseType.GetDataUpdateEvents;
                case 0x0B7ADA36:
                    return DBResponseType.GetNeighborhoodInfo;
                case 0x61586EB4:
                    return DBResponseType.CallCreateFriends;
                case 0xA1586F52:
                    return DBResponseType.CallDecayRelationships;
                case 0xCD33BF75:
                    return DBResponseType.UpdateRelationshipLastContact;
                case 0xB061033E:
                    return DBResponseType.RenameAvatar;
                case 0xDCE98A01:
                    return DBResponseType.AcceptPendingRoomateInv;
                case 0x4B9D77CA:
                    return DBResponseType.AcquireAvatarLease;
                case 0xCB44F065:
                    return DBResponseType.BuyDress;
                case 0xBD8DDB9B:
                    return DBResponseType.BuyLotByAvatarID;
                case 0x4BFD8A12:
                    return DBResponseType.BuyObject;
                case 0xCB393006:
                    return DBResponseType.DeleteAttributeByAvatarID;
                case 0x4A52E0E6:
                    return DBResponseType.DeleteAvatarByID;
                case 0x69CBE292:
                    return DBResponseType.DeleteBookmarks;
                case 0x2B219B40:
                    return DBResponseType.DeleteObject;
                case 0x2B8D6F0B:
                    return DBResponseType.DeleteSpotlightTextByLotID;
                case 0xE95BB42F:
                    return DBResponseType.GetAvatarIDByName;
                case 0x2BFD89F5:
                    return DBResponseType.GetAvatarInventoryByID;
                case 0x3D8F9003:
                    return DBResponseType.GetBookmarks;
                case 0x1BAE532A:
                    return DBResponseType.GetCharByID;
                case 0x4A3929BB:
                    return DBResponseType.GetCityType;
                case 0x6A3D7A09:
                    return DBResponseType.GetDataServiceBuildableMapInfoByXY;
                case 0xEA245646:
                    return DBResponseType.GetDataServiceLotAdmitInfo;
                case 0xE9C81D0A:
                    return DBResponseType.GetDataServiceLotInfoByXY;
                case 0x898A168C:
                    return DBResponseType.GetDebitCreditTask;
                case 0x3FE8B5A4:
                    return DBResponseType.GetGenericFlash;
                case 0xBD90911F:
                    return DBResponseType.GetHouseLeaderByLotID;
                case 0xD6327954:
                    return DBResponseType.GetObjectTuningVariables;
                case 0x6E8EAC4E:
                    return DBResponseType.GetObjectsAndCatalog;
                case 0xABCD897C:
                    return DBResponseType.CreateObject;
                case 0x2C371266:
                    return DBResponseType.GetSelector;
                case 0xE1299B7A:
                    return DBResponseType.GetGameConstants;
                case 0x41C22C14:
                    return DBResponseType.RecordFeedEvent;
                case 0x3C24F6BC:
                    return DBResponseType.DebitCredit;
                case 0xCB4A60AC:
                    return DBResponseType.GetAllRelationshipsByID;
                case 0x4AA84819:
                    return DBResponseType.GetDataServiceAvatarBudgetByID;
                case 0x8AA81196:
                    return DBResponseType.GetDataServiceAvatarSkillsByID;
                case 0x0AE9F8F1:
                    return DBResponseType.GetFriendshipWebCommentByID;
                case 0x3FF83299:
                    return DBResponseType.InsertGenericFlash;
                case 0xABFD897C:
                    return DBResponseType.SecureTrade;
                case 0xAB9DE815:
                    return DBResponseType.SetDataServiceAvatarVarSkillLockByID;
                case 0xCAD4C4F8:
                    return DBResponseType.SetLotCategory;
                case 0x0AF53C18:
                    return DBResponseType.SetLotNeighborhoodID;
                case 0xAB0848EF:
                    return DBResponseType.SetNeighborhoodCenterGridXY;
                case 0x4BFDC6DB:
                    return DBResponseType.InventoryTransfer;
                case 0xA46A18AD:
                    return DBResponseType.GetSelectorApproval;
                case 0xBCD04D22:
                    return DBResponseType.DebitCreditSharedAccount;
                case 0xF898CE34:
                    return DBResponseType.ExternalBankTransaction;
            }
            return DBResponseType.Unknown;
        }
    }
}
