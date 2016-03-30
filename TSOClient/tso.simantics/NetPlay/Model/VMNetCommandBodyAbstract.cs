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

namespace FSO.SimAntics.NetPlay.Model
{
    public abstract class VMNetCommandBodyAbstract : VMSerializable
    {
        public uint ActorUID;
        public bool FromNet = false;

        public abstract bool Execute(VM vm);

        public virtual void Deserialize(BinaryReader reader) {
            FromNet = true;
            ActorUID = reader.ReadUInt32();
        }
        public virtual void SerializeInto(BinaryWriter writer) {
            writer.Write(ActorUID);
        }

        //verifies commands sent by clients before running and forwarding them.
        //if "Verify" returns true, the server runs the command and it is sent to clients
        //this prevents forwarding bogus requests - though some verifications are performed as the command is sequenced.
        //certain commands like "StateSyncCommand" cannot be forwarded from clients.
        public virtual bool Verify(VM vm)
        {
            return true;
        }
    }
}
