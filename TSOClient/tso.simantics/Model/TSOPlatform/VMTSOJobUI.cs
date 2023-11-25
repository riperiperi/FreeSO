using FSO.SimAntics.NetPlay.Model;
using System.Collections.Generic;
using System.IO;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMTSOJobUI : VMSerializable
    {
        public List<string> MessageText = new List<string>();
        public int Minutes;
        public int Seconds;
        public VMTSOJobMode Mode;

        public void Deserialize(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            for (int i=0; i<count; i++)
            {
                MessageText.Add(reader.ReadString());
            }
            Minutes = reader.ReadByte();
            Seconds = reader.ReadByte();
            Mode = (VMTSOJobMode)reader.ReadByte();

        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(MessageText.Count);
            foreach (var msg in MessageText)
            {
                writer.Write(msg);
            }
            writer.Write((byte)Minutes);
            writer.Write((byte)Seconds);
            writer.Write((byte)Mode);
        }
    }

    public enum VMTSOJobMode
    {
        BeforeWork = 0,
        AfterWork = 1,
        Intermission = 2,
        Round = 3
    }
}
