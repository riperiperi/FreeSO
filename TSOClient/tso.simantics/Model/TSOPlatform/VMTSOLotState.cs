using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.IO;
using FSO.SimAntics.Model.Platform;
using Microsoft.Xna.Framework;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMTSOLotState : VMAbstractLotState
    {
        //ephemeral state
        public IVMAvatarNameCache Names = new VMBasicAvatarNameCache();

        //permanent state
        public string Name = "Lot";
        public uint LotID;
        public VMTSOSurroundingTerrain Terrain = new VMTSOSurroundingTerrain();
        public byte PropertyCategory;
        public int Size = 0; // (size | (floors << 8) | (dir << 16)

        public uint OwnerID;
        public HashSet<uint> Roommates = new HashSet<uint>();
        public HashSet<uint> BuildRoommates = new HashSet<uint>();
        public int ObjectLimit;
        public override bool LimitExceeded { get; set; }

        public VMTSOJobUI JobUI;

        public byte SkillMode;
        public List<VMTSOChatChannel> ChatChannels = new List<VMTSOChatChannel>();
        public uint NhoodID;

        public bool CommunityLot
        {
            //some notes for state when in a community lot
            //roommates is now donators. build roommates is now benefactors.
            //serve a similar purpose, but are NOT flashed from database.
            //they are cleared in the saved state when the DB Owner ID differs from the ID here.
            get { return PropertyCategory == 11; }
        }

        public int DonateLimit = 2000;

        public VMTSOLotState() { }
        public VMTSOLotState(int version) : base(version) { }

        public override void Deserialize(BinaryReader reader)
        {
            Name = reader.ReadString();
            LotID = reader.ReadUInt32();
            if (Version > 6) {
                Terrain = new VMTSOSurroundingTerrain();
                Terrain.Deserialize(reader);
            } else {
                reader.ReadByte(); //old Terrain Type
            }
            PropertyCategory = reader.ReadByte();
            Size = reader.ReadInt32();

            OwnerID = reader.ReadUInt32();
            Roommates = new HashSet<uint>();
            var roomCount = reader.ReadInt16();
            for (int i = 0; i < roomCount; i++) Roommates.Add(reader.ReadUInt32());
            BuildRoommates = new HashSet<uint>();
            var broomCount = reader.ReadInt16();
            for (int i = 0; i < broomCount; i++) BuildRoommates.Add(reader.ReadUInt32());

            if (Version > 10)
            {
                if (reader.ReadBoolean())
                {
                    JobUI = new VMTSOJobUI();
                    JobUI.Deserialize(reader);
                }
            }

            if (Version > 24)
            {
                SkillMode = reader.ReadByte();
            }

            if (Version > 27)
            {
                ChatChannels.Clear();
                var channelCount = reader.ReadByte(); //number of chat channels - currently unused

                for (int i=0; i<channelCount; i++)
                {
                    var chan = new VMTSOChatChannel();
                    chan.Deserialize(reader);
                    ChatChannels.Add(chan);
                }
            }
            if (Version > 32)
            {
                NhoodID = reader.ReadUInt32();
            }
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(LotID);
            Terrain.SerializeInto(writer);
            writer.Write(PropertyCategory);
            writer.Write(Size);

            writer.Write(OwnerID);
            writer.Write((short)Roommates.Count);
            foreach (var roomie in Roommates) writer.Write(roomie);
            writer.Write((short)BuildRoommates.Count);
            foreach (var roomie in BuildRoommates) writer.Write(roomie);

            writer.Write(JobUI != null);
            if (JobUI != null) JobUI.SerializeInto(writer);
            writer.Write(SkillMode);

            writer.Write((byte)ChatChannels.Count);
            foreach (var channel in ChatChannels)
            {
                channel.SerializeInto(writer);
            }
            writer.Write(NhoodID);
        }

        public override bool CanPlaceNewUserObject(VM vm)
        {
            return (vm.Context.ObjectQueries.NumUserObjects < ObjectLimit);
        }

        public override bool CanPlaceNewDonatedObject(VM vm)
        {
            return (vm.Context.ObjectQueries.NumDonatedObjects < DonateLimit);
        }

        public override void Tick(VM vm, object owner)
        {
            
        }

        public override void ActivateValidator(VM vm)
        {
            switch (PropertyCategory)
            {
                case 11:
                    Validator = new VMFSOCommunityValidator(vm); break;
                default:
                    Validator = new VMDefaultValidator(vm); break;
            }
            
        }
    }

    public class VMTSOChatChannel : VMSerializable
    {
        public byte ID;
        public string Name = "Custom";
        public string Description = "A Custom Chat Channel";
        public VMTSOAvatarPermissions ViewPermMin;
        public VMTSOAvatarPermissions SendPermMin;
        public VMTSOChatChannelFlags Flags;
        public Color TextColor = new Color(255, 249, 157);

        public string _TextColorString;
        public string TextColorString
        {
            get
            {
                if (_TextColorString == null) _TextColorString = "[color=#" + TextColor.R.ToString("x2") + TextColor.G.ToString("x2") + TextColor.B.ToString("x2") + "]";
                return _TextColorString;
            }
        }

        public static VMTSOChatChannel AdminChannel = new VMTSOChatChannel()
        {
            ID = 7,
            ViewPermMin = VMTSOAvatarPermissions.Admin,
            SendPermMin = VMTSOAvatarPermissions.Admin,
            Name = "Admin",
            Description = "Administrators Only",
            TextColor = Color.White
        };

        public static VMTSOChatChannel MainChannel = new VMTSOChatChannel()
        {
            ID = 0,
            ViewPermMin = VMTSOAvatarPermissions.Visitor,
            SendPermMin = VMTSOAvatarPermissions.Visitor,
            Name = "Chat",
            Description = "Default Channel",
            TextColor = Color.White,
            Flags = VMTSOChatChannelFlags.ShowByDefault
        };

        public VMTSOChatChannel Clone()
        {
            return new VMTSOChatChannel()
            {
                ID = ID,
                Description = Description,
                Flags = Flags,
                Name = Name,
                SendPermMin = SendPermMin,
                TextColor = TextColor,
                ViewPermMin = ViewPermMin
            };
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ID);
            writer.Write(Name);
            writer.Write(Description);
            writer.Write((byte)ViewPermMin);
            writer.Write((byte)SendPermMin);
            writer.Write((byte)Flags);
            writer.Write(TextColor.PackedValue);
        }

        public void Deserialize(BinaryReader reader)
        {
            ID = reader.ReadByte();
            Name = reader.ReadString();
            Description = reader.ReadString();
            ViewPermMin = (VMTSOAvatarPermissions)reader.ReadByte();
            SendPermMin = (VMTSOAvatarPermissions)reader.ReadByte();
            Flags = (VMTSOChatChannelFlags)reader.ReadByte();
            TextColor = new Color(reader.ReadUInt32());
        }
    }

    [Flags]
    public enum VMTSOChatChannelFlags : byte
    {
        EnableTTS = 1,
        ShowByDefault = 2,
        Delete = 128,

        All = 3
    }
}
