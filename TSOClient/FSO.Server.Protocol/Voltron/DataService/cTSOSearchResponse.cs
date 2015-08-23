using FSO.Server.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.Dataservice;

namespace FSO.Server.Protocol.Voltron.DataService
{
    [clsid(0xDBF301A9)]
    public class cTSOSearchResponse : IoBufferSerializable
    {
        public string Query { get; set; }
        public cTSOSearchType Type { get; set; }
        public uint Unknown { get; set; }
        public List<cTSOSearchResponseItem> Items { get; set; }
        
        public IoBuffer Serialize(){
            var result = AbstractVoltronPacket.Allocate(10);
            result.AutoExpand = true;

            result.PutPascalVLCString(Query);
            result.PutUInt32((byte)Type);
            result.PutUInt32(Unknown);
            result.PutUInt32((uint)Items.Count);

            foreach(var item in Items){
                result.PutUInt32(item.Unknown);
                result.PutPascalVLCString(item.Name);
            }

            result.Put(new byte[36]);

            return result;
            //result.Free();
        }
    }

    public class cTSOSearchResponseItem
    {
        public uint Unknown { get; set; }
        public string Name { get; set; }
    }
}
