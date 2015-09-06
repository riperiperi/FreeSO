using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.Dataservice;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.DataService
{
    [clsid(0xDBF301A9)]
    public class cTSOSearchResponse : IoBufferSerializable
    {
        public string Query { get; set; }
        public cTSOSearchType Type { get; set; }
        public uint Unknown { get; set; }
        public List<cTSOSearchResponseItem> Items { get; set; }
        
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

    public class cTSOSearchResponseItem
    {
        public uint EntityId { get; set; }
        public string Name { get; set; }
    }
}
