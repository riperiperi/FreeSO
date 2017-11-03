using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FSO.Vitaboy;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Model
{
    public class VMOutfitReference : VMSerializable
    {
        public string Name = "";
        public ulong ID = 0;
        public Outfit OftData;

        public VMOutfitReference(string name)
        {
            ID = uint.MaxValue;
            Name = name;
        }

        public VMOutfitReference(ulong id)
        {
            ID = id;
        }

        public VMOutfitReference(STR str, bool head)
        {
            OftData = new Outfit();
            if (head)
                OftData.ReadHead(str);
            else 
                OftData.Read(str);
        }

        public static VMOutfitReference Parse(string data, bool ts1)
        {
            ts1 = false;
            if (ts1)
            {
                return new VMOutfitReference(data.Trim());
            } else
            {
                return new VMOutfitReference(Convert.ToUInt64(data.Trim(), 16));
            }
        }

        public Outfit GetContent()
        {
            if (OftData != null) return OftData;
            var content = Content.Content.Get().AvatarOutfits;
            if (ID == uint.MaxValue)
                return content.Get(Name);
            else
                return content.Get(ID);
        }

        public VMOutfitReference(BinaryReader reader)
        {
            Deserialize(reader);
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ID);
            if (ID == uint.MaxValue) writer.Write(Name);
        }

        public void Deserialize(BinaryReader reader)
        {
            ID = reader.ReadUInt64();
            if (ID == uint.MaxValue) Name = reader.ReadString();
        }
    }
}
