using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetChangeEnvironmentCmd : VMNetCommandBodyAbstract
    {
        public List<uint> GUIDsToAdd;
        public List<uint> GUIDsToClear;

        public override bool Execute(VM vm)
        {
            var amb = vm.Context.Ambience;
            foreach (var guid in GUIDsToClear)
                amb.SetAmbience(amb.GetAmbienceFromGUID(guid), false);
            foreach (var guid in GUIDsToAdd)
                amb.SetAmbience(amb.GetAmbienceFromGUID(guid), true);
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (caller == null || //caller must be on lot, be a build roommate.
            ((VMTSOAvatarState)caller.TSOState).Permissions < VMTSOAvatarPermissions.BuildBuyRoommate)
                return false;
            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(GUIDsToAdd.Count);
            foreach (var guid in GUIDsToAdd) writer.Write(guid);
            writer.Write(GUIDsToClear.Count);
            foreach (var guid in GUIDsToClear) writer.Write(guid);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            GUIDsToAdd = new List<uint>();
            var totalAdd = Math.Min(40, reader.ReadInt32());
            for (int i = 0; i < totalAdd; i++) GUIDsToAdd.Add(reader.ReadUInt32());

            GUIDsToClear = new List<uint>();
            var totalClear = Math.Min(40, reader.ReadInt32());
            for (int i = 0; i < totalClear; i++) GUIDsToClear.Add(reader.ReadUInt32());
        }

        #endregion
    }
}
