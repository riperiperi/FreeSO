using FSO.Server.Clients;
using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Voltron.Packets;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Regulators
{
    public class CityConnectionRegulator : AbstractRegulator, IAriesMessageSubscriber, IAriesEventSubscriber
    {
        public AriesClient Client { get; internal set; }
        public CityConnectionMode Mode { get; internal set; } = CityConnectionMode.NORMAL;

        private CityClient CityApi;
        private ShardSelectorServletResponse ShardSelectResponse;

        public CityConnectionRegulator(CityClient cityApi, IKernel kernel)
        {
            this.CityApi = cityApi;
            this.Client = kernel.Get<AriesClient>();
            this.Client.AddSubscriber(this);

            AddState("Disconnected")
                .Default()
                .Transition()
                .OnData(typeof(ShardSelectorServletRequest))
                .TransitionTo("ShardSelect");

            AddState("ShardSelect")
                .OnData(typeof(ShardSelectorServletResponse))
                .TransitionTo("OpenSocket")
                .OnlyTransitionFrom("Disconnected");

            AddState("OpenSocket")
                .OnData(typeof(RequestClientSession))
                .TransitionTo("RequestClientSession")
                .OnlyTransitionFrom("ShardSelect");

            AddState("RequestClientSession")
                .OnData(typeof(HostOnlinePDU))
                .TransitionTo("Connected")
                .OnlyTransitionFrom("OpenSocket");

            AddState("Connected").OnlyTransitionFrom("RequestClientSession");
        }

        public void Connect(CityConnectionMode mode, ShardSelectorServletRequest shard)
        {
            Mode = mode;
            AsyncProcessMessage(shard);
        }

        protected override void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
        }

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
            switch (newState.Name)
            {
                case "ShardSelect":
                    var shard = data as ShardSelectorServletRequest;
                    if (shard == null)
                    {
                        this.ThrowErrorAndReset(new Exception("Unknown parameter"));
                    }
                    else
                    {
                        ShardSelectResponse = CityApi.ShardSelectorServlet(shard);
                        this.AsyncProcessMessage(ShardSelectResponse);
                    }
                    break;

                case "OpenSocket":
                    var settings = data as ShardSelectorServletResponse;
                    if (settings == null)
                    {
                        this.ThrowErrorAndReset(new Exception("Unknown parameter"));
                    }
                    else
                    {
                        Client.Connect(settings.Address + "100");
                    }
                    break;

                case "RequestClientSession":
                    Client.Write(new RequestClientSessionResponse {
                        Password = ShardSelectResponse.Ticket,
                        User = ShardSelectResponse.PlayerID.ToString()
                    });
                    break;
            }
        }



        public void MessageReceived(AriesClient client, object message)
        {
            this.AsyncProcessMessage(message);
        }

        public void SessionCreated(AriesClient client)
        {

        }

        public void SessionOpened(AriesClient client)
        {

        }

        public void SessionClosed(AriesClient client)
        {

        }

        public void SessionIdle(AriesClient client)
        {

        }

        public void InputClosed(AriesClient session)
        {

        }
    }

    public enum CityConnectionMode
    {
        CAS,
        NORMAL
    }
}
