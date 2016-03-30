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
using FSO.SimAntics.Model;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetSimJoinCmd : VMNetCommandBodyAbstract
    {
        public ushort Version = CurVer;

        public ulong HeadID;
        public ulong BodyID;
        public byte SkinTone;
        public bool Gender;
        public string Name;

        public static ushort CurVer = 0xFFF8;

        public override bool Execute(VM vm)
        {
            var sim = vm.Context.CreateObjectInstance(VMAvatar.TEMPLATE_PERSON, LotTilePos.OUT_OF_WORLD, Direction.NORTH).Objects[0];
            var mailbox = vm.Entities.FirstOrDefault(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));

            if (VM.UseWorld) FSO.HIT.HITVM.Get().PlaySoundEvent("lot_enter");
            if (mailbox != null) VMFindLocationFor.FindLocationFor(sim, mailbox, vm.Context);
            sim.PersistID = ActorUID;

            VMAvatar avatar = (VMAvatar)sim;
            avatar.SkinTone = (Vitaboy.AppearanceType)SkinTone;
            avatar.SetPersonData(VMPersonDataVariable.Gender, (short)((Gender) ? 1 : 0));
            avatar.DefaultSuits = new VMAvatarDefaultSuits(Gender);
            avatar.DefaultSuits.Daywear = BodyID;
            avatar.BodyOutfit = BodyID;
            avatar.HeadOutfit = HeadID;

            var tempName = Name;
            int i = 1;
            while (vm.Entities.Any(x => (x is VMAvatar) && ((VMAvatar)x).Name == tempName))
            {
                tempName = Name + " (" + (i++) + ")";
            }
            avatar.Name = tempName;

            vm.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Join, avatar.Name));

            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Version);
            writer.Write(HeadID);
            writer.Write(BodyID);
            writer.Write(SkinTone);
            writer.Write(Gender);
            writer.Write(Name);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Version = reader.ReadUInt16();

            HeadID = reader.ReadUInt64();
            BodyID = reader.ReadUInt64();
            SkinTone = reader.ReadByte();
            Gender = reader.ReadBoolean();
            Name = reader.ReadString();
        }
        #endregion
    }
}
