/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.IO;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Panels;
using FSO.Client.GameContent;
using FSO.Server.Protocol.Authorization;
using FSO.Files;
using FSO.Client.Utils;
using FSO.Client.Regulators;

namespace FSO.Client.UI.Screens
{
    public class LoginScreen : GameScreen
    {
        private UIContainer BackgroundCtnr;
        private UIImage Background;
        private UILoginDialog LoginDialog;
        private UILoginProgress LoginProgress;

        private LoginRegulator Regulator;

        public LoginScreen(LoginRegulator regulator)
        {
            this.Regulator = regulator;


            PlayBackgroundMusic(new string[] { "none" });

            /**
             * Scale the whole screen to 1024
             */
            BackgroundCtnr = new UIContainer();
            BackgroundCtnr.ScaleX = BackgroundCtnr.ScaleY = ScreenWidth / 800.0f;

            /** Background image **/
            Background = new UIImage(GetTexture((ulong)FileIDs.UIFileIDs.setup));
            Background.ID = "Background";
            BackgroundCtnr.Add(Background);

            /** Client version **/
            var lbl = new UILabel();
            lbl.Caption = "Version " + GlobalSettings.Default.ClientVersion;
            lbl.X = 20;
            lbl.Y = 558;
            BackgroundCtnr.Add(lbl);
            this.Add(BackgroundCtnr);

            /** Progress bar **/
            LoginProgress = new UILoginProgress();
            LoginProgress.X = (ScreenWidth - (LoginProgress.Width + 20));
            LoginProgress.Y = (ScreenHeight - (LoginProgress.Height + 20));
            LoginProgress.Opacity = 0.9f;
            this.Add(LoginProgress);

            /** Login dialog **/
            LoginDialog = new UILoginDialog(this);
            LoginDialog.Opacity = 0.9f;
            //Center
            LoginDialog.X = (ScreenWidth - LoginDialog.Width) / 2;
            LoginDialog.Y = (ScreenHeight - LoginDialog.Height) / 2;
            this.Add(LoginDialog);

            bool usernamePopulated = false;

            var loginIniFile = GameFacade.GameFilePath("login.ini");
            if (File.Exists(loginIniFile)){
                var iniFile = IniFile.Read(loginIniFile);
                if (iniFile.ContainsKey("LastSession")){
                    LoginDialog.Username = iniFile["LastSession"]["UserName"];
                    usernamePopulated = true;
                }
            }

            if (usernamePopulated)
            {
                LoginDialog.FocusPassword();
            }
            else
            {
                LoginDialog.FocusUsername();
            }

            var gameplayButton = new UIButton()
            {
                Caption = "Simantics & Lot Debug",
                Y = 10,
                Width = 200,
                X = 10
            };
            this.Add(gameplayButton);
            gameplayButton.OnButtonClick += new ButtonClickDelegate(gameplayButton_OnButtonClick);
            
            Regulator.OnError += AuthRegulator_OnError;
            Regulator.OnTransition += AuthRegulator_OnTransition;
        }

        ~LoginScreen()
        {
            Regulator.OnError -= AuthRegulator_OnError;
            Regulator.OnTransition -= AuthRegulator_OnTransition;
        }

        private void AuthRegulator_OnTransition(string state, object data)
        {
            switch (state)
            {
                case "NotLoggedIn":
                    SetProgress(1);
                    break;
                case "AuthLogin":
                    SetProgress(2);
                    break;
                case "InitialConnect":
                    SetProgress(3);
                    break;
                case "AvatarData":
                    SetProgress(4);
                    break;
                case "ShardStatus":
                    SetProgress(5);
                    break;
            }
        }

        private void AuthRegulator_OnError(object error)
        {
            if (error is Exception)
            {
                error = ErrorMessage.FromLiteral(GameFacade.Strings.GetString("210", "17"));
            }

            if (error is ErrorMessage)
            {
                ErrorMessage errorMsg = (ErrorMessage)error;

                /** Error message intended for the user **/
                UIAlertOptions Options = new UIAlertOptions();
                Options.Message = errorMsg.Message;
                Options.Title = errorMsg.Title;
                Options.Buttons = errorMsg.Buttons;
                GlobalShowAlert(Options, true);
            }
        }

        /// <summary>
        /// Called by login button click in UILoginDialog
        /// </summary>
        public void Login()
        {
            if(LoginDialog.Username.Length == 0 || LoginDialog.Password.Length == 0){
                return;
            }

            Regulator.Login(new AuthRequest
            {
                Username = LoginDialog.Username,
                Password = LoginDialog.Password,
                ServiceID = "2147",
                Version = "2.5"
            });
        }

        private void SetProgress(int stage)
        {
            LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", (stage + 3).ToString());
            LoginProgress.Progress = 25 * (stage - 1);
        }

        void gameplayButton_OnButtonClick(UIElement button)
        {
            UIAlertOptions Options = new UIAlertOptions();
            Options.Message = GameFacade.Strings.GetString("210", "36 301");
            Options.Title = GameFacade.Strings.GetString("210", "40");
            UI.Framework.UIScreen.GlobalShowAlert(Options, true);
        }
    }
}
