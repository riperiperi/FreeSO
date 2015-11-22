using FSO.Server.Clients;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Gluon.Packets;
using Ninject;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot.Lifecycle
{
    public class CityConnections
    {
        private Dictionary<LotServerConfigurationCity, CityConnection> Connections;
        private Thread ConnectionWatcher;
        private bool _Running;

        private PerformanceCounter CpuCounter;
        private LotServerConfiguration Config;

        public CityConnections(LotServerConfiguration config, IKernel kernel)
        {
            Config = config;
            CpuCounter = new PerformanceCounter();
            CpuCounter.CategoryName = "Processor";
            CpuCounter.CounterName = "% Processor Time";
            CpuCounter.InstanceName = "_Total";

            var firstValue = CpuCounter.NextValue();

            Connections = new Dictionary<LotServerConfigurationCity, CityConnection>();
            foreach(var city in config.Cities)
            {
                Connections.Add(city, new CityConnection(kernel, city, config.Call_Sign));
            }
        }

        public void Start()
        {
            Stop();

            _Running = true;
            ConnectionWatcher = new Thread(CheckConnections);
            ConnectionWatcher.Start();
        }

        public void Stop()
        {
            _Running = false;
            if (ConnectionWatcher != null)
            {
                try
                {
                    ConnectionWatcher.Interrupt();
                }
                catch (Exception ex)
                {
                }
            }
        }

        private void CheckConnections()
        {
            while (_Running)
            {
                var cpu = CpuCounter.NextValue();
                var capacity = new AdvertiseCapacity
                {
                    CpuPercentAvg = (byte)(cpu * 100),
                    CurrentLots = 0,
                    MaxLots = 10,
                    RamAvaliable = 0,
                    RamUsed = 0
                };

                //Repair & advertise connections
                foreach (var connection in Connections.Values)
                {
                    if (!connection.Connected)
                    {
                        connection.Connect();
                    }else{
                        connection.Write(capacity);
                    }
                }

                Thread.Sleep(Config.CityReportingInterval);
            }
        }
    }

    public class CityConnection : IAriesEventSubscriber, IAriesMessageSubscriber, IGluonSession
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        
        private AriesClient Client;
        public LotServerConfigurationCity CityConfig;
        public bool Connected { get; internal set; }

        public bool IsAuthenticated
        {
            get
            {
                return false;
            }
        }

        public string CallSign { get; set; }

        private bool _Connecting = false;
        private IAriesPacketRouter _Router;

        public CityConnection(IKernel kernel, LotServerConfigurationCity config, string callSign)
        {
            CityConfig = config;
            Client = new AriesClient(kernel);
            Client.AddSubscriber(this);
            CallSign = callSign;
            _Router = kernel.Get<IAriesPacketRouter>();
        }
        
        public void Connect()
        {
            if (_Connecting || Connected) { return; }

            _Connecting = true;

            //TODO: Fix TLS support
            var endpoint = CityConfig.Host.Replace("100", "101");
            LOG.Info("Lot server connecting to city server: " + endpoint);
            Client.Connect(endpoint);
        }

        public void AuthenticationEstablished()
        {
            Connected = true;
            _Connecting = false;

            var endpoint = CityConfig.Host.Replace("100", "101");
            LOG.Info("Lot server connected to city server: " + endpoint);
        }

        public void MessageReceived(AriesClient client, object message)
        {
            ((AriesPacketRouter)_Router).Handle(this, message);
        }

        public void SessionCreated(AriesClient client)
        {
        }

        public void SessionOpened(AriesClient client)
        {

        }

        public void SessionClosed(AriesClient client)
        {
            Connected = false;
            _Connecting = false;
        }

        public void SessionIdle(AriesClient client)
        {

        }

        public void InputClosed(AriesClient session)
        {

        }

        public void Write(params object[] messages)
        {
            Client.Write(messages);
        }

        public void Close()
        {
            Client.Disconnect();
        }

        public object GetAttribute(string key)
        {
            return null;
        }

        public void SetAttribute(string key, object value)
        {
        }
    }
}
