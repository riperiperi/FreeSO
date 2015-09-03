using FSO.Server.Common;
using FSO.Server.Protocol.Aries;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Filter.Codec;
using Mina.Filter.Logging;
using Mina.Filter.Ssl;
using Mina.Transport.Socket;
using Ninject;
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

        public void Connect(IPEndPoint target)
        {
            Connector = new AsyncSocketConnector();
            Connector.ConnectTimeoutInMillis = 10000;
            Connector.FilterChain.AddLast("logging", new LoggingFilter());

            Connector.Handler = this;
            //Connector.FilterChain.AddLast("ssl", new SslFilter(new X509Certificate()));
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
                //TODO: More efficient framing
                foreach(var packet in packets){
                    this.Session.Write(packet);
                }
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
            
        }

        public void MessageReceived(IoSession session, object message)
        {
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
