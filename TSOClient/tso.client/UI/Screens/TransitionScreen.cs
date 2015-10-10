/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Panels;
using GonzoNet;
using ProtocolAbstractionLibraryD;
using FSO.Client.GameContent;
using FSO.Server.Protocol.CitySelector;
using FSO.Client.Controllers;
using FSO.Client.Regulators;

namespace FSO.Client.UI.Screens
{
    public class TransitionScreen : GameScreen
    {
        private UIContainer m_BackgroundCtnr;
        private UIImage m_Background;
        private UILoginProgress m_LoginProgress;

        private ConnectCASController Controller;
        private CityConnectionRegulator Regulator;

        /// <summary>
        /// Creates a new CityTransitionScreen.
        /// </summary>
        /// <param name="SelectedCity">The city being transitioned to.</param>
        /// <param name="CharacterCreated">If transitioning from CreateASim, this should be true.
        /// A CharacterCreateCity packet will be sent to the CityServer. Otherwise, this should be false.
        /// A CityToken packet will be sent to the CityServer.</param>
        public TransitionScreen()
        {
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
        }

        public bool ShowProgress
        {
            get
            {
                return m_LoginProgress.Visible;
            }
            set
            {
                m_LoginProgress.Visible = value;
            }
        }
        
        public void SetProgress(float progress, int stringIndex)
        {
            m_LoginProgress.ProgressCaption = GameFacade.Strings.GetString("251", (stringIndex).ToString());
            m_LoginProgress.Progress = progress;
        }

        /*
        private void Client_OnConnected(LoginArgsContainer LoginArgs)
        {
            ProgressEvent Progress = 
                new ProgressEvent(EventCodes.PROGRESS_UPDATE);
            Progress.Done = 1;
            Progress.Total = 3;

            UIPacketSenders.SendLoginRequestCity(LoginArgs);
            OnTransitionProgress(Progress);
        }

        private void Controller_OnLoginSuccessCity()
        {
            ProgressEvent Progress = 
                new ProgressEvent(EventCodes.PROGRESS_UPDATE);
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
        /// Authentication failed, so retry.
        /// </summary>
        private void Controller_OnLoginFailureCity()
        {
            if (EventSink.EventQueue[0].ECode == EventCodes.AUTHENTICATION_FAILURE)
            {
                ProgressEvent Progress = 
                    new ProgressEvent(EventCodes.PROGRESS_UPDATE);
                Progress.Done = 1;
                Progress.Total = 3;

                LoginArgsContainer LoginArgs = new LoginArgsContainer();
                LoginArgs.Username = NetworkFacade.Client.ClientEncryptor.Username;
                LoginArgs.Password = Convert.ToBase64String(PlayerAccount.Hash);
                LoginArgs.Enc = NetworkFacade.Client.ClientEncryptor;

                NetworkFacade.Controller.Reconnect(ref NetworkFacade.Client, m_SelectedCity, LoginArgs);
                OnTransitionProgress(Progress);

                //Doing this instead of EventQueue.Clear() ensures we won't accidentally remove any 
                //events that may have been added to the end.
                EventSink.EventQueue.Remove(EventSink.EventQueue[0]);
            }
        }

        private void Controller_OnLoginNotifyCity()
        {
            ProgressEvent Progress = 
                new ProgressEvent(EventCodes.PROGRESS_UPDATE);
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
                    ProgressEvent Progress = new ProgressEvent(EventCodes.PROGRESS_UPDATE);
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
                    ProgressEvent Progress = new ProgressEvent(EventCodes.PROGRESS_UPDATE);
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
        /*private void OnTransitionProgress(ProgressEvent e)
        {
            var stage = e.Done;

            m_LoginProgress.ProgressCaption = GameFacade.Strings.GetString("251", (stage + 4).ToString());
            m_LoginProgress.Progress = 25 * stage;
        }*/

        /// <summary>
        /// A network error occured - 95% of the time, this will be because
        /// a connection could not be established.
        /// </summary>
        /// <param name="Exception">The exception that occured.</param>
        /*private void Controller_OnNetworkError(SocketException Exception)
        {
            UIAlertOptions Options = new UIAlertOptions();
            Options.Message = GameFacade.Strings.GetString("210", "36 301");
            Options.Title = GameFacade.Strings.GetString("210", "40");
            var alert = UI.Framework.UIScreen.ShowAlert(Options, true);

            alert.ButtonMap[UIAlertButtonType.OK].OnButtonClick += new ButtonClickDelegate(ErrorReturnAlert);
            //Note: A network error *should* never occur in this screen, so this code should never be called.
            //Note Note: ahahahaha good one you almost had me there
        }*/

        /*private void ErrorReturnAlert(UIElement button)
        {
            GameFacade.Controller.ShowPersonSelection();
        }*/
    }
}
