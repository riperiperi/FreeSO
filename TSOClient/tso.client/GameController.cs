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

        public GameController(IKernel kernel)
        {
            this.Kernel = kernel;
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
            ChangeState<PersonSelection, PersonSelectionController>((view, controller) =>
            {
            });
        }

        public void ShowPersonCreation(ShardStatusItem selectedCity)
        {
            var screen = Kernel.Get<PersonSelectionEdit>();
            //screen.SelectedCity = selectedCity;
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
        }


        public void ConnectToCity(string cityName, uint avatarId){
            ChangeState<TransitionScreen, ConnectCityController>((view, controller) =>
            {
                controller.Connect(cityName, avatarId, () => { GotoCity(controller.AvatarData); }, new Common.Utils.Callback(Disconnect));
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

        public void GotoCity(LoadAvatarByIDResponse dbAvatar){
            ChangeState<CoreGameScreen, CoreGameScreenController>((view, controller) =>{
                view.VisualBudget = dbAvatar.Cash;
            });
        }

        public void Disconnect()
        {
            Disconnect(false);
        }

        public void Disconnect(bool toLogin){
            ChangeState<TransitionScreen, DisconnectController>((view, controller) =>
            {
                controller.Disconnect(() => HandleDisconnect(toLogin));
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
            GameThread.NextUpdate(x =>
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
                var controller = view.BindController<TController>();
                GameFacade.Screens.RemoveCurrent();
                GameFacade.Screens.AddScreen(view);

                CurrentController = controller;
                CurrentView = view;

                onCreated((TView)view, controller);
            });
        }

        public void ShowCredits()
        {
            var screen = Kernel.Get<Credits>();
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
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
