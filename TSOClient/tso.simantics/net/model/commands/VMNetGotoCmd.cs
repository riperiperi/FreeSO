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

namespace FSO.SimAntics.Netplay.Model.Commands
{
    public class VMNetGotoCmd : VMNetCommandBodyAbstract
    {
        public ushort Interaction;
        public short CallerID;

        public short x;
        public short y;
        public sbyte level;

        private static uint GOTO_GUID = 0x000007C4;

        public override bool Execute(VM vm)
        {
            VMEntity callee = vm.Context.CreateObjectInstance(GOTO_GUID, new LotTilePos(x, y, level), Direction.NORTH).Objects[0];
            VMEntity caller = vm.GetObjectById(CallerID);
            //TODO: check if net user owns caller!
            if (callee == null || callee.Position == LotTilePos.OUT_OF_WORLD || caller == null) return false;
            callee.PushUserInteraction(Interaction, caller, vm.Context);

            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Interaction);
            writer.Write(CallerID);
            writer.Write(x);
            writer.Write(y);
            writer.Write(level);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Interaction = reader.ReadUInt16();
            CallerID = reader.ReadInt16();
            x = reader.ReadInt16();
            y = reader.ReadInt16();
            level = reader.ReadSByte();
        }

        #endregion
    }
}
