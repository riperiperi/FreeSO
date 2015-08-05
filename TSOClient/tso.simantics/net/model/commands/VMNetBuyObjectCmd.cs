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
    public class VMNetBuyObjectCmd : VMNetCommandBodyAbstract
    {
        public uint GUID;
        public short x;
        public short y;
        public sbyte level;
        public Direction dir;

        private List<uint> Blacklist = new List<uint>
        {
            0x24C95F99
        };

        public override bool Execute(VM vm)
        {
            if (Blacklist.Contains(GUID)) return false;
            var group = vm.Context.CreateObjectInstance(GUID, new LotTilePos(x, y, level), dir);
            if (group.BaseObject.Position == LotTilePos.OUT_OF_WORLD)
            {
                group.ExecuteEntryPoint(11, vm.Context); //User Placement
                group.Delete(vm.Context);
                return false;
            }
            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(GUID);
            writer.Write(x);
            writer.Write(y);
            writer.Write(level);
            writer.Write((byte)dir);
        }

        public override void Deserialize(BinaryReader reader)
        {
            GUID = reader.ReadUInt32();
            x = reader.ReadInt16();
            y = reader.ReadInt16();
            level = reader.ReadSByte();
            dir = (Direction)reader.ReadByte();
        }

        #endregion
    }
}
