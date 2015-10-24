using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class FindPlayerResponsePDU : AbstractVoltronPacket
    {
        public uint StatusCode;
        public string ReasonText;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.FindPlayerResponsePDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            //var result = Allocate(8);
            //result.AutoExpand = true;

            output.PutUInt32(StatusCode);
            output.PutPascalString(ReasonText);


            //Room Info
            output.PutPascalString("");
            output.PutPascalString("");
            output.Put((byte)0);

            //Owner
            output.PutPascalString("");
            output.PutPascalString("");

            //Stage id
            output.PutPascalString("");
            output.PutPascalString("");

            //Currnet ocupancy
            output.PutUInt32(10);

            //Max occupancy
            output.PutUInt32(50);

            //pswd required
            output.Put((byte)0);

            //room type
            output.Put((byte)1);

            //Group
            output.PutPascalString("");

            //Admin list
            output.PutUInt16(0);

            //m_EnabledFlag
            output.Put(0);

            //m_AdmitList
            output.PutUInt16(0);

            //m_EnabledFlag
            output.Put(0);

            //m_DenyList
            output.PutUInt16(0);

            //m_EnabledFlag
            output.Put(0);

            output.PutUInt32(0);
            output.PutUInt32(0);
            output.PutUInt32(0);

            output.PutPascalString("");
            output.PutPascalString("");
            output.Put(0);
            output.Put(0);
            //return result;
        }
    }
}
