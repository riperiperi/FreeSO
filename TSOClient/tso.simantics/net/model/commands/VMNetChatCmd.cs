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

namespace FSO.SimAntics.Netplay.Model.Commands
{
    public class VMNetChatCmd : VMNetCommandBodyAbstract
    {
        public short CallerID;
        public string Message;

        public override bool Execute(VM vm)
        {
            VMEntity caller = vm.GetObjectById(CallerID);
            //TODO: check if net user owns caller!
            if (caller == null || caller is VMGameObject) return false;
            ((VMAvatar)caller).Message = Message;
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(CallerID);
            writer.Write(Message);
        }

        public override void Deserialize(BinaryReader reader)
        {
            CallerID = reader.ReadInt16();
            Message = reader.ReadString();
        }
        #endregion
    }
}
