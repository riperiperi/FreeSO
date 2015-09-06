using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using System.ComponentModel;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.DataService
{
    [clsid(0x9736027)]
    public class cTSOTopicUpdateMessage : IoBufferSerializable, IoBufferDeserializable
    {
        public uint MessageId { get; set; } = 0xA97360C5;

        public uint StructType { get; set; }
        public uint StructId { get; set; }
        public uint StructField { get; set; }

        public uint[] DotPath { get; set; }

        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        
        public SerializedValue Value { get; set; }
        public string ReasonText { get; set; }



        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(Unknown1); //Update counter
            output.PutUInt32(MessageId); //Message id
            output.PutUInt32(Unknown2); //Unknown

            //Vector size
            output.PutUInt32(3);
            output.PutUInt32(StructType);
            output.PutUInt32(StructId);
            output.PutUInt32(StructField);

            output.PutUInt32(Value.ClsId);
            output.PutSerializable(Value.Data, context);

            output.PutPascalVLCString(ReasonText);

            //buffer.PutUInt32(cTSOValue.Type);
            //buffer.PutSerializable(cTSOValue.Value);
            //buffer.PutPascalVLCString(ReasonText);
        }

        public void Deserialize(IoBuffer input, ISerializationContext context)
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
            //this.cTSOValue = new cTSOValue {
            //    Type = valueType,
            //    Value = cTSOSerializer.Get().Deserialize(valueType, input)
            //};
            //this.ReasonText = input.GetPascalVLCString();
        }
    }
}
