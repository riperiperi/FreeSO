using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.Model;
using System.ComponentModel;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class DBRequestWrapperPDU : AbstractVoltronPacket
    {
        public uint SendingAvatarID { get; set; }
        public Sender Sender { get; set; }
        public byte Badge { get; set; }
        public byte IsAlertable { get; set; }

        public object BodyType { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Body { get; set; }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            SendingAvatarID = input.GetUInt32();
            Sender = GetSender(input);
            Badge = input.Get();
            IsAlertable = input.Get();

            var bodySize = input.GetUInt32();
            var bodyType = input.GetUInt32();

            var bodyBytes = new byte[bodySize-4];
            input.Get(bodyBytes, 0, (int)bodySize-4);
            var bodyBuffer = IoBuffer.Wrap(bodyBytes);

            this.Body = context.ModelSerializer.Deserialize(bodyType, bodyBuffer, context);
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(SendingAvatarID);
            PutSender(output, Sender);
            output.Put(Badge);
            output.Put(IsAlertable);

            if (Body != null)
            {
                var bodyBytes = context.ModelSerializer.SerializeBuffer(Body, context, true);
                output.PutUInt32((uint)bodyBytes.Remaining);
                output.Put(bodyBytes);
            }
        }

        public override VoltronPacketType GetPacketType(){
            return VoltronPacketType.DBRequestWrapperPDU;
        }
    }
}
