using Mina.Filter.Codec;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public IProtocolDecoder GetDecoder(IoSession session)
        {
            return Kernel.Get<AriesProtocolDecoder>();
        }

        public IProtocolEncoder GetEncoder(IoSession session)
        {
            return Kernel.Get<AriesProtocolEncoder>();
        }
    }
}
