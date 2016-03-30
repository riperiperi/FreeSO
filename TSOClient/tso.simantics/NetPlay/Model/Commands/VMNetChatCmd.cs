/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.SimAntics.NetPlay.Drivers;
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

        public override bool Execute(VM vm)
        {
            VMEntity caller = vm.Entities.FirstOrDefault(x => x.PersistID == ActorUID);
            //TODO: check if net user owns caller!
            if (caller == null || caller is VMGameObject) return false;

            var avatar = (VMAvatar)caller;
            if (Message[0] == '/')
            {
                var spaceIndex = Message.IndexOf(' ');
                if (spaceIndex == -1) spaceIndex = Message.Length;
                if (FromNet || !(vm.Driver is VMServerDriver)) return false;
                //commands are only run from the server sim right now
                var cmd = Message.Substring(1, spaceIndex - 1);
                var args = Message.Substring(Math.Min(Message.Length, spaceIndex + 1), Math.Max(0, Message.Length - (spaceIndex + 1)));
                var server = (VMServerDriver)vm.Driver;
                switch (cmd.ToLower())
                {
                    case "ban":
                        server.BanUser(vm, args);
                        break;
                    case "banip":
                        server.BanIP(args);
                        vm.SignalChatEvent(new VMChatEvent(0, VMChatEventType.Generic, "Added " + args + " to the IP ban list."));
                        break;
                    case "unban":
                        server.SandboxBans.Remove(args.ToLower().Trim(' '));
                        vm.SignalChatEvent(new VMChatEvent(0, VMChatEventType.Generic, "Removed " + args + " from the IP ban list."));
                        break;
                    case "banlist":
                        string result = "";
                        foreach (var ban in server.SandboxBans.List()) result += ban + "\r\n";
                        vm.SignalChatEvent(new VMChatEvent(0, VMChatEventType.Generic, "==== BANNED IPS: ==== \r\n"+result));
                        break;
                }
                return true;
            }
            else
            {
                vm.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Message, avatar.Name, Message));
                avatar.Message = Message;
                return true;
            }
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Message);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Message = reader.ReadString();
        }
        #endregion
    }
}
