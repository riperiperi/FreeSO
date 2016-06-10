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
using FSO.SimAntics.Primitives;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetDeleteObjectCmd : VMNetCommandBodyAbstract
    {
        public short ObjectID;
        public bool CleanupAll;
        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (((VMTSOAvatarState)caller.TSOState).Permissions < VMTSOAvatarPermissions.Roommate) return false;
            VMEntity obj = vm.GetObjectById(ObjectID);
            if (obj == null || caller == null || (obj is VMAvatar)) return false;
            obj.Delete(CleanupAll, vm.Context);

            //TODO: Check if user is owner of object. Maybe sendback to owner inventory if a roommate deletes.

            // If we're the server, tell the global server to give their money back.
            if (vm.GlobalLink != null)
            {
                vm.GlobalLink.PerformTransaction(vm, false, uint.MaxValue, caller.PersistID, obj.MultitileGroup.Price,
                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    vm.SendCommand(new VMNetAsyncResponseCmd(0, new VMTransferFundsState
                    { //update budgets on clients. id of 0 means there is no target thread.
                        Responded = true,
                        Success = success,
                        TransferAmount = transferAmount,
                        UID1 = uid1,
                        Budget1 = budget1,
                        UID2 = uid2,
                        Budget2 = budget2
                    }));
                });
            }

            vm.SignalChatEvent(new VMChatEvent(caller.PersistID, VMChatEventType.Arch,
                caller.Name,
                vm.GetUserIP(caller.PersistID),
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
