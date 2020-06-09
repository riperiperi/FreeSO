/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.LotView.Model;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.Test;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetChatCmd : VMNetCommandBodyAbstract
    {
        public string Message;
        public byte ChannelID;
        public bool SayWithSim;
        public bool Verified;

        public override bool Execute(VM vm, VMAvatar avatar)
        {
            if (Message.Length == 0) return false;
            if (Message.Length > 200) Message = Message.Substring(0, 200);
            if (avatar == null) return false;

            if (Message[0] == '/' && Message.Length > 1)
            {
                var spaceIndex = Message.IndexOf(' ');
                if (spaceIndex == -1) spaceIndex = Message.Length;
                if ((FromNet && avatar.AvatarState.Permissions < VMTSOAvatarPermissions.Admin) || !(vm.Driver is VMServerDriver)) return false;
                //commands are only run from the server sim right now
                var cmd = Message.Substring(1, spaceIndex - 1);
                var args = Message.Substring(Math.Min(Message.Length, spaceIndex + 1), Math.Max(0, Message.Length - (spaceIndex + 1)));
                var server = (VMServerDriver)vm.Driver;
                VMEntity sim;
                switch (cmd.ToLowerInvariant())
                {
                    case "ban":
                        server.BanUser(vm, args);
                        break;
                    case "banip":
                        server.BanIP(args);
                        vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, "Added " + args + " to the IP ban list."));
                        break;
                    case "unban":
                        server.SandboxBans.Remove(args.ToLowerInvariant().Trim(' '));
                        vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, "Removed " + args + " from the IP ban list."));
                        break;
                    case "banlist":
                        string result = "";
                        foreach (var ban in server.SandboxBans.List()) result += ban + "\r\n";
                        vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, "==== BANNED IPS: ==== \r\n"+result));
                        break;
                    case "builder":
                        sim = vm.Entities.Where(x => x is VMAvatar && x.ToString().ToLowerInvariant().Trim(' ') == args.ToLowerInvariant().Trim(' ')).FirstOrDefault();
                        if (sim != null)
                        {
                            vm.ForwardCommand(new VMChangePermissionsCmd()
                            {
                                TargetUID = sim.PersistID,
                                Level = VMTSOAvatarPermissions.BuildBuyRoommate,
                                Verified = true
                            });
                            vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, "Made " + sim.Name + " a build-roommate."));
                        }
                        break;
                    case "admin":
                        sim = vm.Entities.Where(x => x is VMAvatar && x.ToString().ToLowerInvariant().Trim(' ') == args.ToLowerInvariant().Trim(' ')).FirstOrDefault();
                        if (sim != null)
                        {
                            vm.ForwardCommand(new VMChangePermissionsCmd()
                            {
                                TargetUID = sim.PersistID,
                                Level = VMTSOAvatarPermissions.Admin,
                                Verified = true
                            });
                            vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, "Made " + sim.Name + " an admin."));
                        }
                        break;
                    case "roomie":
                        sim = vm.Entities.Where(x => x is VMAvatar && x.ToString().ToLowerInvariant().Trim(' ') == args.ToLowerInvariant().Trim(' ')).FirstOrDefault();
                        if (sim != null)
                        {
                            vm.ForwardCommand(new VMChangePermissionsCmd()
                            {
                                TargetUID = sim.PersistID,
                                Level = VMTSOAvatarPermissions.Roommate,
                                Verified = true
                            });
                            vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, "Made " + sim.Name + " a roommate."));
                        }
                        break;
                    case "visitor":
                        sim = vm.Entities.Where(x => x is VMAvatar && x.ToString().ToLowerInvariant().Trim(' ') == args.ToLowerInvariant().Trim(' ')).FirstOrDefault();
                        if (sim != null)
                        {
                            vm.ForwardCommand(new VMChangePermissionsCmd()
                            {
                                TargetUID = sim.PersistID,
                                Level = VMTSOAvatarPermissions.Visitor,
                                Verified = true
                            });
                            vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, "Made " + sim.Name + " a visitor."));
                        }
                        break;
                    case "close":
                        if (FromNet) return false;
                        vm.CloseNet(VMCloseNetReason.ServerShutdown);
                        break;
                    case "qtrday":
                        var count = int.Parse(args);
                        for (int i=0; i<count; i++)
                        {
                            vm.ProcessQTRDay();
                        }
                        vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, "Ran "+count+" quarter days."));
                        break;
                    case "setjob":
                        var jobsplit = args.Split(' ');
                        if (jobsplit.Length < 2) return true;
                        var jobid = short.Parse(jobsplit[0]);
                        var jobgrade = short.Parse(jobsplit[1]);
                        avatar.SetPersonData(SimAntics.Model.VMPersonDataVariable.OnlineJobID, jobid);
                        avatar.SetPersonData(SimAntics.Model.VMPersonDataVariable.OnlineJobGrade, jobgrade);
                        vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, "Set "+avatar.ToString()+" job grade/type to "+jobgrade+"/"+jobid+"."));
                        break;
                    case "trace":
                        //enables desync tracing
                        vm.UseSchedule = false;
                        vm.Trace = new Engine.Debug.VMSyncTrace();
                        break;
                    case "reload":
                        //enables desync tracing
                        var servD = vm.Driver as VMServerDriver;
                        vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Debug, "Manually requested self resync."));
                        if (servD != null) servD.SelfResync = true;
                        break;
                    case "time":
                        var timesplit = args.Split(' ');
                        if (timesplit.Length < 2) return true;
                        vm.Context.Clock.Hours = int.Parse(timesplit[0]);
                        vm.Context.Clock.Minutes = int.Parse(timesplit[1]);
                        vm.Context.Clock.MinuteFractions = 0;
                        break;
                    case "tuning":
                        var tuningsplit = args.Split(' ');
                        if (tuningsplit.Length < 4) return true;
                        vm.Tuning.AddTuning(new Common.Model.DynTuningEntry()
                        {
                            tuning_type = tuningsplit[0],
                            tuning_table = int.Parse(tuningsplit[1]),
                            tuning_index = int.Parse(tuningsplit[2]),
                            value = float.Parse(tuningsplit[3]),
                        });
                        vm.ForwardCommand(new VMNetTuningCmd()
                        {
                            Tuning = vm.Tuning
                        });
                        break;
                    case "fixall":
                        var fixCount = 0;
                        foreach (var ent in vm.Entities)
                        {
                            if (ent is VMGameObject && ent == ent.MultitileGroup.BaseObject)
                            {
                                var state = (VMTSOObjectState)ent.TSOState;
                                if (state.Broken)
                                {
                                    foreach (var objr in ent.MultitileGroup.Objects)
                                    {
                                        ((VMGameObject)objr).DisableParticle(256);
                                    }
                                    fixCount++;
                                }
                                state.QtrDaysSinceLastRepair = 0;
                                state.Wear = 0;
                            }
                        }
                        vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Generic, "Fixed " + fixCount + " objects."));
                        break;
                    case "testcollision":
                        vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Debug, $"Scanning collision for lot { vm.TSOState.Name }."));
                        try
                        {
                            var collisionValidator = new CollisionTestUtils();
                            collisionValidator.VerifyAllCollision(vm);
                            vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Debug, "No issue detected with collision."));
                        } catch (Exception e)
                        {
                            vm.SignalChatEvent(new VMChatEvent(null, VMChatEventType.Debug, e.Message));
                        }
                        break;
                }
                return true;
            }
            else
            {
                vm.SignalChatEvent(new VMChatEvent(avatar, VMChatEventType.Message, (byte)(ChannelID & 0x7f), avatar.Name, Message));
                if ((ChannelID & 0x80) == 0) avatar.Message = Message;
                UpdateTalkingHeadSeek(vm, avatar);
                return true;
            }
        }

        private void UpdateTalkingHeadSeek(VM vm, VMAvatar talker)
        {
            // Update head seek of everyone else to attempt to look at the person talking.

            var channel = vm.TSOState.ChatChannels.FirstOrDefault(x => x.ID == ChannelID);
            if (ChannelID == 7) channel = VMTSOChatChannel.AdminChannel;

            if (channel != null && channel.ViewPermMin != VMTSOAvatarPermissions.Visitor) return; // Cannot use look towards on private channels.

            bool isImportantChannel = channel != null && channel.SendPermMin > VMTSOAvatarPermissions.Visitor && channel.Flags.HasFlag(VMTSOChatChannelFlags.EnableTTS);
            int multiplier = isImportantChannel ? 2 : 1;
            int talkerRoom = vm.Context.GetObjectRoom(talker);

            foreach (VMAvatar avatar in vm.Context.ObjectQueries.Avatars)
            {
                if (avatar == talker) continue;

                if (!isImportantChannel)
                {
                    // Check if the avatar is in the same room, and rather close by.
                    int avatarRoom = vm.Context.GetObjectRoom(avatar);
                    if (avatarRoom != talkerRoom || LotTilePos.Distance(avatar.Position, talker.Position) > 16 * 10)
                    {
                        continue; // Not close enough.
                    }
                }

                var avatarHeadTarget = vm.GetObjectById(avatar.GetPersonData(VMPersonDataVariable.HeadSeekObject));
                var avatarHeadFinish = avatar.GetPersonData(VMPersonDataVariable.HeadSeekFinishAction);
                var avatarHeadState = avatar.GetPersonData(VMPersonDataVariable.HeadSeekState);
                if (avatarHeadState == 8 || avatarHeadState == 0 || (avatarHeadTarget is VMAvatar && (avatarHeadFinish != -1 || isImportantChannel)))
                {
                    // We can look towards the talker. (important talkers have priority)
                    avatar.SetPersonData(VMPersonDataVariable.HeadSeekObject, talker.ObjectID);
                    avatar.SetPersonData(VMPersonDataVariable.HeadSeekState, 1); //in progress flag only
                    avatar.SetPersonData(VMPersonDataVariable.HeadSeekLimitAction, 1); //look back on limit?
                    avatar.SetPersonData(VMPersonDataVariable.HeadSeekFinishAction, (short)(isImportantChannel ? -1 : 0)); //use to store if the person was an important talker
                    avatar.SetPersonData(VMPersonDataVariable.HeadSeekTimeout, (short)(talker.MessageTimeout * multiplier)); //while the message is present
                }
            }
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified || ChannelID == 0) return true; //normal
            else
            {
                //custom channel. look up the permissions - we might have to send this directly to the other sims
                var channel = vm.TSOState.ChatChannels.FirstOrDefault(x => x.ID == ChannelID);
                if (ChannelID == 7) channel = VMTSOChatChannel.AdminChannel;
                if (channel == null) return false;

                //can we send to this channel?
                if (caller.AvatarState.Permissions < channel.SendPermMin) return false;

                if (channel.ViewPermMin > VMTSOAvatarPermissions.Visitor)
                {
                    //need to direct send to eligible sims
                    ChannelID |= 0x80; //do not play in VM, only send the chat event
                    Verified = true;
                    foreach (var avatar in vm.Context.ObjectQueries.AvatarsByPersist) {
                        if (avatar.Value.AvatarState.Permissions >= channel.ViewPermMin)
                            vm.Driver.SendDirectCommand(avatar.Key, this);
                    }
                    return false; //direct sending will handle this
                }
                else return true; //send to everyone
            }
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            Message = Message.Substring(0, Math.Min(Message.Length, 200));
            base.SerializeInto(writer);
            writer.Write(Message);
            writer.Write(ChannelID);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Message = reader.ReadString();
            ChannelID = reader.ReadByte();
        }
        #endregion
    }
}
