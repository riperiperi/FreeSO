using FSO.Server.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using System.ComponentModel;

namespace FSO.Server.Protocol.Voltron.DataService
{
    [clsid(0x9736027)]
    public class cTSOTopicUpdateMessage : IoBufferSerializable, IoBufferDeserializable
    {
        public uint StructType { get; set; }
        public uint StructId { get; set; }
        public uint StructField { get; set; }

        public uint[] DotPath { get; set; }

        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }

        [TypeConverter(typeof(ExpandableObjectConverter))]
        public cTSOValue cTSOValue { get; set; }
        public string ReasonText { get; set; }

        public uint MessageId { get; set; } = 0xA97360C5;


        public IoBuffer Serialize()
        {
            var buffer = AbstractVoltronPacket.Allocate(16);
            buffer.AutoExpand = true;

            buffer.PutUInt32(Unknown1); //Update counter
            buffer.PutUInt32(MessageId); //Message id
            buffer.PutUInt32(Unknown2); //Unknown

            //Vector size
            buffer.PutUInt32(3);
            buffer.PutUInt32(StructType);
            buffer.PutUInt32(StructId);
            buffer.PutUInt32(StructField);

            buffer.PutUInt32(cTSOValue.Type);
            buffer.PutSerializable(cTSOValue.Value);

            buffer.PutPascalVLCString(ReasonText);
            return buffer;
        }

        public void Deserialize(IoBuffer input)
        {
            Unknown1 = input.GetUInt32();
            MessageId = input.GetUInt32();
            Unknown2 = input.GetUInt32();

            var vectorSize = input.GetUInt32();
            DotPath = new uint[vectorSize];
            for(int i=0; i < vectorSize; i++){
                DotPath[i] = input.GetUInt32();
            }

            var valueType = input.GetUInt32();
            this.cTSOValue = new cTSOValue {
                Type = valueType,
                Value = cTSOSerializer.Get().Deserialize(valueType, input)
            };
            this.ReasonText = input.GetPascalVLCString();
        }
    }
}
