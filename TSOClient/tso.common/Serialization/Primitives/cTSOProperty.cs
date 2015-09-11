using FSO.Common.Serialization.TypeSerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mina.Core.Buffer;
using System.Collections;

namespace FSO.Common.Serialization.Primitives
{
    public class cTSOProperty : IoBufferSerializable
    {
        public uint StructType;
        public List<cTSOPropertyField> StructFields;

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(0x89739A79);
            output.PutUInt32(StructType);
            output.PutUInt32((uint)StructFields.Count);

            foreach (var item in StructFields)
            {
                output.PutUInt32(item.StructFieldID);
                context.ModelSerializer.Serialize(output, item.Value, context, true);
            }
        }
    }

    public class cTSOPropertyField
    {
        public uint StructFieldID;
        public object Value;
    }
}
