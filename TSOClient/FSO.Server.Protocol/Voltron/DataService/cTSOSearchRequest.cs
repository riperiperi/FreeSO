using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.DataService;
using FSO.Server.Protocol.Voltron.Model;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Dataservice
{
    [clsid(0x3626F62)]
    [cTSONetMessageParameter(DBRequestType.Search)]
    [cTSONetMessageParameter(DBRequestType.SearchExactMatch)]
    public class cTSOSearchRequest : IoBufferSerializable, IoBufferDeserializable
    {
        public string Query { get; set; }
        public cTSOSearchType Type { get; set; }
        
        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            this.Query = input.GetPascalVLCString();
            this.Type = (cTSOSearchType)input.GetUInt32();
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutPascalVLCString(this.Query);
            output.PutUInt32((uint)Type);
        }
    }

    public enum cTSOSearchType
    {
        SIMS = 0x01,
        LOTS = 0x02
    }
}
