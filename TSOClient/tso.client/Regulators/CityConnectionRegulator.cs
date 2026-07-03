using FSO.Client.Model;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.DatabaseService;
using FSO.Common.DatabaseService.Model;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Common.Domain.Shards;
using FSO.Common.Model;
using FSO.Common.Utils;
using FSO.Content.Model;
using FSO.Server.Clients;
using FSO.Server.Clients.Framework;
using FSO.Server.DataService.Model;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Utils;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.UI.Model;
using Ninject;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FSO.Client.Regulators
{
    public class ConnectArchiveRequest
    {
        public string DisplayName;
        public string CityAddress;
        public bool SelfHost;
    }

    public class CityConnectionRegulator : AbstractRegulator, IAriesMessageSubscriber, IAriesEventSubscriber
    {
        public AriesClient Client { get; internal set; }
        public CityConnectionMode Mode { get; internal set; } = CityConnectionMode.NORMAL;
        public ArchiveClientList UserList { get; internal set; }
        public uint ModerationLevel { get; internal set; }

        private Binding<Avatar> AvatarBinding;

        public ConnectArchiveRequest ArchiveSettings { get; private set; }
        public string ArchiveServerID { get; private set; }
        private string ArchiveToken;
        public ArchiveConfigFlags ArchiveConfig { get; private set; }
        public bool SpectatorMode { get; private set; }

        private CityClient CityApi;
        private ShardSelectorServletResponse ShardSelectResponse;
        public ShardSelectorServletRequest CurrentShard;
        private IDatabaseService DB;
        private IClientDataService DataService;
        private IShardsDomain Shards;
        private IRealestateDomain Realestate;
        private int _ReestablishAttempt;
        private IShardRealestateDomain ShardRealestate;
        private int ReestablishAttempt
        {
            get
            {
                return _ReestablishAttempt;
            }
            set
            {
                FSOFacade.NetStatus.CityReconnectAttempt = value;
                _ReestablishAttempt = value;
            }
        }
        public bool CanReestablish;

        public CityConnectionRegulator(CityClient cityApi, [Named("City")] AriesClient cityClient, IDatabaseService db, IClientDataService ds, IKernel kernel, IShardsDomain shards, IRealestateDomain realestate)
        {
            this.CityApi = cityApi;
            this.Client = cityClient;
            this.Client.AddSubscriber(this);
            this.DB = db;
            this.DataService = ds;
            this.Shards = shards;
            this.Realestate = realestate;

            AddState("Disconnected")
                .Default()
                .Transition()
                .OnData(typeof(ShardSelectorServletRequest))
                .TransitionTo("SelectCity")
                .OnData(typeof(ConnectArchiveRequest))
                .TransitionTo("ArchiveConnect");

            AddState("ArchiveConnect")
                .OnlyTransitionFrom("Disconnected", "Reconnecting");

            AddState("SelectCity")
                .OnlyTransitionFrom("Disconnected", "Reconnecting");

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
                .OnData(typeof(AriesDisconnected)).TransitionTo("OpenSocketDisconnect")
                .OnlyTransitionFrom("CitySelected", "ArchiveConnect");

            AddState("SocketOpen")
                .OnData(typeof(RequestClientSession)).TransitionTo("RequestClientSession")
                .OnData(typeof(RequestClientSessionArchive)).TransitionTo("RequestClientSessionArchive")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnlyTransitionFrom("OpenSocket");

            // Begin archive regulator states

            AddState("RequestClientSessionArchive")
                .OnData(typeof(HostOnlinePDU)).TransitionTo("HostOnline")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnlyTransitionFrom("SocketOpen");

            AddState("ArchiveSelectAvatar")
                .OnData(typeof(ArchiveAvatarSelectResponse)).TransitionTo("ArchiveSelectedAvatar")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnlyTransitionFrom("PartiallyConnected");

            AddState("ArchiveSelectedAvatar")
                .OnlyTransitionFrom("ArchiveSelectAvatar");

            // End archive regulator states

            AddState("RequestClientSession")
                .OnData(typeof(HostOnlinePDU)).TransitionTo("HostOnline")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnlyTransitionFrom("SocketOpen");

            AddState("HostOnline").OnlyTransitionFrom("RequestClientSession", "RequestClientSessionArchive");
            AddState("PartiallyConnected")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnData(typeof(ShardSelectorServletRequest)).TransitionTo("CompletePartialConnection")
                .OnData(typeof(ArchiveAvatarSelectRequest)).TransitionTo("ArchiveSelectAvatar")
                .OnlyTransitionFrom("HostOnline", "ArchiveSelectedAvatar");

            AddState("CompletePartialConnection").OnlyTransitionFrom("PartiallyConnected");
            AddState("AskForAvatarData")
                .OnData(typeof(LoadAvatarByIDResponse)).TransitionTo("ReceivedAvatarData")
                .OnlyTransitionFrom("PartiallyConnected", "CompletePartialConnection", "ArchiveSelectedAvatar");
            AddState("ReceivedAvatarData").OnlyTransitionFrom("AskForAvatarData");
            AddState("AskForCharacterData").OnlyTransitionFrom("ReceivedAvatarData");
            AddState("ReceivedCharacterData").OnlyTransitionFrom("AskForCharacterData");

            AddState("AskForCityData")
                .OnData(typeof(CityInitResponse)).TransitionTo("ReceivedCityData")
                .OnlyTransitionFrom("ReceivedCharacterData");
            AddState("ReceivedCityData").OnlyTransitionFrom("AskForCityData");

            AddState("Connected")
                .OnData(typeof(ServerByePDU)).TransitionTo("Disconnected")
                .OnData(typeof(AriesDisconnected)).TransitionTo("UnexpectedDisconnect")
                .OnlyTransitionFrom("ReceivedCharacterData", "ReceivedCityData", "Reestablished");

            AddState("UnexpectedDisconnect");

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
                .OnData(typeof(ServerByePDU)).TransitionTo("UnexpectedDisconnect")
                .OnData(typeof(AriesDisconnected)).TransitionTo("ReestablishFail")
                .OnlyTransitionFrom("Reestablishing");
            
            AddState("ReestablishFail")
                .OnData(typeof(ServerByePDU)).TransitionTo("UnexpectedDisconnect")
                .OnlyTransitionFrom("Reestablish", "Reestablishing", "Reestablished");

            AddState("Disconnect")
                .OnData(typeof(AriesDisconnected))
                .TransitionTo("Disconnected");

            AddState("Reconnect")
                .OnData(typeof(AriesDisconnected))
                .TransitionTo("Reconnecting");

            AddState("Reconnecting")
                .OnData(typeof(HostOnlinePDU)).TransitionTo("Connected")
                .OnData(typeof(ShardSelectorServletRequest)).TransitionTo("SelectCity")
                .OnlyTransitionFrom("Reconnect");

            ClearUserList();

            AvatarBinding = new Binding<Avatar>().WithBinding(this, "ModerationLevel", "Avatar_ModerationLevel");

            GameThread.SetInterval(() =>
            {
                if (Client.IsConnected)
                {
                    Client.Write(new Server.Protocol.Electron.Packets.KeepAlive());
                }
            }, 10000); //keep alive every 10 seconds. prevents disconnection by aggressive NAT.
        }

        public string ServerIdFromPublicKey(string client)
        {
            HashAlgorithm algorithm = SHA1.Create();
            StringBuilder sb = new StringBuilder();
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(client));
            foreach (byte b in hash)
                sb.Append(b.ToString("X2"));

            return sb.ToString().Substring(0, 8);
        }

        public string ArchiveHash(string client, string server)
        {
            HashAlgorithm algorithm = SHA1.Create();
            StringBuilder sb = new StringBuilder();
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(client + server));
            foreach (byte b in hash)
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        private void ClearUserList()
        {
            UserList = new ArchiveClientList()
            {
                Clients = new ArchiveClient[0],
                Pending = new ArchivePendingVerification[0],
            };
        }

        public void Connect(CityConnectionMode mode, ShardSelectorServletRequest shard)
        {
            ArchiveSettings = null;
            if(shard.ShardName == null && this.CurrentShard != null)
            {
                shard.ShardName = this.CurrentShard.ShardName;
            }
            Mode = mode;
            if (CurrentState.Name != "Disconnected")
            {
                CurrentShard = shard;
                AsyncTransition("Reconnect");
            }
            else
            {
                AsyncProcessMessage(shard);
            }
        }

        public void ConnectArchive(ConnectArchiveRequest request)
        {
            ArchiveSettings = null;
            Mode = CityConnectionMode.ARCHIVE;
            if (CurrentState.Name != "Disconnected")
            {
                // TODO?
                //CurrentShard = shard;
                //AsyncTransition("Reconnect");
            }
            else
            {
                AsyncProcessMessage(request);
            }
        }

        public bool ReturnToSASArchive()
        {
            if (ArchiveSettings != null)
            {
                ConnectArchive(ArchiveSettings);
                return true;
            }

            return false;
        }

        public void Disconnect(){
            AsyncTransition("Disconnect");
        }

        protected override void OnAfterTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
        }

        private static RSA TryGetCrypto(string publicKey)
        {
            try
            {
                var rsa = RSA.Create();

                rsa.ImportFromPem(publicKey);

                return rsa;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private ShardSelectorServletResponse LastSettings;

        protected override void OnBeforeTransition(RegulatorState oldState, RegulatorState newState, object data)
        {
            switch (newState.Name)
            {
                case "ArchiveConnect":
                    var archiveOpt = data as ConnectArchiveRequest;
                    ArchiveSettings = archiveOpt;
                    this.AsyncTransition("OpenSocket", new ShardSelectorServletResponse()
                    {
                        Address = archiveOpt.CityAddress,
                        ExplicitPort = true
                    });
                    break;

                case "SelectCity":
                    //TODO: Do this on logout / disconnect rather than on connect
                    ResetGame();

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
                    SpectatorMode = ShardSelectResponse.SpectatorMode;
                    this.AsyncProcessMessage(ShardSelectResponse);
                    break;

                case "CitySelected":
                    this.AsyncProcessMessage(data);
                    break;

                case "OpenSocket":
                    ReestablishAttempt = 0;
                    var settings = data as ShardSelectorServletResponse;
                    if (settings == null){
                        this.ThrowErrorAndReset(new Exception("Unknown parameter"));
                    }else{
                        //101 is plain
                        LastSettings = settings;
                        Client.Connect(settings.ExplicitPort ? settings.Address : PortTransformer.TransformAddress(settings.Address));
                    }
                    break;

                case "OpenSocketDisconnect":
                    if (ArchiveSettings?.SelfHost == true)
                    {
                        GameThread.SetTimeout(() => AsyncTransition("OpenSocket", LastSettings), 100);
                    }
                    else
                    {
                        AsyncTransition("UnexpectedDisconnect");
                    }
                    break;

                case "SocketOpen":
                    break;

                case "RequestClientSession":
                    Client.Write(new RequestClientSessionResponse {
                        Password = ShardSelectResponse.Ticket,
                        User = ShardSelectResponse.AvatarID.ToString()
                    });
                    break;

                case "RequestClientSessionArchive":
                    var serverRequest = data as RequestClientSessionArchive;

                    var rsa = TryGetCrypto(serverRequest.ServerKey);

                    if (rsa == null || ArchiveSettings == null)
                    {
                        Disconnect();
                    }
                    else
                    {
                        ((ClientShards)Shards).All = new List<ShardStatusItem>()
                        {
                            new ShardStatusItem()
                            {
                                Id = (int)serverRequest.ShardId,
                                Name = serverRequest.ShardName,
                                Status = ShardStatus.Up,
                                Map = serverRequest.ShardMap,
                                PublicHost = ArchiveSettings.CityAddress
                            }
                        };

                        CurrentShard = new ShardSelectorServletRequest()
                        {
                            ShardName = serverRequest.ShardName,
                        };

                        ArchiveConfig = serverRequest.ArchiveConfig;

                        // Our ID for this server is derived from our client GUID - we don't send it directly.
                        var archiveUserID = ArchiveHash(GlobalSettings.Default.ArchiveClientGUID, serverRequest.ServerKey);
                        ArchiveServerID = ServerIdFromPublicKey(serverRequest.ServerKey);
                        DiscordRpcEngine.SetArchiveID(ArchiveServerID);

                        // It's encrypted with the server's public key, so only the owner of this public key should be able to decrypt the id we use for their server.
                        ArchiveToken = Convert.ToBase64String(rsa.Encrypt(Encoding.UTF8.GetBytes($"{serverRequest.Nonce}\\{archiveUserID}"), RSAEncryptionPadding.Pkcs1));

                        Client.Write(new RequestClientSessionResponse
                        {
                            User = ArchiveSettings.DisplayName,
                            Unknown = 40,
                            Password = ArchiveToken,
                        });
                    }
                    break;

                case "HostOnline":
                    ((ClientShards)Shards).CurrentShard = Shards.GetByName(CurrentShard.ShardName).Id;
                    
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

                case "ArchiveSelectAvatar":
                    var avaSelectRequest = data as ArchiveAvatarSelectRequest;
                    CurrentShard.AvatarID = avaSelectRequest.AvatarId.ToString();
                    Client.Write(avaSelectRequest);
                    break;

                case "ArchiveSelectedAvatar":
                    var avaSelectResponse = data as ArchiveAvatarSelectResponse;

                    if (avaSelectResponse.Code == ArchiveAvatarSelectCode.Success)
                    {
                        AsyncTransition("AskForAvatarData");
                    }
                    else
                    {
                        AsyncTransition("PartiallyConnected");
                    }
                    break;

                case "CompletePartialConnection":
                    var shardRequest = (ShardSelectorServletRequest)data;
                    if (Mode != CityConnectionMode.ARCHIVE && shardRequest.ShardName != CurrentShard.ShardName)
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

                case "ReceivedAvatarData":
                    AsyncTransition("AskForCharacterData");
                    break;

                case "AskForCharacterData":
                    DataService.Request(MaskedStruct.MyAvatar, uint.Parse(CurrentShard.AvatarID)).ContinueWith(x =>
                    {
                        if (x.IsFaulted)
                        {
                            ThrowErrorAndReset(new Exception("Failed to load character from db"));
                        }
                        else
                        {
                            var ava = (Avatar)x.Result;
                            AvatarBinding.Value = ava;

                            ModerationLevel = ava.Avatar_ModerationLevel;

                            AsyncTransition("ReceivedCharacterData");
                        }
                    });
                    break;

                case "ReceivedCharacterData":
                    // If the city map was reported as "dynamic", we need to load the map from the server.
                    {
                        var shards = (ClientShards)Shards;
                        var shardItem = shards.GetById(shards.CurrentShard ?? 1);
                        if (shardItem.Map == "dynamic")
                        {
                            // We need to ask the city for the city data.
                            AsyncTransition("AskForCityData");
                        }
                        else
                        {
                            //For now, we will call this connected
                            AsyncTransition("Connected");
                        }
                        break;
                    }

                case "AskForCityData":
                    Client.Write(
                        new CityInitRequest()
                        {
                        }
                    );
                    break;

                case "ReceivedCityData":
                    var cityInit = data as CityInitResponse;

                    GameThread.InUpdate(() =>
                    {
                        var shards = (ClientShards)Shards;
                        var shardId = shards.CurrentShard ?? 1;
                        var marshal = new CityMapMarshal();
                        marshal.Read(cityInit.CityData);

                        shards.SetShardMapBase(shardId, marshal);

                        // Initialize the realestate with the initial command list

                        ShardRealestate = Realestate.GetByShard(shardId);
                        foreach (var cmd in cityInit.Commands)
                        {
                            ShardRealestate.AppendCommand(cmd.Command);
                        }

                        AsyncTransition("Connected");
                    });

                    break;

                case "Connected":
                    CanReestablish = true;
                    break;

                case "UnexpectedDisconnect":
                    if (ReestablishAttempt > 0 || !CanReestablish)
                    {
                        GameThread.InUpdate(() =>
                        {
                            FSOFacade.Controller.FatalNetworkError(23);
                        });
                    }
                    else
                    {
                        AsyncTransition("Reestablish");
                    }
                    break;

                case "Reestablish":
                    ReestablishAttempt++;
                    Client.Connect(LastSettings.ExplicitPort ? LastSettings.Address : PortTransformer.TransformAddress(LastSettings.Address));
                    break;

                case "Reestablishing":
                    if (ArchiveSettings != null)
                    {
                        Client.Write(new RequestClientSessionResponse
                        {
                            User = CurrentShard.AvatarID,
                            Password = ArchiveToken,
                            Unknown = 40,
                            Unknown2 = 1
                        });
                    }
                    else
                    {
                        Client.Write(new RequestClientSessionResponse
                        {
                            Password = ShardSelectResponse.Ticket,
                            User = ShardSelectResponse.AvatarID,
                            Unknown2 = 1
                        });
                    }
                    break;

                case "Reestablished":
                    Client.Write(
                        new ClientOnlinePDU
                        {
                        }
                        );
                    ReestablishAttempt = 0;
                    AsyncTransition("Connected");
                    break;

                case "ReestablishFail":
                    if (ReestablishAttempt < 10 && CanReestablish)
                    {
                        GameThread.SetTimeout(() =>
                        {
                            if (CurrentState?.Name == "ReestablishFail") AsyncTransition("Reestablish");
                        }, 1000);
                    } else
                    {
                        AsyncTransition("UnexpectedDisconnect");
                    }
                    break;

                case "Disconnect":
                    ShardSelectResponse = null;
                    ReestablishAttempt = 0;
                    CanReestablish = false;
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

                case "Reconnect":
                    ShardSelectResponse = null;
                    if (Client.IsConnected)
                    {
                        Client.Write(new ClientByePDU());
                        Client.Disconnect();
                    }
                    else
                    {
                        AsyncTransition("Reconnecting");
                    }
                    break;
                case "Reconnecting":
                    AsyncProcessMessage(CurrentShard);

                    break;
                case "Disconnected":
                    DiscordRpcEngine.Reset();
                    ((ClientShards)Shards).CurrentShard = null;
                    Realestate.Reset();
                    ReestablishAttempt = 0;
                    CanReestablish = false;
                    ClearUserList();
                    break;
            }
        }

        public void ResetGame()
        {
            UserReference.ResetCache();
        }


        public void MessageReceived(AriesClient client, object message)
        {

            if (message is RequestClientSession || message is RequestClientSessionArchive ||
                message is HostOnlinePDU || message is ServerByePDU || message is ArchiveAvatarSelectResponse || message is CityInitResponse)
            {
                this.AsyncProcessMessage(message);
            }
            else if (message is ArchiveClientList list)
            {
                GameThread.InUpdate(() =>
                {
                    UserList = list;
                    // TODO: notify
                });
            }
            else if (message is AnnouncementMsgPDU msg)
            {
                GameThread.InUpdate(() =>
                {
                    if (msg.Badge == 255)
                    {
                        // This message replaces the network error message when we disconnect.
                        FSOFacade.Controller.FatalErrorMessage(msg);
                    }
                    else
                    {
                        UIAlert alert = null;
                        alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                        {
                            Title = GameFacade.Strings.GetString("195", "30") + GameFacade.CurrentCityName,
                            Message = GameFacade.Strings.GetString("195", "28") + msg.SenderID.Substring(2) + "\r\n"
                            + GameFacade.Strings.GetString("195", "29") + msg.Subject + "\r\n"
                            + msg.Message,
                            Buttons = UIAlertButton.Ok((btn) => UIScreen.RemoveDialog(alert)),
                            Alignment = TextAlignment.Left
                        }, true);
                    }
                });
            }
            else if (message is GlobalTuningUpdate tuning)
            {
                DynamicTuning.Global = tuning.Tuning;
                Content.Content.Get().Upgrades.LoadNetTuning(tuning.ObjectUpgrades);
            }
            else if (message is CityUpdateResponse city)
            {
                GameThread.NextUpdate(x =>
                {
                    foreach (var cmd in city.Commands)
                    {
                        ShardRealestate?.AppendCommand(cmd.Command);
                    }
                });
            }
            else if (message is CityUpdateCommand cmd)
            {
                GameThread.NextUpdate(x =>
                {
                    switch (cmd.Mode)
                    {
                        case CityUpdateCommandMode.Undo:
                            ShardRealestate?.HandleUserCommand(cmd);
                            break;
                        case CityUpdateCommandMode.SetCityName:
                            GameFacade.CurrentCityName = cmd.CityName;
                            break;
                    }
                });
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
            AsyncProcessMessage(new AriesDisconnected());
        }
    }

    public enum CityConnectionMode
    {
        CAS,
        NORMAL,
        ARCHIVE
    }

    class AriesConnected {

    }

    class AriesDisconnected {

    }
}
