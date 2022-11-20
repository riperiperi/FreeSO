/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Model
{
    public enum VMGenericTSOCallMode : byte
    {
        HouseTutorialComplete = 0,
        SwapMyAndStackObjectsSlots = 1,
        SetActionIconToStackObject = 2,
        DoNotUse = 3,
        IsStackObjectARoommate = 4,
        CombineAssetsOfFamilyInTemp0 = 5,
        RemoveFromFamily = 6,
        MakeNewNeighbor = 7, //this one is "depracated"
        FamilySims1TutorialComplete = 8,
        ArchitectureSims1TutorialComplete = 9,
        DisableBuildAndBuy = 10,
        EnableBuildAndBuy = 11,
        GetDistanceToCameraInTemp0 = 12,
        AbortInteractions = 13,
        HouseRadioStationEqualsTemp0 = 14,
        MyRoutingFootprintEqualsTemp0 = 15,
        ChangeNormalOutfit = 16, //changes the normal outfit of the sim to the next available suit
        GetInteractionResult = 17, //true for good, false for bad
        SetInteractionResult = 18, //not implemented as of prealpha?
        DoIOwnThisObject = 19,
        DoesTheLocalMachineSimOwnMe = 20,
        MakeMeStackObjectsOwner = 21,
        GetPermissions = 22,
        SetPermissions = 23,
        AskStackObjectToBeRoommate = 24, //see edith description, puts result in temp 0.
        LeaveLot = 25,
        UNUSED = 26,
        KickoutRoommate = 27,
        KickoutVisitor = 28,
        StackObjectOwnerID = 29,
        CreateCheatNeighbor = 30, //after this ur on your own (post-alpha)
        IsTemp0AvatarIgnoringTemp1Avatar = 31,
        PlayNextSongOnRadioStationInTemp0 = 32,
        Temp0AvatarUnignoreTemp1Avatar = 33,
        GlobalRepairCostInTempXL0 = 34,
        GlobalRepairObjectState = 35,
        IsGlobalBroken = 36, //true if object is broken
        unused37 = 37,
        MayAddRoommate = 38,
        ReturnLotCategory = 39,
        TestStackObject = 40,
        GetCurrentValue = 41,
        IsRegionEmpty = 42,
        SetSpotlightStatus = 43,
        IsFullRefund = 44,
        RefreshBuyAndBuildMode = 45,
        GetLotOwner = 46,
        CopyDynObjNameFromTemp0ToTemp1 = 47,
        GetIsPendingDeletion = 48,
        PetRanAway = 49,
        SetOriginalPurchasePrice = 50,
        HasTemporaryID = 51,
        SetStackObjOwnerToTemp0 = 52,
        IsOnEditableTile = 53,
        SetStackObjectsCrafterNameToAvatarInTemp0 = 54,
        CalcHarvestComponents = 55,
        IsStackObjectForSale = 56,

        //EA-Land functions
        PayPalCashIn = 57,
        PayPalCashOut = 58,
        ReorderCustomObjectXTemp0 = 59,
        GetStackObjectReorderCost = 60,
        EjectCustomBitmap = 61,
        LaunchEditInUserMode = 62,
        SetTemp0ToFirstSkinID = 63,
        BroadcastDevice = 64,

        //FSO functions
        FSOLightRGBFromTemp012 = 128,
        FSOAbortAllInteractions = 129,
        FSOClearStackObjRelationships = 130,
        FSOMarkDonated = 131,
        FSOAccurateDirectionInTemp0 = 132,
        FSOCanBreak = 133,
        FSOBreakObject = 134,
        FSOSetWeatherTemp0 = 135,
        FSOReturnNeighborhoodID = 136,
        FSOGoToLotIDTemp01 = 137,
        FSOIsStackObjectTradable = 138,
        FSOSetStackObjectTransient = 139,
        FSOIsStackObjectPendingRoommateDeletion = 140,
        FSOIsStackObjectAllowedByLotCategory = 141
    }
}
