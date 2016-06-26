using FSO.Server.Common;
using FSO.Server.Protocol.Aries;
using FSO.Server.Protocol.Utils;
using FSO.Server.Protocol.Voltron.Packets;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Filter.Codec;
using Mina.Filter.Logging;
using Mina.Filter.Ssl;
using Mina.Transport.Socket;
using Ninject;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Clients
{
    public interface IAriesMessageSubscriber
    {
        void MessageReceived(AriesClient client, object message);
    }
    
    public interface IAriesEventSubscriber
    {
        void SessionCreated(AriesClient client);
        void SessionOpened(AriesClient client);
        void SessionClosed(AriesClient client);
        void SessionIdle(AriesClient client);
        void InputClosed(AriesClient session);
    }



    public class AriesClient : IoHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private IoConnector Connector;
        private IoSession Session;
        private IKernel Kernel;

        private List<IAriesMessageSubscriber> MessageSubscribers = new List<IAriesMessageSubscriber>();
        private List<IAriesEventSubscriber> EventSubscribers = new List<IAriesEventSubscriber>();
        
        public AriesClient(IKernel kernel)
        {
            this.Kernel = kernel;
        }

        public void AddSubscriber(object sub)
        {
            lock (EventSubscribers)
            {
                if (sub is IAriesEventSubscriber)
                {
                    EventSubscribers.Add((IAriesEventSubscriber)sub);
                }
                if (sub is IAriesMessageSubscriber)
                {
                    MessageSubscribers.Add((IAriesMessageSubscriber)sub);
                }
            }
        }

        public void RemoveSubscriber(object sub)
        {
            lock (EventSubscribers)
            {
                if (sub is IAriesEventSubscriber)
                {
                    EventSubscribers.Remove((IAriesEventSubscriber)sub);
                }
                if (sub is IAriesMessageSubscriber)
                {
                    MessageSubscribers.Remove((IAriesMessageSubscriber)sub);
                }
            }
        }

        public void Connect(string address){
            Connect(IPEndPointUtils.CreateIPEndPoint(address));
        }

        public void Disconnect(){
            Session.Close(false);
        }

        public void Connect(IPEndPoint target)
        {
            Connector = new AsyncSocketConnector();
            Connector.ConnectTimeoutInMillis = 10000;
            //Connector.FilterChain.AddLast("logging", new LoggingFilter());
            
            Connector.Handler = this;
            //var ssl = new CustomSslFilter((X509Certificate)null);
            //ssl.SslProtocol = System.Security.Authentication.SslProtocols.Tls;
            //Connector.FilterChain.AddFirst("ssl", ssl);

            Connector.FilterChain.AddLast("protocol", new ProtocolCodecFilter(new AriesProtocol(Kernel)));
            Connector.Connect(target, new Action<IoSession, IConnectFuture>(OnConnect));
        }

        private void OnConnect(IoSession session, IConnectFuture future)
        {
            this.Session = session;
        }

        public void Write(params object[] packets)
        {
            if (this.Session != null)
            {
                this.Session.Write(packets);
            }
        }

        public bool IsConnected
        {
            get
            {
                return Session != null && Session.Connected;
            }
        }

        public void SessionCreated(IoSession session)
        {
            lock (EventSubscribers)
            {
                EventSubscribers.ForEach(x => x.SessionCreated(this));
            }
        }

        public void SessionOpened(IoSession session)
        {
            lock (EventSubscribers)
            {
                EventSubscribers.ForEach(x => x.SessionOpened(this));
            }
        }

        public void SessionClosed(IoSession session)
        {
            lock (EventSubscribers)
            {
                EventSubscribers.ForEach(x => x.SessionClosed(this));
            }
        }

        public void SessionIdle(IoSession session, IdleStatus status)
        {
            lock (EventSubscribers)
            {
                EventSubscribers.ForEach(x => x.SessionIdle(this));
            }
        }

        public void ExceptionCaught(IoSession session, Exception cause)
        {
            if (cause is System.Net.Sockets.SocketException) session.Close(true);
            else LOG.Error(cause);
        }

        public void MessageReceived(IoSession session, object message)
        {
            if (message is ServerByePDU) session.Close(false);
            lock (EventSubscribers)
            {
                MessageSubscribers.ForEach(x => x.MessageReceived(this, message));
            }
        }

        public void MessageSent(IoSession session, object message)
        {
        }

        public void InputClosed(IoSession session)
        {
            lock (EventSubscribers)
            {
                EventSubscribers.ForEach(x => x.InputClosed(this));
            }
        }
    }
}
