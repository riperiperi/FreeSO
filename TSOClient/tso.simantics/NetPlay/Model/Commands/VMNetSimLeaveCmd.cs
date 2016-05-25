/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetSimLeaveCmd : VMNetCommandBodyAbstract
    {
        private readonly ushort LEAVE_LOT_TREE = 8373;

        public override bool Execute(VM vm)
        {
            var sim = vm.Entities.FirstOrDefault(x => x is VMAvatar && x.PersistID == ActorUID);

            if (sim != null && !sim.Dead)
            {
                // the user has left the lot with their sim still on it...
                // force leave lot. generate an action with incredibly high priority and cancel current
                // TODO: timeout for forceful removal

                ((VMAvatar)sim).SetPersonData(SimAntics.Model.VMPersonDataVariable.RenderDisplayFlags, 1);
                var actions = new List<VMQueuedAction>(sim.Thread.Queue);
                foreach (var action in actions)
                {
                    sim.Thread.CancelAction(action.UID);
                }
                
                var tree = sim.GetBHAVWithOwner(LEAVE_LOT_TREE, vm.Context);
                var routine = vm.Assemble(tree.bhav);

                sim.Thread.EnqueueAction(
                    new FSO.SimAntics.Engine.VMQueuedAction
                    {
                        Callee = sim,
                        CodeOwner = tree.owner,
                        ActionRoutine = routine,
                        Name = "Leave Lot",
                        StackObject = sim,
                        Args = new short[4],
                        InteractionNumber = -1,
                        Priority = short.MaxValue,
                        Flags = TTABFlags.Leapfrog | TTABFlags.MustRun
                    }
                );
            }
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
        }
        #endregion
    }
}
