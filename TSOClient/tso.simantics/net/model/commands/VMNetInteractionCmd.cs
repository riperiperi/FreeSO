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

namespace FSO.SimAntics.Netplay.Model.Commands
{
    public class VMNetInteractionCmd : VMNetCommandBodyAbstract
    {
        public ushort Interaction;
        public short CalleeID;
        public short CallerID;

        public override bool Execute(VM vm)
        {
            VMEntity callee = vm.GetObjectById(CalleeID);
            VMEntity caller = vm.GetObjectById(CallerID);
            //TODO: check if net user owns caller!
            if (callee == null || caller == null) return false;
            callee.PushUserInteraction(Interaction, caller, vm.Context);

            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Interaction);
            writer.Write(CalleeID);
            writer.Write(CallerID);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Interaction = reader.ReadUInt16();
            CalleeID = reader.ReadInt16();
            CallerID = reader.ReadInt16();
        }

        #endregion
    }
}
