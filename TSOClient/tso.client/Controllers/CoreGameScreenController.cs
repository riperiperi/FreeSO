using FSO.Client.Controllers.Panels;
using FSO.Client.Model;
using FSO.Client.Regulators;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Screens;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Enum;
using FSO.Common.Utils;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Model;
using Microsoft.Xna.Framework;
using Ninject;
using Ninject.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public TerrainController Terrain;

        public CoreGameScreenController(CoreGameScreen view, Network.Network network, IClientDataService dataService, IKernel kernel, LotConnectionRegulator joinLotRegulator)
        {
            this.Screen = view;
            this.Network = network;
            this.DataService = dataService;
            this.Chat = new MessagingController(this, view.MessageTray, network, dataService);
            this.JoinLotRegulator = joinLotRegulator;
            this.RoommateProtocol = new RoommateRequestController(this, network, dataService);

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
                        //initialize a lot
                        break;
                    case "LotCommandStream":
                        //forward the command to the VM
                        //doesn't really need to be next update... but we don't want to catch the VM in a half-init state.
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

        public void UploadLotThumbnail()
        {
            if (!Screen.InLot) return;
            var lotID = JoinLotRegulator.GetCurrentLotID();
            if (lotID == 0) return;
            var bigThumb = Screen.vm.Context.World.GetLotThumb(GameFacade.GraphicsDevice);
            byte[] data;
            using (var stream = new MemoryStream()) {
                var tex = TextureUtils.Decimate(bigThumb, GameFacade.GraphicsDevice, 8);
                tex.SaveAsPng(stream, bigThumb.Width / 8, bigThumb.Height / 8);
                tex.Dispose();
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
            ((LotPageController)Screen.LotPage.Controller).Show(lotId);
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
        }
    }
}
