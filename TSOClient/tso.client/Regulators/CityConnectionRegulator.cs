using FSO.Client.Network.DB;
using FSO.Client.UI.Controls;
using FSO.Client.Utils;
using FSO.Server.Clients;
using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Voltron.DataService;
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
        private ShardSelectorServletRequest CurrentShard;
        private DBService DB;

        public CityConnectionRegulator(CityClient cityApi, [Named("City")] AriesClient cityClient, DBService db, IKernel kernel)
        {
            this.CityApi = cityApi;
            this.Client = cityClient;
            this.Client.AddSubscriber(this);
            this.DB = db;

            AddState("Disconnected")
                .Default()
                .Transition()
                .OnData(typeof(ShardSelectorServletRequest))
                .TransitionTo("SelectCity");

            AddState("SelectCity")
                .OnlyTransitionFrom("Disconnected");

            AddState("ConnectToCitySelector")
                .OnData(typeof(ShardSelectorServletResponse))
                .TransitionTo("CitySelected")
                .OnlyTransitionFrom("SelectCity");

            AddState("CitySelected")
                .OnData(typeof(ShardSelectorServletResponse))
                .TransitionTo("OpenSocket")
                .OnlyTransitionFrom("ConnectToCitySelector");

            AddState("OpenSocket")
                .OnData(typeof(AriesConnected)).TransitionTo("SocketOpen")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnlyTransitionFrom("CitySelected");

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
                .OnData(typeof(ShardSelectorServletRequest)).TransitionTo("CompletePartialConnection")
                .OnlyTransitionFrom("HostOnline");

            AddState("CompletePartialConnection").OnlyTransitionFrom("PartiallyConnected");
            AddState("AskForAvatarData")
                .OnData(typeof(LoadAvatarByIDResponse)).TransitionTo("ReceivedAvatarData")
                .OnlyTransitionFrom("PartiallyConnected", "CompletePartialConnection");
            AddState("ReceivedAvatarData").OnlyTransitionFrom("AskForAvatarData");

            AddState("UnexpectedDisconnect");

            AddState("Disconnect")
                .OnData(typeof(AriesDisconnected))
                .TransitionTo("Disconnected");
        }

        public void Connect(CityConnectionMode mode, ShardSelectorServletRequest shard)
        {
            if(shard.ShardName == null && this.CurrentShard != null)
            {
                shard.ShardName = this.CurrentShard.ShardName;
            }
            Mode = mode;
            AsyncProcessMessage(shard);
        }

        public void Disconnect(){
            AsyncTransition("Disconnect");
        }

        protected override void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
        }

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
            switch (newState.Name)
            {
                case "SelectCity":
                    var shard = data as ShardSelectorServletRequest;
                    if (shard == null)
                    {
                        this.ThrowErrorAndReset(new Exception("Unknown parameter"));
                    }

                    this.AsyncTransition("ConnectToCitySelector", shard);
                    break;

                case "ConnectToCitySelector":
                    shard = data as ShardSelectorServletRequest;
                    CurrentShard = shard;
                    ShardSelectResponse = CityApi.ShardSelectorServlet(shard);
                    this.AsyncProcessMessage(ShardSelectResponse);
                    break;

                case "CitySelected":
                    this.AsyncProcessMessage(data);
                    break;

                case "OpenSocket":
                    var settings = data as ShardSelectorServletResponse;
                    if (settings == null){
                        this.ThrowErrorAndReset(new Exception("Unknown parameter"));
                    }else{
                        Client.Connect(settings.Address + "100");
                    }
                    break;

                case "SocketOpen":
                    break;

                case "RequestClientSession":
                    Client.Write(new RequestClientSessionResponse {
                        Password = ShardSelectResponse.Ticket,
                        User = ShardSelectResponse.PlayerID.ToString()
                    });
                    break;

                case "HostOnline":
                    Client.Write(
                        new ClientOnlinePDU {
                        },
                        new SetIgnoreListPDU {
                            PlayerIds = new List<uint>()
                        },
                        new SetInvinciblePDU{
                            Action = 0
                        }
                    );
                    AsyncTransition("PartiallyConnected");
                    break;

                case "PartiallyConnected":
                    if(Mode == CityConnectionMode.NORMAL){
                        AsyncTransition("AskForAvatarData");
                    }
                    break;

                case "CompletePartialConnection":
                    var shardRequest = (ShardSelectorServletRequest)data;
                    if (shardRequest.ShardName != CurrentShard.ShardName)
                    {
                        //Should never get into this state
                        throw new Exception("You cant complete a partial connection for a different city");
                    }
                    CurrentShard = shardRequest;
                    AsyncTransition("AskForAvatarData");
                    break;

                case "AskForAvatarData":
                    DB.LoadAvatarById(new LoadAvatarByIDRequest
                    {
                        AvatarId = uint.Parse(CurrentShard.AvatarID)
                    }).ContinueWith(x =>
                    {
                        if (x.IsFaulted) {
                            ThrowErrorAndReset(new Exception("Failed to load avatar from db"));
                        } else{
                            AsyncProcessMessage(x.Result);
                        }
                    });
                    break;

                case "UnexpectedDisconnect":
                    GameFacade.Controller.FatalNetworkError(23);
                    break;

                case "Disconnect":
                    ShardSelectResponse = null;
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



        public void MessageReceived(AriesClient client, object message)
        {
            if (message is RequestClientSession || 
                message is HostOnlinePDU){
                this.AsyncProcessMessage(message);
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

    public enum CityConnectionMode
    {
        CAS,
        NORMAL
    }

    class AriesConnected {

    }

    class AriesDisconnected {

    }
}
