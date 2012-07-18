/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SimsLib.FAR3;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TSOClient;
using TSOClient.Network;
using TSOClient.Network.Encryption;

namespace TSOClient.LUI
{
    /// <summary>
    /// A clickable, dragable dialog used for logging in to the login server.
    /// </summary>
    public class UILoginDialog : NetworkedUIElement
    {
        private int m_X, m_Y;
        //The texture for the dialog itself.
        private Texture2D m_DiagImg;
        //The texture for the login progress dialog.
        private Texture2D m_LoginProgressDiag;

        //The caption for the Login dialog.
        private string m_Caption;

        private UILabel m_LblOverallProgress, m_LblCurrentTask;
        private UIProgressBar m_OverallProgressbar, m_CurrentTaskbar;

        private UILabel m_LblAccName, m_LblPass;
        private UITextbox m_TxtAccName, m_TxtPass;

        private UILabel m_LblLogin, m_LblExit;
        private UIButton m_BtnLogin, m_BtnExit;

        /// <summary>
        /// Creates a new instance of LoginDialog. Only used once in the game, during login.
        /// </summary>
        /// <param name="X">X position.</param>
        /// <param name="Y">Y position.</param>
        /// <param name="DiagBackgrnd">The background-texture for the dialog. Loaded from dialogs.dat.</param>
        public UILoginDialog(string IP, int Port, int X, int Y, Texture2D DiagBackgrnd, UIScreen Screen, 
            string StrID) : base(IP, Port, Screen, StrID, DrawLevel.AlwaysOnTop)
        {
            m_DiagImg = DiagBackgrnd;
            m_LoginProgressDiag = DiagBackgrnd;
            m_X = X;
            m_Y = Y;

            //This might have to be passed in to the constructor for language purposes.
            m_Caption = "Login to The Sims Online";

            m_LblAccName = new UILabel(1, "LblAccName", (m_X + 20), (m_Y + 65), Screen);
            m_LblPass = new UILabel(2, "LblPass", (m_X + 20), (m_Y + 125), Screen);
            m_TxtAccName = new UITextbox(0x7A4, (m_X + 20), m_Y + (m_DiagImg.Height / 2),
                1000, 205, Screen, "TxtAccName");
            m_TxtPass = new UITextbox(0x7A4, (m_X + 20), m_Y + ((m_DiagImg.Height / 2) + 60),
                1000, 205, Screen, "TxtPass");

            MemoryStream TexStream = new MemoryStream(ContentManager.GetResourceFromLongID(0x1e700000001));
            Texture2D BtnTex = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);
            Color c = new Color(255, 0, 255, 0);
            ManualTextureMask(ref BtnTex, new Color(255, 0, 255, 255));

            TexStream = new MemoryStream(ContentManager.GetResourceFromLongID(0x7a500000001));
            Texture2D ProgressBTex = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);

            m_LblOverallProgress = new UILabel(5, "LblOverallProgress", (m_X + m_DiagImg.Width) + 50,
                (m_Y + m_DiagImg.Height) + 150, Screen);
            m_LblCurrentTask = new UILabel(6, "LblCurrentTask", (m_X + m_DiagImg.Width) + 50,
                (m_Y + m_DiagImg.Height) + 208, Screen);
            
            float Scale = GlobalSettings.Default.ScaleFactor; 

            //Progressbars for showing the loginprocess to the user.
            m_OverallProgressbar = new UIProgressBar((m_X + (m_DiagImg.Width * Scale) + 50) * Scale,
                (m_Y + (m_DiagImg.Height * Scale) + 180) * Scale, 800, ProgressBTex, "0%", Screen, "OverallProgressBar");
            m_CurrentTaskbar = new UIProgressBar((m_X + (m_DiagImg.Width * Scale) + 50) * Scale,
                (m_Y + (m_DiagImg.Height * Scale) + 238) * Scale, 800, ProgressBTex, 
                "Authorizing. Prompting for name and password...", Screen, "CurrentTaskBar");

            //TextID 3: "Login"
            //TextID 4: "Exit"
            m_BtnLogin = new UIButton((m_X + 125) * Scale, (m_Y + (m_DiagImg.Height * Scale) + 35) * Scale, .13f, .2f,
                BtnTex, 3, "BtnLogin", Screen);
            m_BtnExit = new UIButton((m_X + (m_DiagImg.Width * Scale) + 60) * Scale, (m_Y + (m_DiagImg.Height * Scale) + 35) * Scale, 
                .13f, .2f, BtnTex, 4, "BtnExit", Screen);

            //All classes inheriting from NetworkedUIElement MUST subscribe to these events!
            m_Client.OnNetworkError += new TSOClient.Network.NetworkErrorDelegate(m_Client_OnNetworkError);
            m_Client.OnReceivedData += new TSOClient.Network.ReceivedPacketDelegate(m_Client_OnReceivedData);
            m_BtnLogin.OnButtonClick += new ButtonClickDelegate(m_BtnLogin_ButtonClickEvent);
        }

        #region Network

        /// <summary>
        /// Called when the NetworkClient received a new packet.
        /// </summary>
        /// <param name="Packet">The packet received.</param>
        private void m_Client_OnReceivedData(PacketStream Packet)
        {
            switch (Packet.PacketID)
            {
                //InitLoginNotify - 21 bytes
                case 0x01:
                    m_OverallProgressbar.UpdateStatus("25%");
                    m_CurrentTaskbar.UpdateStatus("Attempting authorization...");

                    UIPacketHandlers.OnInitLoginNotify(m_Client, Packet);

                    break;
                //LoginFailResponse - 2 bytes
                case 0x02:
                    UIPacketHandlers.OnLoginFailResponse(ref m_Client, Packet, m_Screen);

                    break;
                //CharacterInfoResponse
                case 0x05:
                    m_OverallProgressbar.UpdateStatus("50%");
                    m_CurrentTaskbar.UpdateStatus("Success!");

                    UIPacketHandlers.OnCharacterInfoResponse(m_Client, Packet);
                    LuaInterfaceManager.CallFunction("LoginSuccess");

                    break;
            }
        }

        /// <summary>
        /// Called if an exception occured when connecting or performing network related tasks.
        /// </summary>
        /// <param name="Exception">The SocketException instance with information about what went wrong.</param>
        private void m_Client_OnNetworkError(System.Net.Sockets.SocketException Exception)
        {
            string ErrorMsg = "";

            //For now, these are hardcoded.
            switch (Exception.ErrorCode)
            {
                case 10060:
                    if (GlobalSettings.Default.CurrentLang == "English")
                        ErrorMsg = "Couldn't connect \n- connection timed out!";
                    else if (GlobalSettings.Default.CurrentLang == "Norwgian")
                        ErrorMsg = "Kunne ikke koble til \n- forbindelsen fikk tidsavbrudd!";
                    break;
                case 10061:
                    if (GlobalSettings.Default.CurrentLang == "English")
                        ErrorMsg = "Couldn't connect \n- connection refused!";
                    else if (GlobalSettings.Default.CurrentLang == "Norwegian")
                        ErrorMsg = "Kunne ikke koble til \n- tilkobling ble nektet!";
                    break;
                case 10064:
                    if (GlobalSettings.Default.CurrentLang == "English")
                        ErrorMsg = "Couldn't connect \n- host is down!";
                    else if (GlobalSettings.Default.CurrentLang == "Norwegian")
                        ErrorMsg = "Kunne ikke koble til \n- tjeneren er nede!";
                    break;
                case 10065:
                    if (GlobalSettings.Default.CurrentLang == "English")
                        ErrorMsg = "Couldn't connect \n- encountered unreachable host!";
                    else if (GlobalSettings.Default.CurrentLang == "Norwegian")
                        ErrorMsg = "Kunne ikke koble til \n- fant ikke tjenerens adresse!";
                    break;
                case 1101:
                    if (GlobalSettings.Default.CurrentLang == "English")
                        ErrorMsg = "Couldn't connect \n- host could not be found!";
                    else if (GlobalSettings.Default.CurrentLang == "Norwegian")
                        ErrorMsg = "Kunne ikke koble til \n- fant ikke tjener!";
                    break;
            }

            //TODO: Create a messagebox here, informing the user what went wrong...
            m_Screen.CreateMsgBox(270, 150, ErrorMsg);
        }

        #endregion

        /// <summary>
        /// Called when the Login button was clicked.
        /// </summary>
        private void m_BtnLogin_ButtonClickEvent(UIButton btn)
        {
            m_Client.Connect(m_TxtAccName.CurrentText.ToUpper(), m_TxtPass.CurrentText.ToUpper());
        }

        public override void Update(GameTime GTime, ref MouseState CurrentMouseState, 
            ref MouseState PrevioMouseState)
        {
            base.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);

            m_TxtAccName.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);
            m_TxtPass.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);

            //No need to call update on the labels...

            m_BtnLogin.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);
            m_BtnExit.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);

            m_OverallProgressbar.Update(GTime);
            m_CurrentTaskbar.Update(GTime);
        }

        public override void Draw(SpriteBatch SBatch)
        {
            float Scale = GlobalSettings.Default.ScaleFactor;

            SBatch.Draw(m_DiagImg, new Vector2(m_X * Scale, m_Y * Scale), null, new Color(255, 255, 255, 205), 0.0f, 
                new Vector2(0.0f, 0.0f), new Vector2(Scale + .99f, Scale + .50f), SpriteEffects.None, 0.0f);

            SBatch.Draw(m_LoginProgressDiag, new Vector2((m_X + (m_DiagImg.Width * Scale)) * Scale, (m_Y + (m_DiagImg.Width * Scale)) * (Scale + .40f)),
                null, new Color(255, 255, 255, 205), 0.0f, new Vector2(0.0f, 0.0f), new Vector2(Scale + .99f, Scale - .13f), 
                SpriteEffects.None, 0.0f);

            SBatch.DrawString(m_Screen.ScreenMgr.SprFontBig, m_Caption, new Vector2((m_X + (m_DiagImg.Width / 2) * Scale),
                m_Y * Scale), Color.Wheat);
            SBatch.DrawString(m_Screen.ScreenMgr.SprFontBig, "The Sims Online Login",
                new Vector2(((m_X + (m_DiagImg.Width * Scale)) + 110) * Scale, (((m_Y + (m_DiagImg.Height * Scale)) * Scale) + 130) * Scale), Color.Wheat);

            m_LblAccName.Draw(SBatch);
            m_LblPass.Draw(SBatch);
            m_TxtAccName.Draw(SBatch);
            m_TxtPass.Draw(SBatch);

            m_BtnLogin.Draw(SBatch);
            m_BtnExit.Draw(SBatch);

            m_LblOverallProgress.Draw(SBatch);
            m_LblCurrentTask.Draw(SBatch);
            m_OverallProgressbar.Draw(SBatch);
            m_CurrentTaskbar.Draw(SBatch);


            base.Draw(SBatch);
        }
    }
}
