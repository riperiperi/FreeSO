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
using FSO.LotView.Model;
using FSO.SimAntics.Model;

namespace FSO.SimAntics.Netplay.Model.Commands
{
    public class VMNetDeleteObjectCmd : VMNetCommandBodyAbstract
    {
        public short ObjectID;
        public bool CleanupAll;

        public override bool Execute(VM vm)
        {
            VMEntity obj = vm.GetObjectById(ObjectID);
            if (obj == null || (obj is VMAvatar)) return false;
            obj.Delete(CleanupAll, vm.Context);
            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(CleanupAll);
        }

        public override void Deserialize(BinaryReader reader)
        {
            ObjectID = reader.ReadInt16();
            CleanupAll = reader.ReadBoolean();
        }

        #endregion
    }
}
