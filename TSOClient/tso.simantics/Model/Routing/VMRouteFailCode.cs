/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Model.Routing
{
    public enum VMRouteFailCode : short
    {
        Success = 0,
        Unknown = 1,
        NoRoomRoute = 2,
        NoPath = 3, //pathfind failed
        Interrupted = 4,
        CantSit = 5,
        CantStand = 6, //with blocking object
        NoValidGoals = 7,
        DestTileOccupied = 8,
        DestChairOccupied = 9, //with blocking object
        NoChair = 10,
        WallInWay = 11, 
        AltsDontMatch = 12,
        DestTileOccupiedPerson = 13 //with blocking object
    }
}
