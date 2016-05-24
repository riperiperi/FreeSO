using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMTSOAvatarState : VMTSOEntityState
    {
        public VMTSOAvatarPermissions Permissions = VMTSOAvatarPermissions.BuildBuyRoommate;
        public HashSet<uint> IgnoredAvatars = new HashSet<uint>();

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Permissions = (VMTSOAvatarPermissions)reader.ReadByte();
            var ignored = reader.ReadInt32();
            IgnoredAvatars.Clear();
            for (int i=0; i<ignored; i++)
            {
                IgnoredAvatars.Add(reader.ReadUInt32());
            }
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write((byte)Permissions);
            writer.Write(IgnoredAvatars.Count);
            foreach (var id in IgnoredAvatars)
                writer.Write(id);
        }

        public override void Tick(VM vm, object owner)
        {
            base.Tick(vm, owner);
        }
    }

    public enum VMTSOAvatarPermissions : byte
    {
        Visitor = 0,
        Roommate = 1,
        BuildBuyRoommate = 2,
        Owner = 3,
        Admin = 4
    }
}
