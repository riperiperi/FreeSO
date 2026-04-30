using FSO.Client.Controllers.Panels;
using FSO.Client.Model;
using FSO.Client.Regulators;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Enum;
using FSO.Common.Serialization.Primitives;
using FSO.Common.Utils;
using FSO.Files.Formats.tsodata;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Model;
using Microsoft.Xna.Framework;
using NLog;
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
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
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

        public TerrainController Terrain;
        public NeighborhoodActionController NeighborhoodProtocol;
        public BulletinActionController BulletinProtocol;

        public CoreGameScreenController(CoreGameScreen view, Network.Network network, IClientDataService dataService, IKernel kernel, LotConnectionRegulator joinLotRegulator)
        {
            this.Screen = view;
            this.Network = network;
            this.DataService = dataService;
            this.Chat = new MessagingController(this, view.MessageTray, network, dataService);
            this.JoinLotRegulator = joinLotRegulator;
            UI.Panels.UIChatPanel.GlobalChatSend = (msg) => network.CityClient.Write(msg);
            this.RoommateProtocol = new RoommateRequestController(this, network, dataService);
            this.NeighborhoodProtocol = kernel.Get<NeighborhoodActionController>();
            this.BulletinProtocol = kernel.Get<BulletinActionController>();

            joinLotRegulator.OnTransition += JoinLotRegulator_OnTransition;

            var shard = Network.MyShard;
            Terrain = kernel.Get<TerrainController>(new ConstructorArgument("parent", this));
            view.Initialize(shard.Name, int.Parse(shard.Map), Terrain);
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
                            GameThread.SetTimeout(() => {
                                if (ReconnectLotID != 0) JoinLot(ReconnectLotID);
                            }, 100);
                        }
                        //destroy the currently active lot (if possible)
                        break;
                    case "PartiallyConnected":
                        Screen.InitializeLot();
                        Screen.vm.MyUID = Network.MyCharacter;
                        Screen.vm.OnAvatarHeadOutfitChanged = vmAva => UploadAvatarThumbnail(vmAva);
                        Screen.vm.OnAvatarReady = vmAva => UploadAvatarThumbnail(vmAva);
                        //initialize a lot
                        break;
                    case "LotCommandStream":
                        //forward the command to the VM
                        //doesn't really need to be next update... but we don't want to catch the VM in a half-init state.
                        if (data == null) break;
                        VMNetMessage msg = null;
                        if (data is FSOVMTickBroadcast)
                            msg = new VMNetMessage(VMNetMessageType.BroadcastTick, ((FSOVMTickBroadcast)data).Data);
                        else
                            msg = new VMNetMessage(VMNetMessageType.Direct, ((FSOVMDirectToClient)data).Data);

                        Screen.Driver?.ServerMessage(msg);
                        break;
                }
            });
        }

        public void JoinLot(uint id)
        {
            var lot = JoinLotRegulator.GetCurrentLotID();
            if (lot == 0)
            {
                JoinLotRegulator.JoinLot(id);
                ReconnectLotID = 0;
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

        public void SwitchLot(uint id)
        {
            if (JoinLotRegulator.GetCurrentLotID() == 0)
            {
                JoinLotRegulator.JoinLot(id);
                ReconnectLotID = 0;
            }
            else
            {
                //force a switch to the target lot
                ReconnectLotID = id;
                Screen.InitiateLotSwitch();
            }
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

        public void UploadLotThumbnail()
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
            DataService.Get<Lot>(lotID).ContinueWith(x =>
            {
                var lot = x.Result;
                if (lot == null) return; //uh, oops!
                lot.Lot_Thumbnail = new Common.Serialization.Primitives.cTSOGenericData(data);
                DataService.Sync(lot, new string[] { "Lot_Thumbnail" });
            });
        }

        private DateTime _LastAvatarThumbUpload = DateTime.MinValue;

        public void UploadAvatarThumbnail(VMAvatar vmAva)
        {
            // Debounce before scheduling — avoids queuing many callbacks during lot load.
            var now = DateTime.UtcNow;
            if ((now - _LastAvatarThumbUpload).TotalSeconds < 5) return;
            _LastAvatarThumbUpload = now;

            LOG.Info("[AvatarThumb] UploadAvatarThumbnail called for avatar persistID={0} name={1}", vmAva.PersistID, vmAva.Name);

            // OpenGL calls (GetAvatarThumb, GetIcon, GraphicsDevice) must run on the
            // main/render thread. This method is invoked from VM.LoadAsync (thread pool)
            // so we marshal the work here to avoid a SIGSEGV in libGLdispatch.
            GameThread.NextUpdate(_ =>
            {
                try
                {
                    var avatarComp = VMEntity.UseWorld ? vmAva.WorldUI as FSO.LotView.Components.AvatarComponent : null;
                    LOG.Info("[AvatarThumb] NextUpdate: UseWorld={0} avatarComp={1}", VMEntity.UseWorld, avatarComp != null ? "set" : "null");

                    // Only upload a client render when we can produce a full-body portrait (3D mode).
                    // In 2D mode GetAvatarThumb returns null; GetIcon only has the small head sprite
                    // which makes a poor body.png and a worse head.png after server-side cropping.
                    // The server's GenerateAvatarThumbnail already handles the 2D case from content.
                    var ico = avatarComp != null
                        ? Screen.vm.Context.World.GetAvatarThumb(avatarComp, GameFacade.GraphicsDevice)
                        : null;
                    LOG.Info("[AvatarThumb] GetAvatarThumb result: {0}", ico != null ? $"{ico.Width}x{ico.Height}" : "null (2D client — skipping upload)");

                    if (ico == null) { LOG.Info("[AvatarThumb] No 3D portrait available — server-side content thumbnail will be used"); return; }

                    byte[] data;
                    using (var stream = new MemoryStream())
                    {
                        ico.SaveAsPng(stream, ico.Width, ico.Height);
                        data = stream.ToArray();
                    }
                    LOG.Info("[AvatarThumb] PNG encoded: {0} bytes", data.Length);

                    DataService.Get<Avatar>(Network.MyCharacter).ContinueWith(x =>
                    {
                        try
                        {
                            var avatar = x.Result;
                            if (avatar == null) { LOG.Warn("[AvatarThumb] DataService.Get returned null avatar"); return; }
                            LOG.Info("[AvatarThumb] Syncing {0} bytes to city server for avatarId={1}", data.Length, Network.MyCharacter);
                            avatar.Avatar_Thumbnail = new cTSOGenericData(data);
                            DataService.Sync(avatar, new string[] { "Avatar_Thumbnail" });
                            LOG.Info("[AvatarThumb] DataService.Sync called successfully");
                        }
                        catch (Exception ex)
                        {
                            LOG.Error(ex, "[AvatarThumb] Exception in DataService.Sync callback");
                        }
                    });
                }
                catch (Exception ex)
                {
                    LOG.Error(ex, "[AvatarThumb] Exception in NextUpdate callback");
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

        public void HandleVMShutdown(VMCloseNetReason reason)
        {
            JoinLotRegulator.AsyncTransition("Disconnect");
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
            RoommateProtocol.Dispose();
            Screen.JoinLotProgress.FindController<JoinLotProgressController>()?.Dispose();
            ((PersonPageController)Screen.PersonPage.Controller)?.Dispose();
            ((InboxController)Screen.Inbox.Controller)?.Dispose();
        }
    }
}
