using FSO.Common.Serialization;
using FSO.Files.Formats.tsodata;
using Mina.Core.Buffer;
using System.IO;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class MailRequest : AbstractElectronPacket
    {
        public MailRequestType Type;
        public long TimestampID;
        public MessageItem Item;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Type = input.GetEnum<MailRequestType>();
            if (Type == MailRequestType.SEND) { 
                var length = input.GetInt32();
                var dat = new byte[length];
                for (int i=0; i<length; i++)
                {
                    dat[i] = input.Get();
                }

                using (var str = new MemoryStream(dat))
                {
                    Item = new MessageItem(str);

                }
            } else
            {
                TimestampID = input.GetInt64();
            }
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.MailRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Type);
            if (Type == MailRequestType.SEND)
            {
                byte[] dat;
                if (Item == null) dat = new byte[0];
                else
                {
                    using (var str = new MemoryStream())
                    {
                        Item.Save(str);
                        dat = str.ToArray();
                    }
                }
                output.PutInt32(dat.Length);
                foreach (var b in dat)
                    output.Put(b);
            } else
                output.PutInt64(TimestampID);
        }
    }

    public enum MailRequestType : byte
    {
        POLL_INBOX,
        SEND,
        DELETE
    }
}
