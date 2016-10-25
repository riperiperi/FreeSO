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

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetInteractionCmd : VMNetCommandBodyAbstract
    {
        public ushort Interaction;
        public short CalleeID;
        public short Param0;

        public override bool Execute(VM vm)
        {
            VMEntity callee = vm.GetObjectById(CalleeID);
            VMEntity caller = vm.Entities.FirstOrDefault(x => x.PersistID == ActorUID);
            if (callee == null || caller == null) return false;
            if (callee is VMGameObject && ((VMGameObject)callee).Disabled > 0) return false;
            callee.PushUserInteraction(Interaction, caller, vm.Context, new short[] { Param0, 0, 0, 0 });

            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Interaction);
            writer.Write(CalleeID);
            writer.Write(Param0);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Interaction = reader.ReadUInt16();
            CalleeID = reader.ReadInt16();
            Param0 = reader.ReadInt16();
        }

        #endregion
    }
}
