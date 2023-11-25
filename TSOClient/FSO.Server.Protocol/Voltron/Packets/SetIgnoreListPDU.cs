using System.Collections.Generic;
using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class SetIgnoreListPDU : AbstractVoltronPacket
    {
        public List<uint> PlayerIds;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            var length = input.GetUInt16();
            PlayerIds = new List<uint>();

            for(var i=0; i < length; i++)
            {
                PlayerIds.Add(input.GetUInt32());
            }
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            var len = 0;
            if(PlayerIds != null)
            {
                len = PlayerIds.Count;
            }

            output.PutUInt16((ushort)len);

            for(int i=0; i < len; i++)
            {
                output.PutUInt32(PlayerIds[i]);
            }
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.SetIgnoreListPDU;
        }
    }
}
