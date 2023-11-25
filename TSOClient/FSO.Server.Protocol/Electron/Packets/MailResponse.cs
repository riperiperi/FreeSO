using FSO.Common.Serialization;
using Mina.Core.Buffer;
using FSO.Files.Formats.tsodata;
using System.IO;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class MailResponse : AbstractElectronPacket
    {
        public MailResponseType Type;
        public MessageItem[] Messages = new MessageItem[0];

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Type = input.GetEnum<MailResponseType>();
            var numMessages = input.GetInt32();
            Messages = new MessageItem[numMessages];
            for (int j = 0; j < numMessages; j++)
            {
                var length = input.GetInt32();
                var dat = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    dat[i] = input.Get();
                }

                using (var str = new MemoryStream(dat))
                {
                    Messages[j] = new MessageItem(str);
                }
            }
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.MailResponse;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Type);
            output.PutInt32(Messages.Length);
            foreach (var msg in Messages)
            {
                byte[] dat;
                using (var str = new MemoryStream())
                {
                    msg.Save(str);
                    dat = str.ToArray();
                }
                output.PutInt32(dat.Length);
                foreach (var b in dat)
                    output.Put(b);
            }
        }
    }

    public enum MailResponseType
    {
        POLL_RESPONSE,
        NEW_MAIL,
        SEND_IGNORING_YOU,
        SEND_IGNORING_THEM,
        SEND_THROTTLED,
        SEND_THROTTLED_DAILY,
        SEND_FAILED,
        SEND_SUCCESS //returns the message you sent, with its db id
    }
}
