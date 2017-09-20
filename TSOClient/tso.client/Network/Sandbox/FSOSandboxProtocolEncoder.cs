using Mina.Filter.Codec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Session;
using Mina.Core.Buffer;
using FSO.SimAntics.NetPlay.Model;

namespace FSO.Client.Network.Sandbox
{
    public class FSOSandboxProtocolEncoder : IProtocolEncoder
    {
        public void Dispose(IoSession session)
        {
        }

        public void Encode(IoSession session, object message, IProtocolEncoderOutput output)
        {
            if (message is object[])
            {
                foreach (var m in (object[])message) Encode(session, m, output);
            }
            else if (message is VMNetMessage)
            {
                var nmsg = (VMNetMessage)message;

                var payload = IoBuffer.Allocate(128);
                payload.Order = ByteOrder.LittleEndian;
                payload.AutoExpand = true;

                payload.PutInt32(0); //packet type
                payload.PutInt32(nmsg.Data.Length + 1);
                payload.Put((byte)nmsg.Type);
                foreach (var b in nmsg.Data) payload.Put(b);
                payload.Flip();

                output.Write(payload);
            }
        }
    }
}
