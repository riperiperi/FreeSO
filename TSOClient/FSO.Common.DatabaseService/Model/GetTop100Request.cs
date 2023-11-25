using FSO.Common.Enum;
using FSO.Common.Serialization;
using Mina.Core.Buffer;
using FSO.Common.DatabaseService.Framework;

namespace FSO.Common.DatabaseService.Model
{
    [DatabaseRequest(DBRequestType.GetTopResultSetByID)]
    public class GetTop100Request : IoBufferSerializable, IoBufferDeserializable
    {
        public Top100Category Category;

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Category = input.GetEnum<Top100Category>();
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Category);
        }
    }
}
