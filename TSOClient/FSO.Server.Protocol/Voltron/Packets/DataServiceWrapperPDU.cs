using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Utils;
using FSO.Server.Protocol.Voltron.DataService;
using System.ComponentModel;

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

        public override IoBuffer Serialize()
        {
            var result = Allocate(4);
            result.AutoExpand = true;

            result.PutUInt32(SendingAvatarID);
            result.PutUInt32(RequestTypeID);

            if (Body is cTSONetMessageStandard)
            {
                cTSONetMessageStandard bodyObj = (cTSONetMessageStandard)Body;
                IoBuffer bodyData = bodyObj.Serialize();
                bodyData.Flip();

                result.PutUInt32((uint)bodyData.Remaining);
                result.PutUInt32(0x9736027);//0x125194E5
                result.Put(bodyData);
            }
            else if(Body is cTSOTopicUpdateMessage)
            {
                cTSOTopicUpdateMessage bodyObj = (cTSOTopicUpdateMessage)Body;
                IoBuffer bodyData = bodyObj.Serialize();
                bodyData.Flip();

                result.PutUInt32((uint)bodyData.Remaining + 4);
                result.PutUInt32(0x9736027);//0x125194E5
                result.Put(bodyData);
            }

            //result.PutUInt32(RequestTypeID);
            //result.PutUInt32(0);
            return result;
        }

        public override void Deserialize(IoBuffer input)
        {
            this.SendingAvatarID = input.GetUInt32();

            this.RequestTypeID = input.GetUInt32();
            this.BodySize = input.GetUInt32();

            var bodyBytes = new byte[this.BodySize];
            input.Get(bodyBytes, 0, (int)this.BodySize);
            this.Body = bodyBytes;
            var bodyBuffer = IoBuffer.Wrap(bodyBytes);
            this.BodyType = bodyBuffer.GetUInt32();

            if (this.BodyType == 0x125194E5)
            {
                var entity = new cTSONetMessageStandard();
                entity.Deserialize(bodyBuffer);
                this.Body = entity;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Unknown body type! " + this.BodyType.ToString("X"));
            }
        }
        
    }

    
}
