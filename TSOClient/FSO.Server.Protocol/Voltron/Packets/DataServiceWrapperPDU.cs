using Mina.Core.Buffer;
using System.ComponentModel;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class DataServiceWrapperPDU : AbstractVoltronPacket
    {
        public uint SendingAvatarID { get; set; }
        public uint RequestTypeID { get; set; }
        
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Body { get; set; }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.DataServiceWrapperPDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(SendingAvatarID);
            output.PutUInt32(RequestTypeID);

            if(Body != null){
                var bodyBytes = context.ModelSerializer.SerializeBuffer(Body, context, true);
                output.PutUInt32((uint)bodyBytes.Remaining);
                output.Put(bodyBytes);
            }

            //output.PutUInt32(RequestTypeID);
            //output.PutUInt32(0);
        }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            this.SendingAvatarID = input.GetUInt32();
            this.RequestTypeID = input.GetUInt32();

            var bodySize = input.GetUInt32();
            var bodyBytes = new byte[bodySize];
            input.Get(bodyBytes, 0, (int)bodySize);
            this.Body = bodyBytes;
            var bodyBuffer = IoBuffer.Wrap(bodyBytes);
            var bodyType = bodyBuffer.GetUInt32();

            this.Body = context.ModelSerializer.Deserialize(bodyType, bodyBuffer, context);
        }
        
    }

    
}
