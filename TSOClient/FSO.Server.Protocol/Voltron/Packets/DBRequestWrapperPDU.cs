using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.Model;
using FSO.Server.Protocol.Utils;
using FSO.Server.Protocol.Voltron.DataService;
using System.ComponentModel;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class DBRequestWrapperPDU : AbstractVoltronPacket
    {
        public uint SendingAvatarID { get; set; }
        public Sender Sender { get; set; }
        public byte Badge { get; set; }
        public byte IsAlertable { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Body { get; set; }

        public override void Deserialize(IoBuffer input)
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

            this.Body = cTSOSerializer.Get().Deserialize(bodyType, bodyBuffer);
        }

        public override IoBuffer Serialize(){
            var result = Allocate(0);
            result.AutoExpand = true;

            result.PutUInt32(SendingAvatarID);
            PutSender(result, Sender);
            result.Put(Badge);
            result.Put(IsAlertable);

            if (Body != null)
            {
                var value = cTSOSerializer.Get().GetValue(Body);
                var valueBytes = IoBufferUtils.SerializableToIoBuffer(value.Value);

                result.PutUInt32((uint)valueBytes.Remaining + 4);
                result.PutUInt32(value.Type);
                result.Put(valueBytes);
            }
            return result;
        }

        public override VoltronPacketType GetPacketType(){
            return VoltronPacketType.DBRequestWrapperPDU;
        }
    }
}
