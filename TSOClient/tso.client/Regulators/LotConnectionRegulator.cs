using FSO.Common.DataService;
using FSO.Common.Utils;
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
        private bool IsDisconnecting = true;
        private string LastAddress;
        private int _ReestablishAttempt;
        private int ReestablishAttempt
        {
            get
            {
                return _ReestablishAttempt;
            }
            set
            {
                FSOFacade.NetStatus.LotReconnectAttempt = value;
                _ReestablishAttempt = value;
            }
        }

        private FindLotResponse FindLotResponse;
        private IClientDataService DataService;

        public LotConnectionRegulator([Named("City")] AriesClient cityClient, [Named("Lot")] AriesClient lotClient, IClientDataService dataService)
        {
            this.City = cityClient;
            this.City.AddSubscriber(this);

            this.Client = lotClient;
            this.Client.AddSubscriber(this);

            this.DataService = dataService;

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
                .OnData(typeof(FSOVMDirectToClient)).TransitionTo("LotCommandStream")
                .OnData(typeof(ServerByePDU)).TransitionTo("Disconnected");

            AddState("Reestablish")
                .OnData(typeof(ServerByePDU)).TransitionTo("UnexpectedDisconnect")
                .OnData(typeof(AriesDisconnected)).TransitionTo("ReestablishFail")
                .OnData(typeof(AriesConnected)).TransitionTo("Reestablishing")
                .OnlyTransitionFrom("UnexpectedDisconnect", "ReestablishFail");

            AddState("Reestablishing")
                .OnData(typeof(AriesDisconnected)).TransitionTo("ReestablishFail")
                .OnData(typeof(ServerByePDU)).TransitionTo("UnexpectedDisconnect")
                .OnData(typeof(HostOnlinePDU)).TransitionTo("Reestablished")
                .OnlyTransitionFrom("Reestablish");

            AddState("Reestablished")
            .OnData(typeof(AriesDisconnected)).TransitionTo("ReestablishFail")
            .OnlyTransitionFrom("Reestablishing");

            AddState("ReestablishFail")
                .OnlyTransitionFrom("Reestablish", "Reestablishing");

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
                    IsDisconnecting = false;
                    AsyncTransition("FindLot", data);
                    break;

                case "FindLot":
                    //LotId = ((JoinLotRequest)data).LotId;
                    City.Write(new FSO.Server.Protocol.Electron.Packets.FindLotRequest {
                        LotId = ((JoinLotRequest)data).LotId
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
                    ReestablishAttempt = 0;
                    var address = data as string;
                    LastAddress = address;
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

                    //When we join a property, get the lot info to update the thumbnail cache
                    DataService.Request(Server.DataService.Model.MaskedStruct.PropertyPage_LotInfo, LotId);
                    break;
                case "UnexpectedDisconnect":
                    if (ReestablishAttempt > 0)
                    {
                        IsDisconnecting = true;
                        AsyncTransition("Disconnected");
                    }
                    else
                    {
                        AsyncTransition("Reestablish");
                    }
                    break;

                case "Reestablish":
                    ReestablishAttempt++;
                    Client.Connect(LastAddress + "101");
                    break;

                case "Reestablishing":
                    Client.Write(new RequestClientSessionResponse
                    {
                        Password = FindLotResponse.LotServerTicket,
                        User = FindLotResponse.User,
                        Unknown2 = 1
                    });
                    break;

                case "Reestablished":
                    Client.Write(
                        new ClientOnlinePDU
                        {
                        }
                        );
                    ReestablishAttempt = 0;
                    AsyncTransition("LotCommandStream");
                    break;

                case "ReestablishFail":
                    if (ReestablishAttempt < 10)
                    {
                        GameThread.SetTimeout(() =>
                        {
                            AsyncTransition("Reestablish");
                        }, 1000);
                    }
                    else
                    {
                        AsyncTransition("UnexpectedDisconnect");
                    }
                    break;


                case "Disconnect":
                    if (Client.IsConnected && !IsDisconnecting)
                    {
                        Client.Write(new ClientByePDU());
                        Client.Disconnect();

                        //When we leave a property, get the lot info to update the thumbnail cache
                        DataService.Request(Server.DataService.Model.MaskedStruct.PropertyPage_LotInfo, LotId);
                    }
                    else
                    {
                        AsyncTransition("Disconnected");
                    }
                    break;

                case "Disconnected":
                    ReestablishAttempt = 0;
                    break;
            }
        }

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
        }
        public void Disconnect()
        {
            AsyncTransition("Disconnect");
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
                    message is FSOVMDirectToClient ||
                    message is ServerByePDU)
                {
                    if (message is ServerByePDU) { }
                    //force in order
                    this.SyncProcessMessage(message);
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
            if (client == Client)
                AsyncProcessMessage(new AriesDisconnected());
        }

        public void SessionIdle(AriesClient client)
        {

        }

        public void InputClosed(AriesClient session)
        {
            AsyncProcessMessage(new AriesDisconnected());
        }
    }

    class JoinLotRequest
    {
        public uint LotId;
    }
}
