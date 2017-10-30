﻿using FSO.Server.Database.DA.Shards;
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
using FSO.Common.Utils;
using FSO.Server.Protocol.Gluon.Model;
using FSO.Server.Database.DA.Hosts;
using Mina.Core.Write;

namespace FSO.Server.Framework.Aries
{
    public abstract class AbstractAriesServer : AbstractServer, IoHandler, ISocketServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        protected IKernel Kernel;

        private AbstractAriesServerConfig Config;
        protected IDAFactory DAFactory;

        private IoAcceptor Acceptor;
        private IoAcceptor PlainAcceptor;
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

            if(config.Public_Host == null || config.Internal_Host == null ||
                config.Call_Sign == null || config.Binding == null)
            {
                throw new Exception("Server configuration missing required fields");
            }

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
        
        protected virtual DbHost CreateHost()
        {
            return new Database.DA.Hosts.DbHost
            {
                call_sign = Config.Call_Sign,
                status = Database.DA.Hosts.DbHostStatus.up,
                time_boot = DateTime.UtcNow,
                internal_host = Config.Public_Host,
                public_host = Config.Internal_Host
            };
        }

        public override void Start()
        {
            Bootstrap();

            using (var db = DAFactory.Get())
            {
                db.Hosts.CreateHost(CreateHost());
            }

            Acceptor = new AsyncSocketAcceptor();

            try {
                if (Config.Certificate != null)
                {
                    var ssl = new SslFilter(new System.Security.Cryptography.X509Certificates.X509Certificate2(Config.Certificate));
                    ssl.SslProtocol = SslProtocols.Tls;
                    Acceptor.FilterChain.AddLast("ssl", ssl);
                    if (Debugger != null)
                    {
                        Acceptor.FilterChain.AddLast("packetLogger", new AriesProtocolLogger(Debugger.GetPacketLogger(), Kernel.Get<ISerializationContext>()));
                        Debugger.AddSocketServer(this);
                    }
                    Acceptor.FilterChain.AddLast("protocol", new ProtocolCodecFilter(Kernel.Get<AriesProtocol>()));
                    Acceptor.Handler = this;

                    Acceptor.Bind(IPEndPointUtils.CreateIPEndPoint(Config.Binding));
                    LOG.Info("Listening on " + Acceptor.LocalEndPoint + " with TLS");
                }

                //Bind in the plain too as a workaround until we can get Mina.NET to work nice for TLS in the AriesClient
                PlainAcceptor = new AsyncSocketAcceptor();
                if (Debugger != null){
                    PlainAcceptor.FilterChain.AddLast("packetLogger", new AriesProtocolLogger(Debugger.GetPacketLogger(), Kernel.Get<ISerializationContext>()));
                }

                PlainAcceptor.FilterChain.AddLast("protocol", new ProtocolCodecFilter(Kernel.Get<AriesProtocol>()));
                PlainAcceptor.Handler = this;
                PlainAcceptor.Bind(IPEndPointUtils.CreateIPEndPoint(Config.Binding.Replace("100", "101")));
                LOG.Info("Listening on " + PlainAcceptor.LocalEndPoint + " in the plain");
            }
            catch(Exception ex)
            {
                LOG.Error("Unknown error bootstrapping server: "+ex.ToString(), ex);
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
            LOG.Info("[SESSION-CREATE (" + Config.Call_Sign +")]");

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

            ariesSession.LastRecv = Epoch.Now;
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
            LOG.Info("[SESSION-CLOSED (" + Config.Call_Sign + ")]");

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
            //todo: handle individual error codes
            if (cause is System.Net.Sockets.SocketException)
            {
                session.Close(true);
            }
            else if (cause is WriteToClosedSessionException || cause is WriteTimeoutException)
            {
                //don't do anything... mina should be able to deal with this
            }
            else if (cause is System.InvalidOperationException)
            {
                LOG.Error(cause, "CRITICAL (mina bug): " + cause.ToString());
                session.Close(true);
            }
            else
            {
                LOG.Error(cause, "Unknown error: " + cause.ToString());
            }
        }

        public void MessageSent(IoSession session, object message)
        {
        }

        public void InputClosed(IoSession session)
        {
        }

        public override void Shutdown()
        {
            Acceptor.Dispose();
            PlainAcceptor.Dispose();

            var sessionClone = _Sessions.Clone();
            foreach (var session in sessionClone)
                session.Close();

            MarkHostDown();
        }

        public void MarkHostDown()
        {
            using (var db = DAFactory.Get())
            {
                try {
                    db.Hosts.SetStatus(Config.Call_Sign, DbHostStatus.down);
                }catch(Exception ex){
                }
            }
        }

        public abstract Type[] GetHandlers();

        public List<ISocketSession> GetSocketSessions()
        {
            var result = new List<ISocketSession>();
            var sessions = _Sessions.RawSessions;
            lock (sessions)
            {
                foreach (var item in sessions)
                {
                    result.Add(item);
                }
            }
            return result;
        }
    }
}
