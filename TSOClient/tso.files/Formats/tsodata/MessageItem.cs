using FSO.Files.Utils;
using System.IO;

namespace FSO.Files.Formats.tsodata
{
    public class MessageItem
    {
        public static int CURRENT_VERSION = 1;
        public int Version = CURRENT_VERSION;
        public int ID;
        public uint SenderID;
        public uint TargetID;
        public string Subject;
        public string Body;
        public string SenderName;
        public long Time;
        public int Type; //(message/vote/club/maxis/tso/house/roommate/call)
        public int Subtype; //(urgent?)
        public int ReadState;
        public int? ReplyID;

        public MessageItem()
        {

        }

        public MessageItem(Stream stream)
        {
            Read(stream);
        }

        public void Save(Stream stream) {
            using (var writer = IoWriter.FromStream(stream))
            {
                writer.WriteCString("FSOI", 4);
                writer.WriteInt32(Version);
                writer.WriteInt32(ID);
                writer.WriteUInt32(SenderID);
                writer.WriteUInt32(TargetID);
                writer.WriteLongPascalString(Subject);
                writer.WriteLongPascalString(Body);
                writer.WriteLongPascalString(SenderName);
                writer.WriteInt64(Time);
                writer.WriteInt32(Type);
                writer.WriteInt32(Subtype);
                writer.WriteInt32(ReadState);
                writer.WriteByte((byte)((ReplyID == null) ? 0 : 1));
                if (ReplyID != null) writer.WriteInt32(ReplyID.Value);
            }
        }

        public void Read(Stream stream)
        {
            using (var reader = IoBuffer.FromStream(stream))
            {
                var magic = reader.ReadCString(4);
                Version = reader.ReadInt32();
                ID = reader.ReadInt32();
                SenderID = reader.ReadUInt32();
                TargetID = reader.ReadUInt32();
                Subject = reader.ReadLongPascalString();
                Body = reader.ReadLongPascalString();
                SenderName = reader.ReadLongPascalString();
                Time = reader.ReadInt64();
                Type = reader.ReadInt32();
                Subtype = reader.ReadInt32();
                ReadState = reader.ReadInt32();
                if (reader.ReadByte() > 0)
                {
                    ReplyID = reader.ReadInt32();
                }
            }
        }
    }

    public enum MessageSpecialType
    {
        Normal = 0,

        //neighbourhoods
        Nominate = 1,
        Vote = 2,

        AcceptNomination = 3,
        FreeVote = 4
    }
}
