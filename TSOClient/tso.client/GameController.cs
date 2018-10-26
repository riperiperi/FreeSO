/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Screens;
using FSO.Client.Network;
using FSO.Client.UI.Framework;
using FSO.Client.GameContent;
using Ninject;
using FSO.Server.Protocol.CitySelector;
using FSO.Client.Controllers;
using FSO.Common.Utils;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Panels;
using FSO.Client.UI;
using FSO.Common.DatabaseService.Model;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Client.Utils;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Client.Controllers.Panels;
using System.Collections.Immutable;
using FSO.Common.DataService.Model;
using FSO.Common.Serialization.Primitives;
using FSO.UI.Model;

namespace FSO.Client
{
    /// <summary>
    /// Handles the game flow between various game modes, e.g. login => city view
    /// </summary>
    public class GameController
    {
        private object CurrentController;
        private UIScreen CurrentView;
        private IKernel Kernel;
        private static bool DummyLinker;

        public GameController(IKernel kernel)
        {
            this.Kernel = kernel;
            if (DummyLinker) LinkEveryController();
        }

        public void DebugShowTypeFaceScreen()
        {
            var screen = new DebugTypeFaceScreen();

            /** Remove preload screen **/
            GameFacade.Screens.AddScreen(screen);
        }

        /// <summary>
        /// Start the preloading process
        /// </summary>
        public void StartLoading()
        {
            ChangeState<LoadingScreen, LoadingScreenController>((view, controller) =>
            {
                controller.Preload();
            });
        }

        /// <summary>
        /// Show the login screen
        /// </summary>
        public void ShowLogin()
        {
            ChangeState<LoginScreen, LoginController>((view, controller) =>
            {

                DiscordRpcEngine.SendFSOPresence("In Main Menu");
            });
            /*
            var screen = Kernel.Get<LoginScreen>();
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
            */
        }

        /// <summary>
        /// Go to the person selection page
        /// </summary>
        public void ShowPersonSelection()
        {
            if (GlobalSettings.Default.CompatState == -1)
            {
                GlobalSettings.Default.CompatState = 0;
                GlobalSettings.Default.Save();
            }
            ChangeState<PersonSelection, PersonSelectionController>((view, controller) =>
            {

                DiscordRpcEngine.SendFSOPresence("In Select A Sim");
            });
        }

        public void ShowPersonCreation(ShardStatusItem selectedCity)
        {
            var screen = Kernel.Get<PersonSelectionEdit>();
            //screen.SelectedCity = selectedCity;
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
        }

        public void LinkEveryController()
        {
            //obvious question - "why":
            //this is for mono ahead of time compilation. By referencing everything here,
            //we make sure these classes and generic variants are AOT compiled

            var load = new LoadingScreen();
            var loadc = new LoadingScreenController(null, null, null);

            
            var screen = new PersonSelectionEdit();
            var t = new TerrainController(null, null, null, null, null);
            var n = new Network.Network(null, null, null, null);
            var v = new Credits();
            var cd = new Common.DataService.ClientDataService(null, null, null);
            var c = new CoreGameScreen();
            var cc = new CoreGameScreenController(null, null, null, null, null);
            var s = new SandboxGameScreen();
            var ps = new PersonSelection(null, null);
            var psc = new PersonSelectionController(null, null, null, null);
            var cl1 = new Server.Clients.AuthClient("");
            var cl2 = new Server.Clients.CityClient("");
            var cl3 = new Server.Clients.ApiClient("");
            var ls = new LoginScreen(null);
            var lc = new LoginController(null, null);
            var lr = new Regulators.LoginRegulator(null, null, null);

            var seled = new PersonSelectionEditController(null, null);
            var casr = new Regulators.CreateASimRegulator(null);
            var purch = new Regulators.PurchaseLotRegulator(null);
            var conn = new Regulators.LotConnectionRegulator(null, null, null);
            var t2 = new Regulators.CityConnectionRegulator(null, null, null, null, Kernel, null);
            var regu = new Regulators.RegulatorsModule();

            var prov = new CacheProvider();
            var clip = new AuthClientProvider(null);
            var citp = new CityClientProvider(null);
            var ar = new Server.Clients.AriesClient(null);
            var tso = new cTSOSerializerProvider(null);
            var ser = new Server.Protocol.Voltron.DataService.cTSOSerializer(null);
            var mods = new ModelSerializerProvider(null);
            var dbs = new Common.DatabaseService.DatabaseService(null);
            var cds = new Common.DataService.ClientDataService(null, null, null);

            var arp = new Server.Protocol.Aries.AriesProtocolDecoder(null);
            var are = new Server.Protocol.Aries.AriesProtocolEncoder(null);
            var serc = new Common.Serialization.SerializationContext(null, null);

            var packets = new object[]
            {
                new ClientOnlinePDU(),
                new HostOnlinePDU(),
                new SetIgnoreListPDU(),
                new SetIgnoreListResponsePDU(),
                new SetInvinciblePDU(),
                new RSGZWrapperPDU(),
                new TransmitCreateAvatarNotificationPDU(),
                new DataServiceWrapperPDU(),
                new DBRequestWrapperPDU(),
                new OccupantArrivedPDU(),
                new ClientByePDU(),
                new ServerByePDU(),
                new FindPlayerPDU(),
                new FindPlayerResponsePDU(),
                new ChatMsgPDU(),
                new AnnouncementMsgPDU(),

                new MessagingWindowController(null, null, null),
                new Controllers.Panels.SecureTradeController(null,null),
                new GizmoSearchController(null, null, null),
                new GizmoTop100Controller(null, null, null, null, null),
                new LotAdmitController(null, null, null),
                new GizmoController(null, null, null),
                new PersonPageController(null,null,null),
                new LotPageController(null, null),
                new BookmarksController(null, null, null),
                new RelationshipDialogController(null, null, null, null),
                new InboxController(null, null, null, null),
                new JoinLotProgressController(null, null),
                new DisconnectController(null, null, null, null, null),

                ImmutableList.Create<uint>(),
                ImmutableList.Create<JobLevel>(),
                ImmutableList.Create<Relationship>(),
                ImmutableList.Create<Bookmark>(),
                ImmutableList.Create<bool>(),
                
                new cTSOGenericData(),
            };
        }


        public void ConnectToCity(string cityName, uint avatarId, uint? lotId)
        {
            ChangeState<TransitionScreen, ConnectCityController>((view, controller) =>
            {
                controller.Connect(cityName, avatarId, () => { GotoCity(controller.AvatarData, lotId); }, new Common.Utils.Callback(Disconnect));
            });
        }

        public void RetireAvatar(string cityName, uint avatarId)
        {
            ChangeState<TransitionScreen, ConnectCityController>((view, controller) =>
            {
                controller.Connect(cityName, avatarId, () => {
                    var network = Kernel.Get<Network.Network>();
                    network.CityClient.Write(new AvatarRetireRequest());
                    GameThread.SetTimeout(() =>
                    {
                        Disconnect();
                    }, 1000);
                }, new Common.Utils.Callback(Disconnect));
            });
        }

        public void ConnectToCAS(string cityName)
        {
            /**
             * Steps:
             *  1) Show transition screen and open a connectino to the server
             *  2) If connection succeeds, go to CAS
             *  3) If connection fails, go back to SAS
             */
            ChangeState<TransitionScreen, ConnectCASController>((view, controller) =>
            {
                controller.Connect(cityName, new Common.Utils.Callback(GotoCAS), new Common.Utils.Callback(Disconnect));
            });
        }

        public void GotoCAS(){
            ChangeState<PersonSelectionEdit, PersonSelectionEditController>((view, controller) => {
                
            });
        }

        public void GotoCity(LoadAvatarByIDResponse dbAvatar, uint? lotId)
        {
            ChangeState<CoreGameScreen, CoreGameScreenController>((view, controller) =>{
                view.VisualBudget = dbAvatar.Cash;

                if(dbAvatar.Bonus != null && dbAvatar.Bonus.Count > 0)
                {
                    UIScreen.ShowDialog(new UIBonusDialog(dbAvatar.Bonus), true);
                }

                if (lotId.HasValue){
                    controller.JoinLot(lotId.Value);
                }
            });
        }

        public void Disconnect()
        {
            Disconnect(false);
        }

        public void Disconnect(bool toLogin){
            ChangeState<TransitionScreen, DisconnectController>((view, controller) =>
            {
                controller.Disconnect((forceLogin) => HandleDisconnect(forceLogin || toLogin), toLogin);
            });
        }


        private void HandleDisconnect(bool forceLogin){
            //Depending on how long is left on the session take user
            //to SAS or login screen
            if (forceLogin)
                ShowLogin();
            else 
                ShowPersonSelection();
        }

        public void FatalNetworkError(int code)
        {
            var title = GameFacade.Strings.GetString("222", "1");
            var desc = GameFacade.Strings.GetString("222", "2").Replace("%d", code.ToString());
            FatalError(title, desc);
        }

        /// <summary>
        /// When something goes very wrong, e.g. the server connection drops
        /// This method should be used. The game controller will tell the user
        /// and then work to clean everything up
        /// </summary>
        public void FatalError(string errorTitle, string errorMessage){
            var alert = UIScreen.GlobalShowAlert(new UI.Controls.UIAlertOptions {
                Message = errorMessage,
                Title = errorTitle,
                Buttons = UIAlertButton.Ok(x => Disconnect())
            }, true);
        }

        private UIDebugMenu _DebugMenu;
        private bool _DebugVisible = false;
        private DialogReference _DebugDialog;

        public void ToggleDebugMenu()
        {
            if(_DebugMenu == null){
                _DebugMenu = new UIDebugMenu();
                _DebugDialog = new UI.DialogReference()
                {
                    Dialog = _DebugMenu,
                    Modal = true
                };
            }

            if (_DebugVisible){
                _DebugVisible = false;
                GameFacade.Screens.AddDialog(_DebugDialog);
            }else{
                _DebugVisible = true;
                GameFacade.Screens.RemoveDialog(_DebugDialog);
            }
        }

        private void ChangeState<TView, TController>(Callback<TView, TController> onCreated) where TView : UIScreen
        {
            Binding.DisposeAll();
            GameThread.InUpdate(() =>
            {
                GameFacade.Cursor.SetCursor(Common.Rendering.Framework.CursorType.Normal); //reset cursor
                if (CurrentController != null)
                {
                    if (CurrentController is IDisposable)
                    {
                        ((IDisposable)CurrentController).Dispose();
                    }
                }

                var view = (UIScreen)Kernel.Get<TView>();
                var controller = ControllerUtils.BindController<TController>(view);
                GameFacade.Screens.RemoveCurrent();
                GameFacade.Screens.AddScreen(view);

                CurrentController = controller;
                CurrentView = view;

                onCreated((TView)view, controller);
            });
        }

        public void EnterSandboxMode(string lotName, bool external)
        {
            var screen = new SandboxGameScreen();
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
            screen.Initialize(lotName, external);
            DiscordRpcEngine.SendFSOPresence("Playing Sandbox Mode");
        }

        public void ShowCredits()
        {
            var screen = Kernel.Get<Credits>();
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
            DiscordRpcEngine.SendFSOPresence("Viewing Credits");
        }

        public void StartDebugTools()
        {
			/*
            if (GameFacade.DebugWindow != null)
            {
                if (GameFacade.DebugWindow.Visible)
                {
                    GameFacade.DebugWindow.Hide();
                }
                else
                {
                    GameFacade.DebugWindow.Show();
                }
                return;
            }

            var debugWindow = new FSO.Client.Debug.TSOClientTools();
            GameFacade.DebugWindow = debugWindow;

            debugWindow.Show();
			*/
            //debugWindow.PositionAroundGame(GameFacade.Game.Window);
        }
    }

}
