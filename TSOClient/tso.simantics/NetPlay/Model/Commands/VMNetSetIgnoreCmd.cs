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
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetSetIgnoreCmd : VMNetCommandBodyAbstract
    {
        public uint TargetPID;
        public bool SetIgnore;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (caller == null) return false;
            var ignored = ((VMTSOAvatarState)caller.TSOState).IgnoredAvatars;
            if (SetIgnore && ignored.Count < 128) ignored.Add(TargetPID);
            else ignored.Remove(TargetPID);
            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(TargetPID);
            writer.Write(SetIgnore);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            TargetPID = reader.ReadUInt32();
            SetIgnore = reader.ReadBoolean();
        }

        #endregion
    }
}