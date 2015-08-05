/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Netplay.Model
{
    public abstract class VMNetCommandBodyAbstract : VMSerializable
    {
        public abstract bool Execute(VM vm);

        public abstract void Deserialize(BinaryReader reader);
        public abstract void SerializeInto(BinaryWriter writer);
    }
}
