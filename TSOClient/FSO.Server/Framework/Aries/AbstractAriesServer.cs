using FSO.Server.Database.DA.Shards;
using FSO.Server.Protocol.Aries;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Server.Servers;
using FSO.Server.Servers.City;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Filter.Codec;
using Mina.Filter.Ssl;
using Mina.Transport.Socket;
using Ninject;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using FSO.Server.Common;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Database.DA;
using FSO.Server.Protocol.Voltron;
using FSO.Server.Framework.Voltron;
using FSO.Common.Serialization;
using FSO.Common.Domain.Shards;
using FSO.Server.Protocol.CitySelector;

namespace FSO.Server.Framework.Aries
{
    public abstract class AbstractAriesServer : AbstractServer, IoHandler, ISocketServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        protected IKernel Kernel;

        private AbstractAriesServerConfig Config;
        protected IDAFactory DAFactory;

        private IoAcceptor Acceptor;
        private IServerDebugger Debugger;

        private AriesPacketRouter _Router = new AriesPacketRouter();
        private Sessions _Sessions;

        private List<IAriesSessionInterceptor> _SessionInterceptors = new List<IAriesSessionInterceptor>();

        public AbstractAriesServer(AbstractAriesServerConfig config, IKernel kernel)
        {
            _Sessions = new Sessions(this);
            this.Kernel = kernel;
            this.DAFactory = Kernel.Get<IDAFactory>();
            this.Config = config;


            Kernel.Bind<IAriesPacketRouter>().ToConstant(_Router);
            Kernel.Bind<ISessions>().ToConstant(this._Sessions);
        }

        public IAriesPacketRouter Router
        {
            get
            {
                return _Router;
            }
        }

        public override void AttachDebugger(IServerDebugger debugger)
        {
            this.Debugger = debugger;
        }

        public override void Start()
        {
            Bootstrap();
            
            Acceptor = new AsyncSocketAcceptor();

            try {
                var ssl = new SslFilter(new System.Security.Cryptography.X509Certificates.X509Certificate2(Config.Certificate));
                ssl.SslProtocol = SslProtocols.Tls;
                Acceptor.FilterChain.AddLast("ssl", ssl);
                if(Debugger != null)
                {
                    Acceptor.FilterChain.AddLast("packetLogger", new AriesProtocolLogger(Debugger.GetPacketLogger(), Kernel.Get<ISerializationContext>()));
                    Debugger.AddSocketServer(this);
                }
                Acceptor.FilterChain.AddLast("protocol", new ProtocolCodecFilter(Kernel.Get<AriesProtocol>()));
                Acceptor.Handler = this;

                Acceptor.Bind(IPEndPointUtils.CreateIPEndPoint(Config.Binding));
                LOG.Info("Listening on " + Acceptor.LocalEndPoint + " with TLS");

                //Bind in the plain too as a workaround until we can get Mina.NET to work nice for TLS in the AriesClient
                var plainAcceptor = new AsyncSocketAcceptor();
                if (Debugger != null){
                    plainAcceptor.FilterChain.AddLast("packetLogger", new AriesProtocolLogger(Debugger.GetPacketLogger(), Kernel.Get<ISerializationContext>()));
                }

                plainAcceptor.FilterChain.AddLast("protocol", new ProtocolCodecFilter(Kernel.Get<AriesProtocol>()));
                plainAcceptor.Handler = this;
                plainAcceptor.Bind(IPEndPointUtils.CreateIPEndPoint(Config.Binding.Replace("100", "101")));
                LOG.Info("Listening on " + plainAcceptor.LocalEndPoint + " in the plain");
            }
            catch(Exception ex)
            {
                LOG.Error("Unknown error bootstrapping server", ex);
            }
        }

        public ISessions Sessions
        {
            get { return _Sessions; }
        }

        public List<IAriesSessionInterceptor> SessionInterceptors
        {
            get
            {
                return _SessionInterceptors;
            }
        }

        protected virtual void Bootstrap()
        {
            Router.On<RequestClientSessionResponse>(HandleVoltronSessionResponse);

            if(this is IAriesSessionInterceptor){
                _SessionInterceptors.Add((IAriesSessionInterceptor)this);
            }

            var handlers = GetHandlers();
            foreach(var handler in handlers)
            {
                var handlerInstance = Kernel.Get(handler);
                _Router.AddHandlers(handlerInstance);
                if(handlerInstance is IAriesSessionInterceptor){
                    _SessionInterceptors.Add((IAriesSessionInterceptor)handlerInstance);
                }
            }
        }
        

        public void SessionCreated(IoSession session)
        {
            LOG.Info("[SESSION-CREATE]");

            //Setup session
            var ariesSession = new AriesSession(session);
            session.SetAttribute("s", ariesSession);
            _Sessions.Add(ariesSession);

            foreach(var interceptor in _SessionInterceptors){
                try{
                    interceptor.SessionCreated(ariesSession);
                }catch(Exception ex){
                    LOG.Error(ex);
                }
            }

            //Ask for session info
            session.Write(new RequestClientSession());
        }

        /// <summary>
        /// Voltron clients respond with RequestClientSessionResponse in response to RequestClientSession
        /// This handler validates that response, performs auth and upgrades the session to a voltron session.
        /// 
        /// This will allow this session to make voltron requests from this point onwards
        /// </summary>
        protected abstract void HandleVoltronSessionResponse(IAriesSession session, object message);


        public void MessageReceived(IoSession session, object message)
        {
            var ariesSession = session.GetAttribute<IAriesSession>("s");

            if (!ariesSession.IsAuthenticated)
            {
                /** You can only use aries packets when anon **/
                if(!(message is IAriesPacket))
                {
                    throw new Exception("Voltron packets are forbidden before aries authentication has completed");
                }
            }

            RouteMessage(ariesSession, message);
        }

        protected virtual void RouteMessage(IAriesSession session, object message)
        {
            _Router.Handle(session, message);
        }

        public void SessionOpened(IoSession session)
        {
        }

        public void SessionClosed(IoSession session)
        {
            LOG.Info("[SESSION-CLOSED]");

            var ariesSession = session.GetAttribute<IAriesSession>("s");
            _Sessions.Remove(ariesSession);

            foreach (var interceptor in _SessionInterceptors)
            {
                try{
                    interceptor.SessionClosed(ariesSession);
                }
                catch (Exception ex)
                {
                    LOG.Error(ex);
                }
            }
        }

        public void SessionIdle(IoSession session, IdleStatus status)
        {
        }

        public void ExceptionCaught(IoSession session, Exception cause)
        {
            LOG.Error("Unknown error", cause);
        }

        public void MessageSent(IoSession session, object message)
        {
        }

        public void InputClosed(IoSession session)
        {
        }


        public override void Shutdown()
        {

        }


        


        public abstract Type[] GetHandlers();

        public List<ISocketSession> GetSocketSessions()
        {
            var result = new List<ISocketSession>();
            foreach(var item in _Sessions.RawSessions)
            {
                result.Add(item);
            }
            return result;
        }
    }
}
