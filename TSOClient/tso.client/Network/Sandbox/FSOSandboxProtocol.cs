using Mina.Filter.Codec;
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
