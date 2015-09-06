using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Common.Serialization;
using FSO.Common.Serialization.TypeSerializers;
using FSO.Common.DatabaseService.Framework;

namespace FSO.Common.DatabaseService.Model
{
    [DatabaseResponse(DBResponseType.SearchExactMatch)]
    public class SearchResponse : IoBufferSerializable
    {
        public string Query { get; set; }
        public SearchType Type { get; set; }
        public uint Unknown { get; set; }
        public List<SearchResponseItem> Items { get; set; }
        
        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutPascalVLCString(Query);
            output.PutUInt32((byte)Type);
            output.PutUInt32(Unknown);
            output.PutUInt32((uint)Items.Count);

            foreach(var item in Items){
                output.PutUInt32(item.EntityId);
                output.PutPascalVLCString(item.Name);
            }

            output.Skip(36);
        }
    }

    public class SearchResponseItem
    {
        public uint EntityId { get; set; }
        public string Name { get; set; }
    }
}
