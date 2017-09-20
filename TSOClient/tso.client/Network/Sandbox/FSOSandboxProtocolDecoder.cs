using Mina.Filter.Codec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using Mina.Core.Session;
using FSO.Common.Serialization;
using FSO.Server.Protocol.Utils;
using FSO.SimAntics.NetPlay.Model;

namespace FSO.Client.Network.Sandbox
{
    public class FSOSandboxProtocolDecoder : CustomCumulativeProtocolDecoder
    {
        protected override bool DoDecode(IoSession session, IoBuffer buffer, IProtocolDecoderOutput output)
        {
            if (buffer.Remaining < 8)
            {
                return false;
            }

            /**
             * We expect aries, voltron or electron packets
             */
            var startPosition = buffer.Position;

            buffer.Order = ByteOrder.LittleEndian;
            uint packetType = buffer.GetUInt32(); //currently unused
            uint payloadSize = buffer.GetUInt32();

            if (buffer.Remaining < payloadSize)
            {
                /** Not all here yet **/
                buffer.Position = startPosition;
                return false;
            }

            var type = (VMNetMessageType)buffer.Get();
            var data = new List<byte>();
            for (int i=0; i<payloadSize-1; i++)
            {
                data.Add(buffer.Get());
            }
            var packet = new VMNetMessage(type, data.ToArray());
            output.Write(packet);

            return true;
        }
    }
}
