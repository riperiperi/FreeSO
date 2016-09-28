using FSO.Server.Clients;
using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Voltron.Packets;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Regulators
{
    public class LotConnectionRegulator : AbstractRegulator, IAriesMessageSubscriber, IAriesEventSubscriber
    {
        //Lot connection client
        public AriesClient Client { get; internal set; }
        private AriesClient City;
        private uint LotId;

        private FindLotResponse FindLotResponse;

        public LotConnectionRegulator([Named("City")] AriesClient cityClient, [Named("Lot")] AriesClient lotClient)
        {
            this.City = cityClient;
            this.City.AddSubscriber(this);

            this.Client = lotClient;
            this.Client.AddSubscriber(this);

            City.AddSubscriber(this);

            AddState("Disconnected")
                .Transition()
                .OnData(typeof(JoinLotRequest)).TransitionTo("SelectLot")
                .Default();

            AddState("SelectLot").OnlyTransitionFrom("Disconnected");
            AddState("FindLot").OnlyTransitionFrom("SelectLot").OnData(typeof(FindLotResponse)).TransitionTo("FoundLot");
            AddState("FoundLot").OnlyTransitionFrom("FindLot");

            AddState("OpenSocket")
                .OnData(typeof(AriesConnected)).TransitionTo("SocketOpen")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnlyTransitionFrom("FoundLot");

            AddState("SocketOpen")
                .OnData(typeof(RequestClientSession)).TransitionTo("RequestClientSession")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnlyTransitionFrom("OpenSocket");

            AddState("RequestClientSession")
                .OnData(typeof(HostOnlinePDU)).TransitionTo("HostOnline")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnlyTransitionFrom("SocketOpen");

            AddState("HostOnline").OnlyTransitionFrom("RequestClientSession");
            AddState("PartiallyConnected")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnData(typeof(FSOVMTickBroadcast)).TransitionTo("LotCommandStream")
                .OnData(typeof(FSOVMDirectToClient)).TransitionTo("LotCommandStream")
                .OnlyTransitionFrom("HostOnline");

            AddState("LotCommandStream")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnData(typeof(FSOVMTickBroadcast)).TransitionTo("LotCommandStream")
                .OnData(typeof(FSOVMDirectToClient)).TransitionTo("LotCommandStream");

            AddState("UnexpectedDisconnect");
            
            AddState("Disconnect")
                .OnData(typeof(AriesDisconnected))
                .TransitionTo("Disconnected");
        }

        protected override void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
            switch (newState.Name)
            {
                case "SelectLot":
                    AsyncTransition("FindLot", data);
                    break;

                case "FindLot":
                    LotId = ((JoinLotRequest)data).LotId;
                    City.Write(new FSO.Server.Protocol.Electron.Packets.FindLotRequest {
                        LotId = LotId
                    });
                    break;
                case "FoundLot":
                    var result = (FindLotResponse)data;
                    if(result.Status == Server.Protocol.Electron.Model.FindLotResponseStatus.FOUND)
                    {
                        LotId = result.LotId;
                        FindLotResponse = result;
                        AsyncTransition("OpenSocket", result.Address);
                    }
                    else
                    {
                        ThrowErrorAndReset(result.Status);
                    }
                    break;

                case "OpenSocket":
                    var address = data as string;
                    if (address == null)
                    {
                        this.ThrowErrorAndReset(new Exception("Unknown parameter"));
                    }
                    else
                    {
                        //101 is plain
                        Client.Connect(address + "101");
                    }
                    break;

                case "SocketOpen":
                    break;

                case "RequestClientSession":
                    Client.Write(new RequestClientSessionResponse
                    {
                        Password = FindLotResponse.LotServerTicket,
                        User = FindLotResponse.User
                    });
                    break;

                case "HostOnline":
                    Client.Write(
                        new ClientOnlinePDU
                        {
                        }
                    );
                    
                    AsyncTransition("PartiallyConnected");
                    break;
                case "UnexpectedDisconnect":
                    AsyncTransition("Disconnected");
                    break;

                case "Disconnect":
                    if (Client.IsConnected)
                    {
                        Client.Write(new ClientByePDU());
                        Client.Disconnect();
                    }
                    else
                    {
                        AsyncTransition("Disconnected");
                    }
                    break;
            }
        }

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
        }

        public void JoinLot(uint id)
        {
            AsyncProcessMessage(new JoinLotRequest { LotId = id });
        }

        public uint GetCurrentLotID()
        {
            if (CurrentState.Name == "LotCommandStream") return LotId;
            else return 0;
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if (client == City)
            {
                if (message is FindLotResponse)
                {
                    AsyncProcessMessage(message);
                }
            }
            else
            {
                if (message is RequestClientSession ||
                    message is HostOnlinePDU ||
                    message is FSOVMTickBroadcast ||
                    message is FSOVMDirectToClient)
                {
                    this.AsyncProcessMessage(message);
                }
            }
        }

        public void SessionCreated(AriesClient client)
        {
            this.AsyncProcessMessage(new AriesConnected());
        }

        public void SessionOpened(AriesClient client)
        {

        }

        public void SessionClosed(AriesClient client)
        {
            AsyncProcessMessage(new AriesDisconnected());
        }

        public void SessionIdle(AriesClient client)
        {

        }

        public void InputClosed(AriesClient session)
        {

        }
    }

    class JoinLotRequest
    {
        public uint LotId;
    }
}
