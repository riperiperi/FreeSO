using Mina.Core.Service;
using System;
using System.Threading.Tasks;
using Mina.Core.Session;
using System.Net;
using Mina.Transport.Socket;
using Mina.Core.Future;
using Mina.Filter.Codec;
using FSO.SimAntics.NetPlay.Model;
using FSO.Common.Utils;
using FSO.Server.Common;

namespace FSO.Client.Network.Sandbox
{
    public class FSOSandboxClient : IoHandler
    {
        private IoConnector Connector;
        private IoSession Session;

        public event Action<VMNetMessage> OnMessage;
        public event Action OnConnectComplete;

        public void Connect(string address)
        {
            // Happy-eyeballs: prefer v4, fall back to v6 if the v4 path fails.
            // Identical strategy to AriesClient.Connect; see IPEndPointUtils.ResolveAll.
            var endpoints = IPEndPointUtils.ResolveAll(address);
            if (endpoints.Length == 0) return;

            int perAttemptMs = endpoints.Length > 1 ? 5000 : 10000;

            Task.Run(() =>
            {
                foreach (var ep in endpoints)
                {
                    if (TryConnectOnce(ep, perAttemptMs)) return;
                }
                // All addresses exhausted. Sandbox client has no SessionClosed
                // event to fire — the caller will see IsConnected == false.
            });
        }

        // Single attempt; returns true on success. Same shape as
        // AriesClient.TryConnectOnce so happy-eyeballs iteration above can run
        // synchronously inside the Task.Run.
        private bool TryConnectOnce(IPEndPoint target, int timeoutMs)
        {
            var socketConnector = new AsyncSocketConnector();
            socketConnector.SessionConfig.NoDelay = true;
            Connector = socketConnector;
            Connector.ConnectTimeoutInMillis = timeoutMs;

            Connector.Handler = this;
            Connector.FilterChain.AddLast("protocol", new ProtocolCodecFilter(new FSOSandboxProtocol()));
            var future = Connector.Connect(target, new Action<IoSession, IConnectFuture>(OnConnect));

            if (!future.Await(timeoutMs)) return false;
            if (future.Canceled || future.Exception != null) return false;
            return Session != null;
        }

        public void Disconnect()
        {
            if (Session != null)
            {
                Session.Close(false);
            }
        }

        public void Connect(IPEndPoint target)
        {
            var socketConnector = new AsyncSocketConnector();
            socketConnector.SessionConfig.NoDelay = true;
            Connector = socketConnector;
            Connector.ConnectTimeoutInMillis = 10000;

            Connector.Handler = this;
            Connector.FilterChain.AddLast("protocol", new ProtocolCodecFilter(new FSOSandboxProtocol()));
            Connector.Connect(target, new Action<IoSession, IConnectFuture>(OnConnect));
        }

        private void OnConnect(IoSession session, IConnectFuture future)
        {
            this.Session = session;
            GameThread.NextUpdate(x =>
            {
                OnConnectComplete();
            });
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

        public void ExceptionCaught(IoSession session, Exception cause)
        {
        }

        public void InputClosed(IoSession session)
        {
        }

        public void MessageReceived(IoSession session, object message)
        {
            if (message is VMNetMessage)
            {
                var nmsg = (VMNetMessage)message;
                OnMessage(nmsg);
            }
        }

        public void MessageSent(IoSession session, object message)
        {
        }

        public void SessionClosed(IoSession session)
        {
        }

        public void SessionCreated(IoSession session)
        {
        }

        public void SessionIdle(IoSession session, IdleStatus status)
        {
        }

        public void SessionOpened(IoSession session)
        {
        }
    }
}
