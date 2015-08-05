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
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.Netplay.Model.Commands
{
    public class VMNetSimJoinCmd : VMNetCommandBodyAbstract
    {
        public uint SimID;
        public ushort Version = CurVer;

        public static ushort CurVer = 0xFFFF;

        public override bool Execute(VM vm)
        {
            var sim = vm.Context.CreateObjectInstance(VMAvatar.TEMPLATE_PERSON, LotTilePos.OUT_OF_WORLD, Direction.NORTH).Objects[0];
            var mailbox = vm.Entities.First(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));

            VMFindLocationFor.FindLocationFor(sim, mailbox, vm.Context);
            sim.PersistID = SimID;
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(SimID);
            writer.Write(Version);
        }

        public override void Deserialize(BinaryReader reader)
        {
            SimID = reader.ReadUInt32();
            Version = reader.ReadUInt16();
        }
        #endregion
    }
}
