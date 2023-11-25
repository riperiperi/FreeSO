using Mina.Core.Buffer;
using FSO.Common.Serialization.TypeSerializers;

namespace FSO.Common.Serialization.Primitives
{
    [cTSOValue(0x9736027)]
    public class cTSOTopicUpdateMessage : IoBufferSerializable, IoBufferDeserializable
    {
        public uint MessageId { get; set; } = 0xA97360C5;

        public uint StructType { get; set; }
        public uint StructId { get; set; }
        public uint StructField { get; set; }

        public uint[] DotPath { get; set; }

        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        
        public object Value { get; set; }
        
        public string ReasonText { get; set; }


        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(Unknown1); //Update counter
            output.PutUInt32(MessageId); //Message id
            output.PutUInt32(Unknown2); //Unknown

            if (DotPath != null)
            {
                output.PutUInt32((uint)DotPath.Length);
                foreach(var component in DotPath){
                    output.PutUInt32(component);
                }
            }
            else
            {
                //Vector size
                output.PutUInt32(3);
                output.PutUInt32(StructType);
                output.PutUInt32(StructId);
                output.PutUInt32(StructField);
            }

            //Write value
            context.ModelSerializer.Serialize(output, Value, context, true);

            output.PutPascalVLCString(ReasonText);
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
            this.Value = context.ModelSerializer.Deserialize(valueType, input, context);
            //this.ReasonText = input.GetPascalVLCString();
        }
    }
}
