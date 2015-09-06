using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.DataService;
using System.ComponentModel;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class DataServiceWrapperPDU : AbstractVoltronPacket
    {
        public uint SendingAvatarID { get; set; }
        public uint RequestTypeID { get; set; }
        public uint BodyType { get; set; }
        public uint BodyEntityID { get; set; }
        public byte[] BodyEntityParameter { get; set; }
        
        public uint BodySize { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public object Body { get; set; }


        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.DataServiceWrapperPDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            //var result = Allocate(4);
            //result.AutoExpand = true;

            output.PutUInt32(SendingAvatarID);
            output.PutUInt32(RequestTypeID);

            /*if (Body is cTSONetMessageStandard)
            {
                cTSONetMessageStandard bodyObj = (cTSONetMessageStandard)Body;
                IoBuffer bodyData = bodyObj.Serialize(context);
                bodyData.Flip();

                output.PutUInt32((uint)bodyData.Remaining);
                output.PutUInt32(0x9736027);//0x125194E5
                output.Put(bodyData);
            }
            else if(Body is cTSOTopicUpdateMessage)
            {
                cTSOTopicUpdateMessage bodyObj = (cTSOTopicUpdateMessage)Body;
                IoBuffer bodyData = bodyObj.Serialize(context);
                bodyData.Flip();

                output.PutUInt32((uint)bodyData.Remaining + 4);
                output.PutUInt32(0x9736027);//0x125194E5
                output.Put(bodyData);
            }*/

            //output.PutUInt32(RequestTypeID);
            //output.PutUInt32(0);
        }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            this.SendingAvatarID = input.GetUInt32();
            this.RequestTypeID = input.GetUInt32();
            this.BodySize = input.GetUInt32();

            var bodyBytes = new byte[this.BodySize];
            input.Get(bodyBytes, 0, (int)this.BodySize);
            this.Body = bodyBytes;
            var bodyBuffer = IoBuffer.Wrap(bodyBytes);
            this.BodyType = bodyBuffer.GetUInt32();

            this.Body = cTSOSerializer.Get().Deserialize(BodyType, bodyBuffer);
        }
        
    }

    
}
