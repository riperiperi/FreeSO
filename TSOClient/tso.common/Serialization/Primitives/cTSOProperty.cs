using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Serialization.Primitives
{
    public class cTSOProperty
    {
        public uint StructType;
        public List<cTSOField> StructFields;
        
        /*
        public IoBuffer Serialize()
        {
            var result = AbstractVoltronPacket.Allocate(12);
            result.AutoExpand = true;
            result.PutUInt32(0x89739A79);
            result.PutUInt32(StructType);
            result.PutUInt32((uint)StructFields.Count);
            foreach (var item in StructFields)
            {
                result.PutUInt32(item.ID);
                result.PutUInt32(item.Value.Type);
                result.PutSerializable(item.Value.Value);
            }
            return result;
        }*/
    }
}
