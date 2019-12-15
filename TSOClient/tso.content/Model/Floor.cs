﻿/*
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
    /// <summary>
    /// A floor resource.
    /// </summary>
    public class Floor
    {
        public ushort ID;
        public SPR2 Near;
        public SPR2 Medium;
        public SPR2 Far;
    }
}
