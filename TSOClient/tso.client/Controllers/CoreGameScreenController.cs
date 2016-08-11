using FSO.Client.Controllers.Panels;
using FSO.Client.Model;
using FSO.Client.Regulators;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Screens;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Enum;
using FSO.Common.Utils;
using FSO.Server.Protocol.Electron.Packets;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Model;
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
        private Network.Network Network;
        private IClientDataService DataService;
        private LotConnectionRegulator JoinLotRegulator;

        public TerrainController Terrain;

        public CoreGameScreenController(CoreGameScreen view, Network.Network network, IClientDataService dataService, IKernel kernel, LotConnectionRegulator joinLotRegulator)
        {
            this.Screen = view;
            this.Network = network;
            this.DataService = dataService;
            this.Chat = new MessagingController(this, view.MessageTray, network, dataService);
            this.JoinLotRegulator = joinLotRegulator;

            joinLotRegulator.OnTransition += JoinLotRegulator_OnTransition;

            var shard = Network.MyShard;
            Terrain = kernel.Get<TerrainController>(new ConstructorArgument("parent", this));
            view.Initialize(shard.Name, int.Parse(shard.Map), Terrain);
        }

        public void AddWindow(UIContainer window)
        {
            Screen.WindowContainer.Add(window);
        }

        private void JoinLotRegulator_OnTransition(string transition, object data)
        {
            GameThread.NextUpdate((state) =>
            {
                switch (transition)
                {
                    case "UnexpectedDisconnect":
                        //todo: what if we disconnect from lot but not city? the reverse?
                        break;
                    case "Disconnected":
                        Screen.CleanupLastWorld();
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

                        Screen.Driver.ServerMessage(msg);
                        break;
                }
            });
        }

        public void JoinLot(uint id)
        {
            JoinLotRegulator.JoinLot(id);
            //var progress = new UIJoinLotProgress();
            //UIScreen.GlobalShowDialog(progress, true);
            //UIJoinLotProgress
        }

        public void CallAvatar(uint avatarId){
            DataService.Get<Avatar>(avatarId).ContinueWith(x =>
            {
                Chat.Call(UserReference.Wrap(x.Result));
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
                TextureUtils.Decimate(bigThumb, GameFacade.GraphicsDevice, 8).SaveAsPng(stream, bigThumb.Width / 8, bigThumb.Height / 8);
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
            if(user.Type == UserReferenceType.AVATAR)
            {
                ShowPersonPage(user.Id);
            }
        }

        public void ShowPersonPage(uint avatarId){
            ((PersonPageController)Screen.PersonPage.Controller).Show(avatarId);
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
        }
    }
}
