/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Afr0. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Panels;
using TSOClient.Events;
using TSOClient.Network;
using TSOClient.Network.Events;
using GonzoNet;
using ProtocolAbstractionLibraryD;

namespace TSOClient.Code.UI.Screens
{
    public class CityTransitionScreen : GameScreen
    {
        private UIContainer m_BackgroundCtnr;
        private UIImage m_Background;
        private UILoginProgress m_LoginProgress;
        private CityInfo m_SelectedCity;
        private bool m_CharacterCreated = false;
        private bool m_Dead = false;

        /// <summary>
        /// Creates a new CityTransitionScreen.
        /// </summary>
        /// <param name="SelectedCity">The city being transitioned to.</param>
        /// <param name="CharacterCreated">If transitioning from CreateASim, this should be true.
        /// A CharacterCreateCity packet will be sent to the CityServer. Otherwise, this should be false.
        /// A CityToken packet will be sent to the CityServer.</param>
        public CityTransitionScreen(CityInfo SelectedCity, bool CharacterCreated)
        {
            m_SelectedCity = SelectedCity;
            m_CharacterCreated = CharacterCreated;

            /**
             * Scale the whole screen to 1024
             */
            m_BackgroundCtnr = new UIContainer();
            m_BackgroundCtnr.ScaleX = m_BackgroundCtnr.ScaleY = ScreenWidth / 800.0f;

            /** Background image **/
            m_Background = new UIImage(GetTexture((ulong)FileIDs.UIFileIDs.setup));
            m_Background.ID = "Background";
            m_BackgroundCtnr.Add(m_Background);

            var lbl = new UILabel();
            lbl.Caption = "Version " + GlobalSettings.Default.ClientVersion;
            lbl.X = 20;
            lbl.Y = 558;
            m_BackgroundCtnr.Add(lbl);
            this.Add(m_BackgroundCtnr);

            m_LoginProgress = new UILoginProgress();
            m_LoginProgress.X = (ScreenWidth - (m_LoginProgress.Width + 20));
            m_LoginProgress.Y = (ScreenHeight - (m_LoginProgress.Height + 20));
            m_LoginProgress.Opacity = 0.9f;
            this.Add(m_LoginProgress);

            NetworkFacade.Controller.OnNetworkError += new NetworkErrorDelegate(Controller_OnNetworkError);

            LoginArgsContainer LoginArgs = new LoginArgsContainer();
            LoginArgs.Username = NetworkFacade.Client.ClientEncryptor.Username;
            LoginArgs.Password = Convert.ToBase64String(PlayerAccount.Hash);
            LoginArgs.Enc = NetworkFacade.Client.ClientEncryptor;

            NetworkFacade.Client = new NetworkClient(SelectedCity.IP, SelectedCity.Port, 
                GonzoNet.Encryption.EncryptionMode.AESCrypto);
            //This might not fix decryption of cityserver's packets, but it shouldn't make things worse...
            NetworkFacade.Client.ClientEncryptor = LoginArgs.Enc;
            //THIS IS IMPORTANT - THIS NEEDS TO BE COPIED AFTER IT HAS BEEN RECREATED FOR
            //THE RECONNECTION TO WORK!
            LoginArgs.Client = NetworkFacade.Client;
            NetworkFacade.Client.OnConnected += new OnConnectedDelegate(Client_OnConnected);
            NetworkFacade.Controller.Reconnect(ref NetworkFacade.Client, SelectedCity, LoginArgs);
            
            NetworkFacade.Controller.OnCharacterCreationStatus += new OnCharacterCreationStatusDelegate(Controller_OnCharacterCreationStatus);
            NetworkFacade.Controller.OnCityTransferProgress += new OnCityTransferProgressDelegate(Controller_OnCityTransfer);
            NetworkFacade.Controller.OnLoginNotifyCity += new OnLoginNotifyCityDelegate(Controller_OnLoginNotifyCity);
            NetworkFacade.Controller.OnLoginSuccessCity += new OnLoginSuccessCityDelegate(Controller_OnLoginSuccessCity);
            NetworkFacade.Controller.OnLoginFailureCity += new OnLoginFailureCityDelegate(Controller_OnLoginFailureCity);
        }

        ~CityTransitionScreen()
        {
            NetworkFacade.Controller.OnNetworkError -= new NetworkErrorDelegate(Controller_OnNetworkError);
            NetworkFacade.Controller.OnCharacterCreationStatus -= new OnCharacterCreationStatusDelegate(Controller_OnCharacterCreationStatus);
            NetworkFacade.Controller.OnCityTransferProgress -= new OnCityTransferProgressDelegate(Controller_OnCityTransfer);
        }

        private void Client_OnConnected(LoginArgsContainer LoginArgs)
        {
            TSOClient.Network.Events.ProgressEvent Progress = 
                new TSOClient.Network.Events.ProgressEvent(TSOClient.Events.EventCodes.PROGRESS_UPDATE);
            Progress.Done = 1;
            Progress.Total = 3;

            UIPacketSenders.SendLoginRequestCity(LoginArgs);
            OnTransitionProgress(Progress);
        }

        private void Controller_OnLoginSuccessCity()
        {
            TSOClient.Network.Events.ProgressEvent Progress = 
                new TSOClient.Network.Events.ProgressEvent(TSOClient.Events.EventCodes.PROGRESS_UPDATE);
            Progress.Done = 2;
            Progress.Total = 3;

            if (m_CharacterCreated)
            {
                UIPacketSenders.SendCharacterCreateCity(NetworkFacade.Client, PlayerAccount.CurrentlyActiveSim);
                OnTransitionProgress(Progress);
            }
            else
            {
                UIPacketSenders.SendCityToken(NetworkFacade.Client);
                OnTransitionProgress(Progress);
            }
        }

        /// <summary>
        /// Client sent invalid challenge response (last stage of authentication).
        /// Should never occur.
        /// </summary>
        private void Controller_OnLoginFailureCity()
        {
            Controller_OnNetworkError(new SocketException());
        }

        private void Controller_OnLoginNotifyCity()
        {
            TSOClient.Network.Events.ProgressEvent Progress = 
                new TSOClient.Network.Events.ProgressEvent(TSOClient.Events.EventCodes.PROGRESS_UPDATE);
            Progress.Done = 2;
            Progress.Total = 3;
            OnTransitionProgress(Progress);
        }

        /// <summary>
        /// Received a status update from the CityServer.
        /// Occurs after sending the token.
        /// </summary>
        /// <param name="e">Status of transfer.</param>
        private void Controller_OnCityTransfer(CityTransferStatus e)
        {
            switch (e)
            {
                case CityTransferStatus.Success:
                    if (m_Dead) return; //don't create multiple please
                    TSOClient.Network.Events.ProgressEvent Progress = new ProgressEvent(EventCodes.PROGRESS_UPDATE);
                    Progress.Done = 2;
                    Progress.Total = 3;
                    
                    //Commenting out the below line makes the city show up when creating a new Sim... o_O
                    //OnTransitionProgress(Progress);
                    GameFacade.Controller.ShowCity();
                    m_Dead = true;
                    break;
                case CityTransferStatus.GeneralError:
                    Controller_OnNetworkError(new SocketException());
                    break;
            }
        }

        /// <summary>
        /// Received a status update from the CityServer.
        /// Occurs after sending CharacterCreation packet.
        /// </summary>
        /// <param name="e">Status of character creation.</param>
        private void Controller_OnCharacterCreationStatus(CharacterCreationStatus CCStatus)
        {
            switch (CCStatus)
            {
                case CharacterCreationStatus.Success:
                    if (m_Dead) return;
                    TSOClient.Network.Events.ProgressEvent Progress = new ProgressEvent(EventCodes.PROGRESS_UPDATE);
                    Progress.Done = 1;
                    Progress.Total = 3;

                    //Lord have mercy on the soul who figures out why commenting out the below line
                    //causes the city to show...
                    //OnTransitionProgress(Progress);
                    GameFacade.Controller.ShowCity();
                    m_Dead = true;
                    break;
                case CharacterCreationStatus.GeneralError:
                    Controller_OnNetworkError(new SocketException());
                    break;
            }
        }

        /// <summary>
        /// Another stage in the CityServer transition progress was done.
        /// </summary>
        /// <param name="e"></param>
        private void OnTransitionProgress(ProgressEvent e)
        {
            var stage = e.Done;

            m_LoginProgress.ProgressCaption = GameFacade.Strings.GetString("251", (stage + 4).ToString());
            m_LoginProgress.Progress = 25 * stage;
        }

        /// <summary>
        /// A network error occured - 95% of the time, this will be because
        /// a connection could not be established.
        /// </summary>
        /// <param name="Exception">The exception that occured.</param>
        private void Controller_OnNetworkError(SocketException Exception)
        {
            UIAlertOptions Options = new UIAlertOptions();
            Options.Message = GameFacade.Strings.GetString("210", "36 301");
            Options.Title = GameFacade.Strings.GetString("210", "40");
            Options.Buttons = UIAlertButtons.OK;
            UI.Framework.UIScreen.ShowAlert(Options, true);

            /** Reset **/
            //Note: A network error *should* never occur in this screen, so this code should never be called.
            GameFacade.Controller.ShowPersonSelection();
        }
    }
}
