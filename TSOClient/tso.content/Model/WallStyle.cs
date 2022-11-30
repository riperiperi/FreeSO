/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.Content.Model
{
    public class WallStyle
    {
        public ushort ID;
        public string Name;
        public string Description;
        public int Price;
        public SPR WallsUpNear;
        public SPR WallsUpMedium;
        public SPR WallsUpFar;
        //for most fences, the following will be null. This means to use the ones above when walls are down.
        public SPR WallsDownNear;
        public SPR WallsDownMedium;
        public SPR WallsDownFar;

        public bool IsDoor; // Set at runtime for dynamic wall styles.
    }
}
