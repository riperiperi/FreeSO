/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Text;
using FSO.Files.HIT;

namespace FSO.Content.Model
{
    public class HITEventRegistration
    {
        public string Name;
        public HITEvents EventType;
        public uint TrackID;
        public HITResourceGroup ResGroup; //used to access this event's hit code
    }
}
