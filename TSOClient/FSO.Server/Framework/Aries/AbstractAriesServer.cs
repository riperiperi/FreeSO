using FSO.Server.Database.DA.Shards;
using FSO.Server.DataService.Shards;
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

namespace FSO.Server.Framework.Aries
{
    public abstract class AbstractAriesServer : AbstractServer, IoHandler, ISocketServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IKernel Kernel;

        private CityServerConfiguration Config;
        private Shard Shard;
        private IDAFactory DAFactory;

        private IoAcceptor Acceptor;
        private IServerDebugger Debugger;

        private AriesPacketRouter _Router = new AriesPacketRouter();
        private Sessions _Sessions = new Sessions();

        private List<IAriesSessionInterceptor> _SessionInterceptors = new List<IAriesSessionInterceptor>();

        public AbstractAriesServer(CityServerConfiguration config, IKernel kernel)
        {
            this.Kernel = kernel;
            this.DAFactory = Kernel.Get<IDAFactory>();
            this.Config = config;
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
            LOG.Info("Starting city server for city: " + Config.ID);
            Bootstrap();
            
            Acceptor = new AsyncSocketAcceptor();

            try {
                var ssl = new SslFilter(new System.Security.Cryptography.X509Certificates.X509Certificate2(Config.Certificate));
                ssl.SslProtocol = SslProtocols.Tls;
                //Acceptor.FilterChain.AddLast("ssl", ssl);
                if(Debugger != null)
                {
                    Acceptor.FilterChain.AddLast("packetLogger", new AriesProtocolLogger(Debugger.GetPacketLogger(), Kernel.Get<ISerializationContext>()));
                    Debugger.AddSocketServer(this);
                }
                Acceptor.FilterChain.AddLast("protocol", new ProtocolCodecFilter(Kernel.Get<AriesProtocol>()));
                Acceptor.Handler = this;

                Acceptor.Bind(IPEndPointUtils.CreateIPEndPoint(Config.Binding));
                LOG.Info("Listening on " + Acceptor.LocalEndPoint);
            }catch(Exception ex)
            {
                LOG.Error("Unknown error bootstrapping server", ex);
            }
        }

        public ISessions Sessions
        {
            get { return _Sessions; }
        }

        protected void Bootstrap()
        {
            var shardsData = Kernel.Get<ShardsDataService>();
            this.Shard = shardsData.GetById(Config.ID);
            if(this.Shard == null)
            {
                throw new Exception("Unable to find a shard with id " + Config.ID + ", check it exists in the database");
            }

            LOG.Info("City identified as " + Shard.name);

            //Bindings
            Kernel.Bind<IAriesPacketRouter>().ToConstant(_Router);
            Kernel.Bind<Shard>().ToConstant(this.Shard);
            Kernel.Bind<ISessions>().ToConstant(this._Sessions);

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
        private void HandleVoltronSessionResponse(IAriesSession session, object message)
        {
            var rawSession = (AriesSession)session;
            
            var packet = message as RequestClientSessionResponse;

            if(message != null)
            {
                using (var da = DAFactory.Get())
                {
                    var ticket = da.Shards.GetTicket(packet.Password);
                    if(ticket != null)
                    {
                        //TODO: Check if its expired
                        da.Shards.DeleteTicket(packet.Password);

                        //Time to upgrade to a voltron session
                        var newSession = rawSession.UpgradeSession<VoltronSession>();
                        newSession.UserId = ticket.user_id;
                        newSession.AvatarId = ticket.avatar_id;
                        newSession.IsAuthenticated = true;

                        _Sessions.Remove(rawSession);
                        _Sessions.Add(newSession);

                        foreach (var interceptor in _SessionInterceptors)
                        {
                            try
                            {
                                interceptor.SessionUpgraded(rawSession, newSession);
                            }
                            catch (Exception ex)
                            {
                                LOG.Error(ex);
                            }
                        }
                        return;
                    }
                }
            }

            //Failed authentication
            rawSession.Close();
        }


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

            _Router.Handle(ariesSession, message);
        }

        public void SessionOpened(IoSession session)
        {
        }

        public void SessionClosed(IoSession session)
        {
            LOG.Info("[SESSION-CLOSED]");

            var ariesSession = session.GetAttribute<IAriesSession>("s");

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
