using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FSO.SimAntics.NetPlay.EODs.Handlers.Data
{
    public class VMEODSignsData : VMSerializable
    {
        public ushort Flags;
        public string Text = "";

        public VMEODSignsData() { }

        public VMEODSignsData(byte[] data)
        {
            using (var reader = new BinaryReader(new MemoryStream(data)))
            {
                Deserialize(reader);
            }
        }

        public byte[] Save()
        {
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                SerializeInto(writer);
                return stream.ToArray();
            }
            
        }

        public void Deserialize(BinaryReader reader)
        {
            Flags = reader.ReadUInt16();
            Text = reader.ReadString();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Flags);
            writer.Write(Text);
        }
    }
}
