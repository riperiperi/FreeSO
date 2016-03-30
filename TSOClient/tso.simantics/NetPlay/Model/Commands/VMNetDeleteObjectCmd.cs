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

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetDeleteObjectCmd : VMNetCommandBodyAbstract
    {
        public short ObjectID;
        public bool CleanupAll;
        public override bool Execute(VM vm)
        {
            VMEntity obj = vm.GetObjectById(ObjectID);
            var avaEnt = vm.Entities.FirstOrDefault(x => x.PersistID == ActorUID);
            if (obj == null || avaEnt == null || (obj is VMAvatar) || !(avaEnt is VMAvatar)) return false;
            obj.Delete(CleanupAll, vm.Context);

            var avatar = (VMAvatar)avaEnt;
            vm.SignalChatEvent(new VMChatEvent(avaEnt.PersistID, VMChatEventType.Arch,
                avatar.Name,
                vm.GetUserIP(avaEnt.PersistID),
                "deleted " + obj.ToString()
            ));

            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ObjectID);
            writer.Write(CleanupAll);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ObjectID = reader.ReadInt16();
            CleanupAll = reader.ReadBoolean();
        }

        #endregion
    }
}
