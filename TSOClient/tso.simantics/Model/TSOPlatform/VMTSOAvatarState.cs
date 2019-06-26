using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.SimAntics.Model.Platform;
using Microsoft.Xna.Framework;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMTSOAvatarState : VMTSOEntityState, VMIAvatarState
    {
        public VMTSOAvatarPermissions Permissions { get; set; } = VMTSOAvatarPermissions.Visitor;
        public HashSet<uint> IgnoredAvatars = new HashSet<uint>();
        public Dictionary<short, VMTSOJobInfo> JobInfo = new Dictionary<short, VMTSOJobInfo>();
        public VMTSOAvatarFlags Flags;
        public Color ChatColor = Color.White;
        public sbyte ChatTTSPitch; //-100 to 100. 
        public byte ChatChannel = 255; //the chat channel this avatar is viewing. 255 = all.

        public static Color[] RandomColours = new Color[] {
            new Color(255, 255, 255),
            new Color(125, 255, 255),
            new Color(255, 125, 255),
            new Color(255, 255, 125),
            new Color(125, 125, 255),
            new Color(255, 125, 125),
            new Color(125, 255, 125),
            new Color(0, 255, 255),
            new Color(255, 255, 0)
        };

        public VMTSOAvatarState() { }

        public VMTSOAvatarState(int version) : base(version) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Permissions = (VMTSOAvatarPermissions)reader.ReadByte();
            var ignored = reader.ReadInt32();
            IgnoredAvatars.Clear();
            for (int i = 0; i < ignored; i++)
            {
                IgnoredAvatars.Add(reader.ReadUInt32());
            }
            JobInfo.Clear();
            if (Version > 7)
            {
                var jobs = reader.ReadInt32();
                for (int i = 0; i < jobs; i++)
                {
                    var id = reader.ReadInt16();
                    var job = new VMTSOJobInfo();
                    job.Deserialize(reader);
                    JobInfo[id] = job;
                }
            }
            if (Version > 9)
                Flags = (VMTSOAvatarFlags)reader.ReadUInt32();

            if (Version > 27)
            {
                ChatColor = new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), (byte)255);
                ChatTTSPitch = reader.ReadSByte();
                ChatChannel = reader.ReadByte();
            }
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write((byte)Permissions);
            writer.Write(IgnoredAvatars.Count);
            foreach (var id in IgnoredAvatars)
                writer.Write(id);
            writer.Write(JobInfo.Count);
            foreach (var item in JobInfo)
            {
                writer.Write(item.Key);
                item.Value.SerializeInto(writer);
            }
            writer.Write((uint)Flags);

            writer.Write(ChatColor.R);
            writer.Write(ChatColor.G);
            writer.Write(ChatColor.B);
            writer.Write(ChatTTSPitch);
            writer.Write(ChatChannel);
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

    [Flags]
    public enum VMTSOAvatarFlags : uint
    {
        CanBeRoommate = 1, //TODO: update on becoming roomie of another lot, while on this lot.
        NewPlayer = 2, //under a week old
        Mayor = 4, //is mayor of this neighborhood
    }
}
