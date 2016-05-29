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
using FSO.SimAntics.Model.TSOPlatform;
using GonzoNet;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetSimJoinCmd : VMNetCommandBodyAbstract
    {
        public ushort Version = CurVer;

        //TODO: replace with VMPersistAvatarBlock (obtained from global server)
        
        public uint RequesterID; //just here to notify client that this join was meant for them. = old ActorUID (random)
        //do NOT make this the ticket in future. randomly distributing the auth ticket to everyone on server is a dumb idea

        public ulong HeadID;
        public ulong BodyID;
        public byte SkinTone;
        public bool Gender;
        public string Name;
        public VMTSOAvatarPermissions Permissions;

        public static ushort CurVer = 0xFFEF;

        //variables used locally for deferred avatar loading
        public bool Verified;
        public string Ticket; //right now this is ip:name. in future will be provided by city
        public NetworkClient Client; //REPLACE WHEN MOVING OFF GONZONET!!

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
            avatar.Name = Name;
            ((VMTSOAvatarState)avatar.TSOState).Permissions = Permissions;

            if (ActorUID == uint.MaxValue - 1)
            {
                avatar.SetValue(VMStackObjectVariable.Hidden, 1);
                avatar.SetPosition(LotTilePos.OUT_OF_WORLD, Direction.NORTH, vm.Context);
                avatar.SetFlag(VMEntityFlags.HasZeroExtent, true);
                avatar.SetPersonData(VMPersonDataVariable.IsGhost, 1); //oooooOOooooOo
            }

            if (RequesterID == vm.MyUID) vm.MyUID = ActorUID; //we're this sim! try send commands as them.
            vm.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Join, avatar.Name));

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            Name = Name.Replace("\r\n", "");
            if (Verified == true) return true;
            if (Ticket == null) Ticket = "local" + ":" + Name;

            var tempName = Name; //obviously not a concern for final server. but for now... prevents people logging in with 2x same persist
            int i = 1;
            while (vm.Entities.Any(x => (x is VMAvatar) && ((VMAvatar)x).Name == tempName))
            {
                tempName = Name + " (" + (i++) + ")";
            }
            Name = tempName;

            if (FromNet && RequesterID == uint.MaxValue - 1) return false; //only server can set themselves as server...
            RequesterID = ActorUID;
            vm.GlobalLink.ObtainAvatarFromTicket(vm, Ticket, (uint persistID, VMTSOAvatarPermissions permissions) =>
                {
                    //first, verify if their sim has left the lot yet. if not, they cannot join until they have left.
                    //(only really happens with an immediate rejoin)
                    if (vm.Entities.FirstOrDefault(x => x.PersistID == persistID) != null && Client != null)
                    {
                        Client.Disconnect(); //would like to send a message but need a rework of VMServerDriver to make it happen
                        return;
                    }

                    //TODO: a lot more persist state
                    this.ActorUID = persistID;
                    this.Permissions = permissions;
                    this.Verified = true;
                    vm.ForwardCommand(this);
                });
            return false;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Version);
            writer.Write(RequesterID);
            writer.Write(HeadID);
            writer.Write(BodyID);
            writer.Write(SkinTone);
            writer.Write(Gender);
            writer.Write(Name);
            writer.Write((byte)Permissions);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Version = reader.ReadUInt16();
            RequesterID = reader.ReadUInt32();
            HeadID = reader.ReadUInt64();
            BodyID = reader.ReadUInt64();
            SkinTone = reader.ReadByte();
            Gender = reader.ReadBoolean();
            Name = reader.ReadString();
            Permissions = (VMTSOAvatarPermissions)reader.ReadByte();
        }
        #endregion
    }
}
