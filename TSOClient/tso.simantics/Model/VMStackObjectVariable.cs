﻿/*
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
    public enum VMStackObjectVariable
    {
        Invalid = -1,
        Graphic = 0,
        Direction = 1,
        ContainerId = 2,
        SlotNumber = 3,
        AllowedHeightFlags = 4,
        WallAdjacencyFlags = 5,
        RouteId = 6,
        RoomImpact = 7,
        Flags = 8,
        RoutePrefference = 9,
        RoutePenalty = 10,
        ObjectId = 11,
        TargetId = 12,
        WallPlacementFlags = 13,
        SlotHierNumber = 14,
        RepairState = 15,
        LightSource = 16,
        WalkStyle = 17,
        SimAge = 18,
        UnusedGender = 19,
        TreeTableEntry = 20,
        BirthMinutes = 21,
        Speed = 22,
        RotationNotches = 23,
        BirthHour = 24,
        LockoutCount = 25,
        ParentId = 26,
        Weight = 27,
        SupportStrength = 28,
        Room = 29,
        RoomPlacement = 30,
        PrepValue = 31,
        CookValue = 32,
        SurfaceValue = 33,
        Hidden = 34,
        Temperature = 35,
        DisposeValue = 36,
        WashDishValue = 37,
        EatingSurfaceValue = 38,
        DirtyLevel = 39,
        FlagField2 = 40,
        CurrentValue = 41,
        PlacementFlags = 42,
        MovementFlags = 43, //players can move it, players can delete it, stays after evict
        MaximumGrade = 44,
        BirthYear = 45,
        BirthMonth = 46,
        BirthDay = 47,
        AgeInMonths = 48,
        Size = 49,
        HideInteraction = 50,
        LightingContribution = 51,
        PrimitiveResult = 52,
        WallBlockFlags = 53,
        PrimitiveResultID = 54,
        GlobalWearRepairState = 55,
        AgeInvenQtrDaysStart = 56,
        AgeQtrDays = 57,
        ObjectVersion = 58,
        Category = 59,
        SimIndependent = 60,
        ServingSurfaceValue = 61,
        UseCount = 62,
        ExclusivePlacementFlags = 63,
        GardeningValue = 64,
        WashHandsValue = 65,
        FunctionScore = 66,
        SlotCount = 67,
        PersistedFlags = 68,
        kUnused_RandomSeedLO = 69, //that's a fun name...
        SalePriceHi = 70,
        SalePriceLo = 71,
        GroupID = 72,
        Reserved73 = 73,
        Reserved74 = 74,
        Reserved75 = 75,
        Reserved76 = 76,
        Reserved77 = 77,
        Reserved78 = 78,
        FSOEngineQuery = 79
    }
}
