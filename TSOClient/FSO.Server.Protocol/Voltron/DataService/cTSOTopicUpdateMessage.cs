using FSO.Server.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Voltron.DataService
{
    public class cTSOTopicUpdateMessage : IoBufferSerializable
    {
        public uint StructType;
        public uint StructId;
        public uint StructField;
        
        public cTSOValue cTSOValue;

        public string ReasonText;

        public IoBuffer Serialize()
        {
            var buffer = AbstractVoltronPacket.Allocate(16);
            buffer.AutoExpand = true;

            buffer.PutUInt32(0); //Update counter
            buffer.PutUInt32(0xA97360C5); //Message id
            buffer.PutUInt32(0); //Unknown

            //Vector size
            buffer.PutUInt32(3);
            buffer.PutUInt32(StructType);
            buffer.PutUInt32(StructId);
            buffer.PutUInt32(StructField);

            buffer.PutUInt32(cTSOValue.Type);
            buffer.PutSerializable(cTSOValue.Value);

            buffer.PutPascalVLCString(ReasonText);
            return buffer;
        }
    }
}
