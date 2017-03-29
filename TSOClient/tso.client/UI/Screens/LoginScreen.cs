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
using FSO.Client.UI.Model;
using FSO.HIT;
using FSO.Files.Utils;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common;
using FSO.Server.Clients;
using Ninject;
using System.Net.NetworkInformation;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FSO.Client.UI.Screens
{
    public class LoginScreen : GameScreen, IDisposable
    {
        private UISetupBackground Background;
        public UILoginDialog LoginDialog;
        public UILoginProgress LoginProgress;

        private LoginRegulator Regulator;

        public LoginScreen(LoginRegulator regulator)
        {
            try
            {
                if (File.Exists("update2.exe"))
                {
                    File.Delete("update.exe");
                    File.Move("update2.exe", "update.exe");
                }
            } catch (Exception) { 
                //maybe signal to user that the updater update failed
            }

            this.Regulator = regulator;
            regulator.Logout();

            HITVM.Get().PlaySoundEvent(UIMusic.None);
            GlobalSettings.Default.Save();

            Background = new UISetupBackground();

            /** Client version **/
            var lbl = new UILabel();
            lbl.Caption = "Version " + GlobalSettings.Default.ClientVersion;
            lbl.X = 20;
            lbl.Y = 558;
            Background.BackgroundCtnr.Add(lbl);
            this.Add(Background);

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
                Caption = "Sandbox Mode",
                Y = 10,
                Width = 125,
                X = 10
            };
            this.Add(gameplayButton);
            gameplayButton.OnButtonClick += new ButtonClickDelegate(gameplayButton_OnButtonClick);
            
            Regulator.OnError += AuthRegulator_OnError;
            Regulator.OnTransition += AuthRegulator_OnTransition;
        }

        public override void GameResized()
        {
            base.GameResized();
            LoginProgress.X = (ScreenWidth - (LoginProgress.Width + 20));
            LoginProgress.Y = (ScreenHeight - (LoginProgress.Height + 20));

            LoginDialog.X = (ScreenWidth - LoginDialog.Width) / 2;
            LoginDialog.Y = (ScreenHeight - LoginDialog.Height) / 2;
        }

        public void Dispose()
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
                Version = "2.5",
                ClientID = GetUID()
            });
        }

        private string GetUID()
        {
            var id =
            (
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.OperationalStatus == OperationalStatus.Up
                select nic.GetPhysicalAddress().ToString()
            ).FirstOrDefault();

            return GetHashString(id ?? "");
        }

        public static string GetHashString(string input)
        {
            HashAlgorithm algorithm = SHA1.Create();
            StringBuilder sb = new StringBuilder();
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            foreach (byte b in hash)
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        private void SetProgress(int stage)
        {
            var auth = GameFacade.Kernel.Get<AuthClient>();
            LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", (stage + 3).ToString())
                .Replace("EA.COM", auth.BaseUrl.Substring(7).TrimEnd('/'));
            LoginProgress.Progress = 25 * (stage - 1);
        }

        void gameplayButton_OnButtonClick(UIElement button)
        {
            UIAlertOptions Options = new UIAlertOptions();
            Options.Message = "Sandbox Mode will be implemented in a future version.";
            Options.Title = GameFacade.Strings.GetString("210", "40");
            UI.Framework.UIScreen.GlobalShowAlert(Options, true);
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
        }
    }
}
