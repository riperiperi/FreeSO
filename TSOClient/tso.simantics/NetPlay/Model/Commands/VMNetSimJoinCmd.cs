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

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetSimJoinCmd : VMNetCommandBodyAbstract
    {
        public ushort Version = CurVer;

        public VMNetAvatarPersistState AvatarState;

        public static ushort CurVer = 0xFFEE;

        //variables used locally for deferred avatar loading

        public override bool Execute(VM vm)
        {
            var name = AvatarState.Name.Substring(0, Math.Min(AvatarState.Name.Length, 64));
            var sim = vm.Context.CreateObjectInstance(VMAvatar.TEMPLATE_PERSON, LotTilePos.OUT_OF_WORLD, Direction.NORTH).Objects[0];
            var mailbox = vm.Entities.FirstOrDefault(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));

            if (VM.UseWorld) FSO.HIT.HITVM.Get().PlaySoundEvent("lot_enter");
            if (mailbox != null) VMFindLocationFor.FindLocationFor(sim, mailbox, vm.Context, VMPlaceRequestFlags.Default);
            sim.PersistID = ActorUID;

            VMAvatar avatar = (VMAvatar)sim;
            AvatarState.Apply(avatar);

            var oldRoomCount = vm.TSOState.Roommates.Count;
            //some off lot changes may have occurred. Keep things up to date if we're caught between database sync points (TODO: right now never, but should happen on every roomie change).
            if (AvatarState.Permissions > VMTSOAvatarPermissions.Visitor && AvatarState.Permissions < VMTSOAvatarPermissions.Admin)
            {
                if (!vm.TSOState.Roommates.Contains(AvatarState.PersistID))
                {
                    vm.TSOState.Roommates.Add(AvatarState.PersistID);
                    if (AvatarState.Permissions > VMTSOAvatarPermissions.Roommate)
                        vm.TSOState.BuildRoommates.Add(AvatarState.PersistID);
                    else
                        vm.TSOState.BuildRoommates.Remove(AvatarState.PersistID);
                    VMBuildableAreaInfo.UpdateOverbudgetObjects(vm);
                }
            } else if (AvatarState.Permissions != VMTSOAvatarPermissions.Admin)
            {
                if (vm.TSOState.Roommates.Contains(AvatarState.PersistID))
                {
                    vm.TSOState.Roommates.Remove(AvatarState.PersistID);
                    vm.TSOState.BuildRoommates.Remove(AvatarState.PersistID);
                    VMBuildableAreaInfo.UpdateOverbudgetObjects(vm);
                }
            }

            if (oldRoomCount != vm.TSOState.Roommates.Count)
            {
                //mark objects not owned by roommates for inventory transfer
                foreach (var ent in vm.Entities)
                {
                    if (ent is VMGameObject && ent.PersistID > 0 && ((VMTSOObjectState)ent.TSOState).OwnerID == avatar.PersistID)
                    {
                        if (AvatarState.Permissions < VMTSOAvatarPermissions.Roommate) ((VMGameObject)ent).Disabled |= VMGameObjectDisableFlags.PendingRoommateDeletion;
                        else ((VMGameObject)ent).Disabled &= ~VMGameObjectDisableFlags.PendingRoommateDeletion;
                        ((VMGameObject)ent).RefreshLight();
                    }
                }
            }

            vm.Context.ObjectQueries.RegisterAvatarPersist(avatar, avatar.PersistID);
            if (ActorUID == uint.MaxValue - 1)
            {
                avatar.SetValue(VMStackObjectVariable.Hidden, 1);
                avatar.SetPosition(LotTilePos.OUT_OF_WORLD, Direction.NORTH, vm.Context);
                avatar.SetFlag(VMEntityFlags.HasZeroExtent, true);
                avatar.SetPersonData(VMPersonDataVariable.IsGhost, 1); //oooooOOooooOo
            }
            
            vm.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Join, avatar.Name));

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            /*

            //OLD NET ticket->data service

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
            */
            return !FromNet; //can only be sent out by server
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Version);
            AvatarState.SerializeInto(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Version = reader.ReadUInt16();
            AvatarState = new VMNetAvatarPersistState();
            AvatarState.Deserialize(reader);
        }
        #endregion
    }
}
