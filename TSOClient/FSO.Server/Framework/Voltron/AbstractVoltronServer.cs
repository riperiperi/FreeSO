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

namespace FSO.Server.Framework.Voltron
{
    public class AbstractVoltronServer : AbstractServer, IoHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IKernel Kernel;

        private CityServerConfiguration Config;
        private Shard Shard;

        private IoAcceptor Acceptor;
        private IServerDebugger Debugger;

        public AbstractVoltronServer(CityServerConfiguration config, IKernel kernel)
        {
            this.Kernel = kernel;
            this.Config = config;
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
                Acceptor.FilterChain.AddLast("ssl", ssl);
                if(Debugger != null)
                {
                    Acceptor.FilterChain.AddLast("packetLogger", new AriesProtocolLogger(Debugger.GetPacketLogger()));
                }
                Acceptor.FilterChain.AddLast("protocol", new ProtocolCodecFilter(Kernel.Get<AriesProtocol>()));
                Acceptor.Handler = this;

                Acceptor.Bind(CreateIPEndPoint(Config.Binding));
                LOG.Info("Listening on " + Acceptor.LocalEndPoint);
            }catch(Exception ex)
            {
                LOG.Error("Unknown error bootstrapping server", ex);
            }
        }

        private void Bootstrap()
        {
            var shardsData = Kernel.Get<ShardsDataService>();
            this.Shard = shardsData.GetById(Config.ID);
            if(this.Shard == null)
            {
                throw new Exception("Unable to find a shard with id " + Config.ID + ", check it exists in the database");
            }

            LOG.Info("City identified as " + Shard.name);
        }

        public override void Shutdown()
        {

        }


        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length != 2) throw new FormatException("Invalid endpoint format");
            System.Net.IPAddress ip;
            if (!System.Net.IPAddress.TryParse(ep[0], out ip))
            {
                throw new FormatException("Invalid ip-adress");
            }
            int port;
            if (!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }

        public void SessionCreated(IoSession session)
        {
            LOG.Info("[SESSION-CREATE]");

            session.Write(new HostOnlinePDU
            {
                ClientBufSize = 4096,
                HostVersion = 0x7FFF,
                HostReservedWords = 0
            });
        }

        public void SessionOpened(IoSession session)
        {
        }

        public void SessionClosed(IoSession session)
        {
            LOG.Info("[SESSION-CLOSED]");
        }

        public void SessionIdle(IoSession session, IdleStatus status)
        {
        }

        public void ExceptionCaught(IoSession session, Exception cause)
        {
            LOG.Error("Unknown error", cause);
        }

        public void MessageReceived(IoSession session, object message)
        {
        }

        public void MessageSent(IoSession session, object message)
        {
        }

        public void InputClosed(IoSession session)
        {
        }

    }
}
