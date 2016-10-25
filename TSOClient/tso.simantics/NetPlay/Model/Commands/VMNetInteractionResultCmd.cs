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

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetInteractionResultCmd : VMNetCommandBodyAbstract
    {
        public ushort ActionUID;
        public bool Accepted;
        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (caller == null) return false;
            var interaction = caller.Thread.Queue.FirstOrDefault(x => x.UID == ActionUID);
            if (interaction != null)
            {
                interaction.InteractionResult = (sbyte)(Accepted ? 2 : 1);
            }
            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ActionUID);
            writer.Write(Accepted);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ActionUID = reader.ReadUInt16();
            Accepted = reader.ReadBoolean();
        }

        #endregion
    }
}
