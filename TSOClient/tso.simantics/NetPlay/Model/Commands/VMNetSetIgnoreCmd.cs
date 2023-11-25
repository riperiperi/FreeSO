using System.IO;
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