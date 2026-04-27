using FSO.Client.Controllers.Panels;
using FSO.Client.Model;
using FSO.Client.Regulators;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Enum;
using FSO.Common.Model;
using FSO.Common.Utils;
using FSO.Files.Formats.tsodata;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Model;
using Microsoft.Xna.Framework;
using Ninject;
using Ninject.Parameters;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace FSO.Client.Controllers
{
    public class CoreGameScreenController : IDisposable
    {
        public CoreGameScreen Screen;
        private MessagingController Chat;
        private RoommateRequestController RoommateProtocol;
        private Network.Network Network;
        private IClientDataService DataService;
        private LotConnectionRegulator JoinLotRegulator;
        /// <summary>
        /// Lot to connect to immediately after disconnecting. Used for job lots and switching lots.
        /// </summary>
        public uint ReconnectLotID = 0;
        public LotTransitionInfo ReconnectTransition;

        public TerrainController Terrain;
        public NeighborhoodActionController NeighborhoodProtocol;
        public BulletinActionController BulletinProtocol;
        public CityResourceController CityResource;

        public CityConnectionMode Mode => Network.Mode;
        public ArchiveConfigFlags ArchiveConfig => Network.ArchiveConfig;
        public ConnectArchiveRequest ArchiveHost => Network.ArchiveHost;
        public bool LocalTransition => ReconnectLotID != 0 && ReconnectTransition != null;

        public CoreGameScreenController(CoreGameScreen view, Network.Network network, IClientDataService dataService, IKernel kernel, LotConnectionRegulator joinLotRegulator)
        {
            view.Controller = this; // Set this early so all FindController<> calls work.
            this.Screen = view;
            this.Network = network;
            this.DataService = dataService;
            this.Chat = new MessagingController(this, view.MessageTray, network, dataService);
            this.CityResource = new CityResourceController(network);
            this.JoinLotRegulator = joinLotRegulator;
            this.RoommateProtocol = new RoommateRequestController(this, network, dataService);
            this.NeighborhoodProtocol = kernel.Get<NeighborhoodActionController>();
            this.BulletinProtocol = kernel.Get<BulletinActionController>();

            joinLotRegulator.OnTransition += JoinLotRegulator_OnTransition;

            var shard = Network.MyShard;
            Terrain = kernel.Get<TerrainController>(new ConstructorArgument("parent", this));
            view.Initialize(shard.Name, Terrain);
            view.AllowCityEditor = ArchiveConfig.HasFlag(ArchiveConfigFlags.CityEditor); // TODO: only if admin
        }

        public void AddWindow(UIContainer window)
        {
            Screen.WindowContainer.Add(window);

            var position = new Vector2(25, 25);

            /*
            var bounds = Screen.GetBounds();

            window.X = ((bounds.Width - window.Size.X) / 2);
            window.Y = ((bounds.Height - window.Size.Y) / 2);
            */

            while (Screen.WindowContainer.GetChildren().Any(x => x.Position == position)) position += new Vector2(50, 50);
            window.Position = position;
        }

        public void RemoveWindow(UIContainer window)
        {
            Screen.WindowContainer.Remove(window);
        }

        private void JoinLotRegulator_OnTransition(string transition, object data)
        {
            GameThread.InUpdate(() =>
            {
                switch (transition)
                {
                    case "UnexpectedDisconnect":
                        //todo: what if we disconnect from lot but not city? the reverse?
                        break;
                    case "Disconnected":
                        Screen.CleanupLastWorld();

                        if (ReconnectLotID != 0)
                        {
                            GameThread.InUpdate(() => {
                                if (ReconnectLotID != 0) JoinLot(ReconnectLotID, ReconnectTransition);
                            });
                        }
                        //destroy the currently active lot (if possible)
                        break;
                    case "PartiallyConnected":
                        Screen.InitializeLot();
                        Screen.vm.MyUID = Network.MyCharacter;
                        //initialize a lot
                        break;
                    case "LotCommandStream":
                        //forward the command to the VM
                        //doesn't really need to be next update... but we don't want to catch the VM in a half-init state.
                        if (data == null) break;
                        VMNetMessage msg = null;
                        if (data is FSOVMTickBroadcast broadcast)
                            msg = new VMNetMessage(broadcast.Catchup ? VMNetMessageType.CatchupTick : VMNetMessageType.BroadcastTick, broadcast.Data);
                        else
                            msg = new VMNetMessage(VMNetMessageType.Direct, ((FSOVMDirectToClient)data).Data);

                        Screen.Driver?.ServerMessage(msg);
                        break;
                }
            });
        }

        public void JoinLot(uint id, LotTransitionInfo transition = null)
        {
            var lot = JoinLotRegulator.GetCurrentLotID();
            if (lot == 0)
            {
                JoinLotRegulator.JoinLot(id, transition);
                ReconnectLotID = 0;
                ReconnectTransition = transition;
            }
            else if (lot == id)
            {
                //we're already on this lot. zoom in!
                Screen.ZoomLevel = 0;
            }
            else
            {
                //we're in a lot. Ask the user if we can leave the current one.
                Screen.ShowReconnectDialog(id);
            }
        }

        public void SwitchLot(uint id, LotTransitionInfo transition)
        {
            if (JoinLotRegulator.GetCurrentLotID() == 0)
            {
                JoinLotRegulator.JoinLot(id, transition);
                ReconnectLotID = 0;
                ReconnectTransition = null;
            }
            else
            {
                //force a switch to the target lot
                JoinLotRegulator.LeavingLot = true;
                ReconnectLotID = id;
                ReconnectTransition = transition;
                Screen.InitiateLotSwitch();

                // If there's a transition, we can leave immediately.
                if (transition != null)
                {
                    JoinLotRegulator.Disconnect();
                }
            }
        }

        public uint GetVisualLotID()
        {
            uint lotID = Screen.VisualVM?.TSOState?.LotID ?? 0;

            return lotID == 0 ? JoinLotRegulator.GetCurrentLotID() : lotID;
        }

        public uint GetCurrentLotID()
        {
            return JoinLotRegulator.GetCurrentLotID();
        }

        public void CallAvatar(uint avatarId){
            DataService.Get<Avatar>(avatarId).ContinueWith(x =>
            {
                var msg = Chat.Call(UserReference.Wrap(x.Result));
                if (msg != null) Chat.ShowWindow(msg);
            });
        }

        public void DisplayEmail(MessageItem item)
        {
            UserReference r = null;
            switch (item.Type)
            {
                case 1:
                    r = UserReference.Of(UserReferenceType.TSO); break; //vote
                case 2:
                    r = UserReference.Of(UserReferenceType.TSO); break; //club
                case 3:
                    r = UserReference.Of(UserReferenceType.MAXIS); break;
                case 4:
                    r = UserReference.Of(UserReferenceType.TSO); break;
                case 5:
                    r = UserReference.Of(UserReferenceType.TSO); break; //house
                case 6:
                    r = UserReference.Of(UserReferenceType.TSO); break; //roommate
            }
            if (r == null)
            {
                DataService.Get<Avatar>(item.SenderID).ContinueWith(x =>
                {
                    GameThread.NextUpdate(y =>
                    {
                        var msg = Chat.ReadLetter(UserReference.Wrap(x.Result), item);
                        if (msg != null)
                        {
                            Chat.SetEmailMessage(msg, item);
                            Chat.ShowWindow(msg);
                        }
                    });
                });
            } else
            {
                GameThread.NextUpdate(y =>
                {
                    var msg = Chat.ReadLetter(r, item);
                    if (msg != null)
                    {
                        Chat.SetEmailMessage(msg, item);
                        Chat.ShowWindow(msg);
                    }
                });
            }
        }

        public void WriteEmail(uint avatarId, string subject)
        {
            DataService.Get<Avatar>(avatarId).ContinueWith(x =>
            {
                GameThread.InUpdate(() =>
                {
                    var msg = Chat.WriteLetter(UserReference.Wrap(x.Result));
                    Chat.SetEmailMessage(msg, new MessageItem() { Subject = subject, Body = "" });
                    if (msg != null) Chat.ShowWindow(msg);
                });
            });
        }

        public void UploadLotThumbnail(bool buildMode)
        {
            if (!Screen.InLot) return;
            var lotID = JoinLotRegulator.GetCurrentLotID();
            if (lotID == 0) return;
            var bigThumb = Screen.vm.Context.World.GetLotThumb(GameFacade.GraphicsDevice, null);
            byte[] data;
            using (var stream = new MemoryStream()) {
                var tex = TextureUtils.Decimate(bigThumb, GameFacade.GraphicsDevice, 2, false);
                tex.SaveAsPng(stream, bigThumb.Width / 2, bigThumb.Height / 2);
                Terrain.OverrideLotThumb(lotID, tex);
                //tex.Dispose();
                data = stream.ToArray();
            }

            byte[] facadeData = null;

            if (buildMode && FSOEnvironment.Enable3D)
            {
                var result = new FSOFHelper(GameFacade.GraphicsDevice, Screen.vm, Screen.vm.Context.World).GenerateIngameFSOF();
                Terrain.OverrideLotFacade(lotID, result);

                using var mem = new MemoryStream();

                result.Save(mem);

                facadeData = mem.ToArray();
            }

            DataService.Get<Lot>(lotID).ContinueWith(x =>
            {
                var lot = x.Result;
                if (lot == null) return; //uh, oops!
                lot.Lot_Thumbnail = new Common.Serialization.Primitives.cTSOGenericData(data);

                if (facadeData != null)
                {
                    lot.Lot_Facade = new Common.Serialization.Primitives.cTSOGenericData(facadeData);
                    DataService.Sync(lot, ["Lot_Thumbnail", "Lot_Facade"]);
                }
                else
                {
                    DataService.Sync(lot, ["Lot_Thumbnail"]);
                }
            });
        }

        public void ShowPersonPage(UserReference user)
        {
            if(user?.Type == UserReferenceType.AVATAR)
            {
                ShowPersonPage(user.Id);
            }
        }

        public void ToggleBookmarks()
        {
            ((BookmarksController)Screen.Bookmarks.Controller).Toggle();
        }

        public void ShowBookmarks()
        {
            ((BookmarksController)Screen.Bookmarks.Controller).Show();
        }

        public void ShowPersonPage(uint avatarId){
            ((PersonPageController)Screen.PersonPage.Controller).Show(avatarId);
        }

        public void ToggleRelationshipDialog()
        {
            ((RelationshipDialogController)Screen.Relationships.Controller).Toggle(Network.MyCharacter);
        }

        public void ShowRelationshipDialog(uint avatarID)
        {
            ((RelationshipDialogController)Screen.Relationships.Controller).Show(avatarID);
        }

        public void ShowMyPersonPage(){
            ShowPersonPage(Network.MyCharacter);
        }

        public void ShowLotPage(uint lotId)
        {
            if (lotId == 0) return;
            ((LotPageController)Screen.LotPage.Controller).Show(lotId);
        }

        public void ShowNeighPage(uint neighId)
        {
            ((NeighPageController)Screen.NeighPage.Controller).Show(neighId);
        }

        public void SendVMMessage(byte[] data)
        {
            if (Network.LotClient.IsConnected)
            {
                Network.LotClient.Write(new FSOVMCommand() { Data = data });
            }
        }

        public void MoveMeOut(uint target_lot, Callback<bool> onResult)
        {
            RoommateProtocol.OnMoveoutResult = onResult;
            Network.CityClient.Write(new ChangeRoommateRequest()
            {
                Type = Server.Protocol.Electron.Model.ChangeRoommateType.KICK,
                AvatarId = Network.MyCharacter,
                LotLocation = target_lot
            });
        }

        public void GetAvatarModel(uint key, Callback<Avatar> callback)
        {
            DataService.Get<Avatar>(key).ContinueWith(x =>
            {
                if (x.Result != null)
                {
                    GameThread.InUpdate(() =>
                    {
                        callback(x.Result);
                    });
                }
            });
        }

        public void FindMyNhood(Action<uint> callback)
        {
            DataService.Get<Avatar>(Network.MyCharacter).ContinueWith(x =>
            {
                if (x.Result != null)
                {
                    x.Result.Avatar_LotGridXY = uint.MaxValue;
                    PropertyChangedEventHandler handler = null;
                    handler = (obj, evt) =>
                    {
                        if (evt.PropertyName == "Avatar_LotGridXY")
                        {
                            x.Result.PropertyChanged -= handler;
                            DataService.Get<Lot>(x.Result.Avatar_LotGridXY).ContinueWith(y =>
                            {
                                y.Result.Lot_NeighborhoodID = 0;
                                PropertyChangedEventHandler handler2 = null;
                                handler2 = (obj2, evt2) =>
                                {
                                    if (evt2.PropertyName == "Lot_NeighborhoodID")
                                    {
                                        y.Result.PropertyChanged -= handler2;
                                        GameThread.InUpdate(() =>
                                        {
                                            callback(y.Result.Lot_NeighborhoodID);
                                        });
                                    }
                                };
                                y.Result.PropertyChanged += handler2;
                                DataService.Request(Server.DataService.Model.MaskedStruct.PropertyPage_LotInfo, y.Result.Id);
                            });
                        }
                    };
                    x.Result.PropertyChanged += handler;

                    DataService.Request(Server.DataService.Model.MaskedStruct.SimPage_Main, Network.MyCharacter);
                }
            });
        }

        public void ModRequest(uint entityId, ModerationRequestType type)
        {
            Network.CityClient.Write(new ModerationRequest()
            {
                EntityId = entityId,
                Type = type
            });
        }

        public void ArchiveModRequest(uint entityId, ArchiveModerationRequestType type, int value = 0)
        {
            Network.CityClient.Write(new ArchiveModerationRequest()
            {
                EntityId = entityId,
                Type = type,
                Value = value
            });
        }

        public void HandleVMShutdown(VMCloseNetReason reason)
        {
            var state = JoinLotRegulator.CurrentState.Name;
            if (state != "Disconnected" && state != "Disconnect")
            {
                JoinLotRegulator.AsyncTransition("Disconnect");
            }
        }

        public bool IsMe(uint id)
        {
            return id == Network.MyCharacter;
        }

        public uint MyID()
        {
            return Network.MyCharacter;
        }

        public void Dispose()
        {
            JoinLotRegulator.OnTransition -= JoinLotRegulator_OnTransition;
            Screen.CleanupLastWorld();
            GameFacade.Scenes.Clear();
            Terrain.Dispose();
            Chat.Dispose();
            CityResource.Dispose();
            RoommateProtocol.Dispose();
            Screen.JoinLotProgress.FindController<JoinLotProgressController>()?.Dispose();
            ((PersonPageController)Screen.PersonPage.Controller)?.Dispose();
            ((InboxController)Screen.Inbox.Controller)?.Dispose();
        }
    }
}
