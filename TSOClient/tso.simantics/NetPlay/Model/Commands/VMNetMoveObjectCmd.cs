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
    public class VMNetMoveObjectCmd : VMNetCommandBodyAbstract
    {
        public short ObjectID;
        public short x;
        public short y;
        public sbyte level;
        public Direction dir;

        public override bool Execute(VM vm)
        {
            VMEntity obj = vm.GetObjectById(ObjectID);
            var avaEnt = vm.Entities.FirstOrDefault(x => x.PersistID == ActorUID);
            if (obj == null || avaEnt == null || (obj is VMAvatar) || !(avaEnt is VMAvatar)) return false;
            var result = obj.SetPosition(new LotTilePos(x, y, level), dir, vm.Context);
            if (result.Status == VMPlacementError.Success)
            {
                obj.MultitileGroup.ExecuteEntryPoint(11, vm.Context); //User Placement

                var avatar = (VMAvatar)avaEnt;
                vm.SignalChatEvent(new VMChatEvent(avaEnt.PersistID, VMChatEventType.Arch,
                    avatar.Name,
                    vm.GetUserIP(avaEnt.PersistID),
                    "moved " + obj.ToString() +" to (" + x / 16f + ", " + y / 16f + ", " + level + ")"
                ));

                return true;
            } else
            {
                return false;
            }
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ObjectID);
            writer.Write(x);
            writer.Write(y);
            writer.Write(level);
            writer.Write((byte)dir);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ObjectID = reader.ReadInt16();
            x = reader.ReadInt16();
            y = reader.ReadInt16();
            level = reader.ReadSByte();
            dir = (Direction)reader.ReadByte();
        }

        #endregion
    }
}
