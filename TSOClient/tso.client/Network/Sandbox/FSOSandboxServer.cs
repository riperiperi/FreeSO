using Mina.Core.Service;
using Mina.Filter.Codec;
using Mina.Transport.Socket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Session;
using FSO.SimAntics.NetPlay.Model;
using System.IO;
using FSO.Common.Utils;

namespace FSO.Client.Network.Sandbox
{
    public class FSOSandboxServer : IoHandler
    {
        //todo: persist id provider
        //right now just give each new connected user a higher persist id than the last
        public uint PersistID = 2;

        public event Action<VMNetClient, VMNetMessage> OnMessage;
        public event Action<VMNetClient> OnConnect;
        public event Action<VMNetClient> OnDisconnect;

        private List<IoSession> Sessions = new List<IoSession>();

        public void ForceDisconnect(VMNetClient cli)
        {
            if (cli.NetHandle == null) return;
            ((IoSession)cli.NetHandle).Close(false);
        }

        public void ExceptionCaught(IoSession session, Exception cause)
        {
            session.Close(true);
        }

        public void SendMessage(VMNetClient cli, VMNetMessage msg)
        {
            if (cli.NetHandle == null) return;
            ((IoSession)cli.NetHandle).Write(msg);
        }

        public void Broadcast(VMNetMessage msg, HashSet<VMNetClient> ignore)
        {
            List<IoSession> cliClone;
            lock (Sessions) cliClone = new List<IoSession>(Sessions);
            foreach (var s in cliClone)
            {
                if (ignore.Contains(s.GetAttribute('c'))) continue;
                s.Write(msg);
            }
        }

        public void InputClosed(IoSession session)
        {
        }

        public void MessageReceived(IoSession session, object message)
        {
            if (message is VMNetMessage)
            {
                GameThread.NextUpdate(x =>
                {
                    var nmsg = (VMNetMessage)message;
                    var cli = (VMNetClient)session.GetAttribute('c');
                    if (cli.AvatarState == null)
                    {
                        //we're still waiting for the avatar state so the user can join
                        if (nmsg.Type == VMNetMessageType.AvatarData)
                        {
                            var state = new VMNetAvatarPersistState();
                            try
                            {
                                state.Deserialize(new System.IO.BinaryReader(new MemoryStream(nmsg.Data)));
                            }
                            catch (Exception)
                            {
                                return;
                            }
                            cli.PersistID = state.PersistID;
                            cli.AvatarState = state;

                            OnConnect(cli);
                        }
                    }
                    else
                    {
                        OnMessage(cli, nmsg);
                    }
                });
            }
        }

        public void MessageSent(IoSession session, object message)
        {

        }

        public void SessionClosed(IoSession session)
        {
            var cli = (VMNetClient)session.GetAttribute('c');
            if (cli != null) GameThread.NextUpdate(x =>
            {
                OnDisconnect(cli);
            });

            lock (Sessions) Sessions.Remove(session);
        }

        public void SessionCreated(IoSession session)
        {
            var cli = new VMNetClient()
            {
                PersistID = PersistID++,
                RemoteIP = session.RemoteEndPoint.ToString(),
                AvatarState = null,
            };
            cli.NetHandle = session;
            session.SetAttribute('c', cli);

            lock (Sessions) Sessions.Add(session);
        }

        public void SessionIdle(IoSession session, IdleStatus status)
        {

        }

        public void SessionOpened(IoSession session)
        {
        }

        private AsyncSocketAcceptor Acceptor;

        public void Start(ushort port)
        {
            Acceptor = new AsyncSocketAcceptor();
            Acceptor.FilterChain.AddLast("protocol", new ProtocolCodecFilter(new FSOSandboxProtocol()));
            Acceptor.Handler = this;
            System.Net.IPAddress ip;
            System.Net.IPAddress.TryParse("0.0.0.0", out ip);

            try
            {
                Acceptor.Bind(new IPEndPoint(ip, port));
            } catch
            {

            }
        }

        public void Shutdown()
        {
            Acceptor?.Dispose();
            lock (Sessions)
            {
                foreach (var s in Sessions) s.Close(true);
            }
        }
    }
}
