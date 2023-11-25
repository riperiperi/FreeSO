using Mina.Filter.Codec;
using Ninject;
using Mina.Core.Session;

namespace FSO.Server.Protocol.Aries
{
    public class AriesProtocol : IProtocolCodecFactory
    {
        private IKernel Kernel;

        public AriesProtocol(IKernel kernel)
        {
            this.Kernel = kernel;
        }

        private IProtocolDecoder _Decoder;

        public IProtocolDecoder GetDecoder(IoSession session)
        {
            if (_Decoder == null)
            {
                _Decoder = Kernel.Get<AriesProtocolDecoder>();
            }
            return _Decoder;
        }

        private IProtocolEncoder _Encoder;

        public IProtocolEncoder GetEncoder(IoSession session)
        {
            if(_Encoder == null)
            {
                _Encoder = Kernel.Get<AriesProtocolEncoder>();
            }
            return _Encoder;
        }
    }
}
