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
    /// <summary>
    /// Causes a sim to begin being deleted. Can be user initiated, but they will be disconnected when their sim is fully gone.
    /// </summary>
    public class VMNetSimLeaveCmd : VMNetCommandBodyAbstract
    {
        public override bool Execute(VM vm, VMAvatar sim)
        {
            if (sim != null && !sim.Dead)
            {
                // the user has left the lot with their sim still on it...
                // force leave lot. generate an action with incredibly high priority and cancel current
                // TODO: timeout for forceful removal

                sim.UserLeaveLot();
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
