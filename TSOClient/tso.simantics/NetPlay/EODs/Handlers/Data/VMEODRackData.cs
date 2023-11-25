using FSO.SimAntics.NetPlay.Model;
using System.IO;

namespace FSO.SimAntics.NetPlay.EODs.Handlers.Data
{
    public class VMEODRackData : VMSerializable
    {
        public string RackName = "Name Your Rack";

        public VMEODRackData() { }

        public VMEODRackData(byte[] data)
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
            RackName = reader.ReadString();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(RackName);
        }
    }
}
