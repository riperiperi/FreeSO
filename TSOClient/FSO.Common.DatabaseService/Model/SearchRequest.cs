using Mina.Core.Buffer;
using FSO.Common.Serialization;
using FSO.Common.DatabaseService.Framework;

namespace FSO.Common.DatabaseService.Model
{
    [DatabaseRequest(DBRequestType.Search)]
    [DatabaseRequest(DBRequestType.SearchExactMatch)]
    public class SearchRequest : IoBufferSerializable, IoBufferDeserializable
    {
        public string Query { get; set; }
        public SearchType Type { get; set; }
        
        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            this.Query = input.GetPascalVLCString();
            this.Type = (SearchType)input.GetUInt32();
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutPascalVLCString(this.Query);
            output.PutUInt32((uint)Type);
        }
    }

    public enum SearchType
    {
        SIMS = 0x01,
        LOTS = 0x02,
        NHOOD = 0x03
    }
}
