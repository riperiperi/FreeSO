using Mina.Filter.Codec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Session;

namespace FSO.Client.Network.Sandbox
{
    public class FSOSandboxProtocol : IProtocolCodecFactory
    {
        private IProtocolDecoder _Decoder;

        public IProtocolDecoder GetDecoder(IoSession session)
        {
            if (_Decoder == null)
            {
                _Decoder = new FSOSandboxProtocolDecoder();
            }
            return _Decoder;
        }

        private IProtocolEncoder _Encoder;

        public IProtocolEncoder GetEncoder(IoSession session)
        {
            if (_Encoder == null)
            {
                _Encoder = new FSOSandboxProtocolEncoder();
            }
            return _Encoder;
        }
    }
}
