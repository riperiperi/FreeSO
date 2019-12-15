/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.LotView.Model
{
    /// <summary>
    /// Current direction of world/camera/renderer
    /// </summary>
    public enum Direction
    {
        NORTHWEST = 0x80,
        WEST = 0x40,
        SOUTHWEST = 0x20,
        SOUTH = 0x10,
        SOUTHEAST = 0x08,
        EAST = 0x04,
        NORTHEAST = 0x02,
        NORTH = 0x01
    }
}
