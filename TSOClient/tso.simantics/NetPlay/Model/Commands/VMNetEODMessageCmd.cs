using System;
using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetEODMessageCmd : VMNetCommandBodyAbstract
    {
        public uint PluginID;
        public string EventName;
        public bool Binary;
        public string TextData;
        public byte[] BinData;

        public bool Verified = false;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            //only run by client. send to their UI handler.
            //TODO: DO NOT BROADCAST VIA SYNC COMMAND INTERFACE TO ALL. ONLY TO TARGET!
            if (caller != null && caller.PersistID == vm.MyUID)
            {
                vm.SignalEODMessage(this);
            }

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified == true) return true; //temporary... forward messages through tick broadcast
            //only run by server. Forward message to this avatar's connected EOD.
            if (caller == null) return false;

            try
            {
                vm.EODHost.Deliver(this, caller);
            }
            catch (Exception)
            {
                // some future NLog statement
            }

            return false;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(PluginID);
            writer.Write(EventName);
            writer.Write(Binary);
            if (Binary)
            {
                writer.Write((ushort)BinData.Length);
                writer.Write(BinData);
            } else
            {
                writer.Write(TextData);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            PluginID = reader.ReadUInt32();
            EventName = reader.ReadString();
            Binary = reader.ReadBoolean();
            if (Binary)
            {
                var length = reader.ReadUInt16();
                BinData = reader.ReadBytes(length);
            } else
            {
                TextData = reader.ReadString();
            }
        }
        #endregion
    }
}
