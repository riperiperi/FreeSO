using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.DataService.Model
{
    public enum MaskedStruct
    {
        Petition_Avatar,
        ServerMyAvatar,
        AvatarIDByNameLookup,
        MapView_RollOverInfo_Lot,
        CurrentAvatarList,
        Server_LotNeighborhoodID,
        CurrentTop100List,
        RoommatesPanel_Lot,
        MyLot_PossibleNeighborhoods,
        CurrentHouse,
        MapView_NearZoom_Lot_Thumbnail,
        SimPage_MyLot,
        MapView_RollOverInfo_Lot_Price,
        ServerTop100List_Lot,
        SimPage_DescriptionPanel,
        ServerMyLot,
        QueryPanel_Avatar,
        PropertyPage_LotInfo,
        Bookmark_Lot,
        Messaging_Message_Avatar,
        FriendshipWeb_Avatar,
        RelationshipComment,
        SimPage_SkillsPanel,
        Neighbor_Avatar,
        SimPage_JobsPanel,
        ServerNeighborhoods_City,
        Messaging_Icon_Avatar,
        Thumbnail_Avatar,
        Thumbnail_Lot,
        MapView_NearZoom_Neighborhood,
        MyAvatar,
        MyGDMInbox,
        MapView_FarZoom_Rollover_Spotlight,
        AdmitInfo_Lot,
        MyLot,
        Petition_Lot,
        CurrentCityList,
        SimPage_Main,
        Bookmark_Avatar,
        Top100List_Avatar,
        CurrentCityTopTenNeighborhoods,
        Top100List_Summary,
        MapView_NearZoom_Lot,
        CurrentCity,
        ServerCity,
        Top100List_Lot,
        ServerDataUpdateEventList,
        ServerNeighborhood,
        MapView_FarZoom_Lot,

        Unknown
    }

    public static class MaskedStructUtils
    {
        public static uint GetID(this MaskedStruct ms)
        {
            switch (ms)
            {
                case MaskedStruct.Petition_Avatar: return 55625319;
                case MaskedStruct.ServerMyAvatar: return 79448686;
                case MaskedStruct.AvatarIDByNameLookup: return 192206885;
                case MaskedStruct.MapView_RollOverInfo_Lot: return 202158566;
                case MaskedStruct.CurrentAvatarList: return 276913389;
                case MaskedStruct.Server_LotNeighborhoodID: return 311831197;
                case MaskedStruct.CurrentTop100List: return 325293329;
                case MaskedStruct.RoommatesPanel_Lot: return 342189413;
                case MaskedStruct.MyLot_PossibleNeighborhoods: return 426067697;
                case MaskedStruct.CurrentHouse: return 452882977;
                case MaskedStruct.MapView_NearZoom_Lot_Thumbnail: return 473320541;
                case MaskedStruct.SimPage_MyLot: return 555587230;
                case MaskedStruct.MapView_RollOverInfo_Lot_Price: return 578547040;
                case MaskedStruct.ServerTop100List_Lot: return 735461095;
                case MaskedStruct.SimPage_DescriptionPanel: return 966279788;
                case MaskedStruct.ServerMyLot: return 1012787489;
                case MaskedStruct.QueryPanel_Avatar: return 1078807729;
                case MaskedStruct.PropertyPage_LotInfo: return 1214988158;
                case MaskedStruct.Bookmark_Lot: return 1272196620;
                case MaskedStruct.Messaging_Message_Avatar: return 1439870519;
                case MaskedStruct.FriendshipWeb_Avatar: return 1730682788;
                case MaskedStruct.RelationshipComment: return 1957648337;
                case MaskedStruct.SimPage_SkillsPanel: return 1979695769;
                case MaskedStruct.Neighbor_Avatar: return 2007265256;
                case MaskedStruct.SimPage_JobsPanel: return 2265984954;
                case MaskedStruct.ServerNeighborhoods_City: return 2291722316;
                case MaskedStruct.Messaging_Icon_Avatar: return 2373950450;
                case MaskedStruct.Thumbnail_Avatar: return 2422387600;
                case MaskedStruct.Thumbnail_Lot: return 2483122079;
                case MaskedStruct.MapView_NearZoom_Neighborhood: return 2533543945;
                case MaskedStruct.MyAvatar: return 2758690780;
                case MaskedStruct.MyGDMInbox: return 2775928562;
                case MaskedStruct.MapView_FarZoom_Rollover_Spotlight: return 2940026733;
                case MaskedStruct.AdmitInfo_Lot: return 2999381252;
                case MaskedStruct.MyLot: return 3050713309;
                case MaskedStruct.Petition_Lot: return 3310489632;
                case MaskedStruct.CurrentCityList: return 3438992523;
                case MaskedStruct.SimPage_Main: return 3494046166;
                case MaskedStruct.Bookmark_Avatar: return 3551663818;
                case MaskedStruct.Top100List_Avatar: return 3587451849;
                case MaskedStruct.CurrentCityTopTenNeighborhoods: return 3691510047;
                case MaskedStruct.Top100List_Summary: return 3752764953;
                case MaskedStruct.MapView_NearZoom_Lot: return 3824982265;
                case MaskedStruct.CurrentCity: return 3840520797;
                case MaskedStruct.ServerCity: return 3880957778;
                case MaskedStruct.Top100List_Lot: return 3933940170;
                case MaskedStruct.ServerDataUpdateEventList: return 4204965376;
                case MaskedStruct.ServerNeighborhood: return 4236962517;
                case MaskedStruct.MapView_FarZoom_Lot: return 4249159840;
                
                default:
                    throw new Exception("Unknown masked struct");
            }
        }


        public static MaskedStruct FromID(uint id)
        {
            switch (id)
            {
                case 55625319: return MaskedStruct.Petition_Avatar;
                case 79448686: return MaskedStruct.ServerMyAvatar;
                case 192206885: return MaskedStruct.AvatarIDByNameLookup;
                case 202158566: return MaskedStruct.MapView_RollOverInfo_Lot;
                case 276913389: return MaskedStruct.CurrentAvatarList;
                case 311831197: return MaskedStruct.Server_LotNeighborhoodID;
                case 325293329: return MaskedStruct.CurrentTop100List;
                case 342189413: return MaskedStruct.RoommatesPanel_Lot;
                case 426067697: return MaskedStruct.MyLot_PossibleNeighborhoods;
                case 452882977: return MaskedStruct.CurrentHouse;
                case 473320541: return MaskedStruct.MapView_NearZoom_Lot_Thumbnail;
                case 555587230: return MaskedStruct.SimPage_MyLot;
                case 578547040: return MaskedStruct.MapView_RollOverInfo_Lot_Price;
                case 735461095: return MaskedStruct.ServerTop100List_Lot;
                case 966279788: return MaskedStruct.SimPage_DescriptionPanel;
                case 1012787489: return MaskedStruct.ServerMyLot;
                case 1078807729: return MaskedStruct.QueryPanel_Avatar;
                case 1214988158: return MaskedStruct.PropertyPage_LotInfo;
                case 1272196620: return MaskedStruct.Bookmark_Lot;
                case 1439870519: return MaskedStruct.Messaging_Message_Avatar;
                case 1730682788: return MaskedStruct.FriendshipWeb_Avatar;
                case 1957648337: return MaskedStruct.RelationshipComment;
                case 1979695769: return MaskedStruct.SimPage_SkillsPanel;
                case 2007265256: return MaskedStruct.Neighbor_Avatar;
                case 2265984954: return MaskedStruct.SimPage_JobsPanel;
                case 2291722316: return MaskedStruct.ServerNeighborhoods_City;
                case 2373950450: return MaskedStruct.Messaging_Icon_Avatar;
                case 2422387600: return MaskedStruct.Thumbnail_Avatar;
                case 2483122079: return MaskedStruct.Thumbnail_Lot;
                case 2533543945: return MaskedStruct.MapView_NearZoom_Neighborhood;
                case 2758690780: return MaskedStruct.MyAvatar;
                case 2775928562: return MaskedStruct.MyGDMInbox;
                case 2940026733: return MaskedStruct.MapView_FarZoom_Rollover_Spotlight;
                case 2999381252: return MaskedStruct.AdmitInfo_Lot;
                case 3050713309: return MaskedStruct.MyLot;
                case 3310489632: return MaskedStruct.Petition_Lot;
                case 3438992523: return MaskedStruct.CurrentCityList;
                case 3494046166: return MaskedStruct.SimPage_Main;
                case 3551663818: return MaskedStruct.Bookmark_Avatar;
                case 3587451849: return MaskedStruct.Top100List_Avatar;
                case 3691510047: return MaskedStruct.CurrentCityTopTenNeighborhoods;
                case 3752764953: return MaskedStruct.Top100List_Summary;
                case 3824982265: return MaskedStruct.MapView_NearZoom_Lot;
                case 3840520797: return MaskedStruct.CurrentCity;
                case 3880957778: return MaskedStruct.ServerCity;
                case 3933940170: return MaskedStruct.Top100List_Lot;
                case 4204965376: return MaskedStruct.ServerDataUpdateEventList;
                case 4236962517: return MaskedStruct.ServerNeighborhood;
                case 4249159840: return MaskedStruct.MapView_FarZoom_Lot;

                default:
                    return MaskedStruct.Unknown;
            }
        }
    }
}
