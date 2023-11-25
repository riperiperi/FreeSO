using FSO.Common.Serialization;
using FSO.Files.Formats.tsodata;
using Mina.Core.Buffer;
using System.Collections.Generic;
using System.IO;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class SendCityMail : AbstractGluonCallPacket
    {
        public List<MessageItem> Items { get; set; }

        public SendCityMail() { }

        public SendCityMail(List<MessageItem> items) {
            Items = items;
        }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            base.Deserialize(input, context);
            var itemCount = input.GetInt32();
            var dataSize = input.GetInt32();
            var data = input.GetSlice(dataSize).GetBytes();
            using (var mem = new MemoryStream(data)) {
                Items = new List<MessageItem>();
                for (int i = 0; i < itemCount; i++)
                {
                    var message = new MessageItem();
                    message.Read(mem);
                    Items.Add(message);
                }
            }
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            base.Serialize(output, context);
            byte[] data = null;
            using (var mem = new MemoryStream())
            {
                foreach (var item in Items)
                {
                    item.Save(mem);
                }
                data = mem.ToArray();
            }
            output.PutInt32(Items.Count);
            output.PutInt32(data.Length);
            output.Put(data);
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.CitySendMail;
        }
    }
}
