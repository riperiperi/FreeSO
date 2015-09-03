using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Utils;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class SetIgnoreListPDU : AbstractVoltronPacket
    {
        public List<uint> PlayerIds;

        public override void Deserialize(IoBuffer input)
        {
            var length = input.GetUInt16();
            PlayerIds = new List<uint>();

            for(var i=0; i < length; i++)
            {
                PlayerIds.Add(input.GetUInt32());
            }
        }

        public override IoBuffer Serialize()
        {
            var len = 0;
            if(PlayerIds != null)
            {
                len = PlayerIds.Count;
            }

            var result = Allocate(2 + (len * 4));
            result.PutUInt16((ushort)len);

            for(int i=0; i < len; i++)
            {
                result.PutUInt32(PlayerIds[i]);
            }
            return result;
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.SetIgnoreListPDU;
        }
    }
}
