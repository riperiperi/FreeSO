/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.SimAntics.Engine;
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
        public bool Global;
        public short CallerID;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            VMEntity callee = vm.GetObjectById(CalleeID);
            if (caller == null && CallerID > 0)
            {
                //try get caller from normal id
                caller = (vm.GetObjectById(CallerID) as VMAvatar);
                if (caller == null) return false;
            }
            if (callee == null) return false;
            if (callee is VMGameObject && ((VMGameObject)callee).Disabled > 0) return false;
            if ((caller.Thread?.Queue?.Count ?? 0) >= VMThread.MAX_USER_ACTIONS) return false;
            callee.PushUserInteraction(Interaction, caller, vm.Context, Global, new short[] { Param0, 0, 0, 0 });

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (caller == null && FromNet) return false;

            if (!FromNet) return true;
            VMEntity callee = vm.GetObjectById(CalleeID);
            if (caller == null && CallerID > 0)
            {
                //try get caller from normal id
                caller = (vm.GetObjectById(CallerID) as VMAvatar);
                if (caller == null) return false;
            }
            if (callee == null) return false;
            if (callee is VMGameObject && ((VMGameObject)callee).Disabled > 0) return false;
            if ((caller.Thread?.Queue?.Count ?? 0) >= VMThread.MAX_USER_ACTIONS) return false;

            var oldInUse = callee.GetFlag(VMEntityFlags.Occupied);
            callee.SetFlag(VMEntityFlags.Occupied, false); //ignore this for now - it only matters when we're running the interaction
            var pie = callee.GetPieMenuForInteraction(vm, caller, Interaction, Global, false);
            callee.SetFlag(VMEntityFlags.Occupied, oldInUse); //reset back to normal

            //does the pie generated for this object contain the variant the client asked for?
            return pie != null && pie.Any(x => x.Param0 == Param0);
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Interaction);
            writer.Write(CalleeID);
            writer.Write(Param0);
            writer.Write(Global);
            writer.Write(CallerID);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Interaction = reader.ReadUInt16();
            CalleeID = reader.ReadInt16();
            Param0 = reader.ReadInt16();
            Global = reader.ReadBoolean();
            CallerID = reader.ReadInt16();
        }

        #endregion
    }
}
