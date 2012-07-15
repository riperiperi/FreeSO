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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimsLib.FAR3;
using TSOClient.Network;

namespace TSOClient.LUI
{
    /// <summary>
    /// The CitySelectionDialog is used to select which city a Sim is supposed to live in.
    /// </summary>
    class UICitySelectionDialog : NetworkedUIElement
    {
        private int m_X, m_Y;
        //The texture for the dialog itself.
        private Texture2D m_DiagImg;
        //The caption for the dialog.
        private string m_Caption;

        //Lists the cities that are currently online.
        private UIListBox m_OnlineCities;

        //For displaying the currently selected city's description.
        private UITextEdit m_CityDescription;
        private UIImage m_CityDescBackground;

        //For displaying the currently selected city's image.
        private UIImage m_CityImg;

        private UIButton m_OKBtn, m_CancelBtn;

        /// <summary>
        /// Creates a new instance of CitySelectionDialog. Only used once in the game, when creating a new sim.
        /// </summary>
        /// <param name="Client">A NetworkClient instance, used to communicate with the loginserver.</param>
        /// <param name="X">The x-coordinate of this dialog on the screen.</param>
        /// <param name="Y">The y-coordinate of this dialog on the screen.</param>
        /// <param name="DiagBackgrnd">The background-texture for the dialog. Loaded from dialogs.dat.</param>
        /// <param name="Screen">A UIScreen instance, which is the screen that this dialog will be displayed on.</param>
        /// <param name="StrID">The string ID of this UICitySelectionDialog instance.</param>
        public UICitySelectionDialog(NetworkClient Client, int X, int Y, Texture2D DiagBackgrnd, 
            List<CityServerInformation> CityServerInfo, UIScreen Screen, string StrID) : base(Client, Screen, StrID, 
            DrawLevel.DontGiveAFuck)
        {
            m_DiagImg = DiagBackgrnd;
            m_X = X;
            m_Y = Y;
            m_Caption = "What City Do You Want to Live In?";

            //TODO: Should probably finetune all coordinates to work based on the dialog's coordinates...
            m_OnlineCities = new UIListBox(40, 78, 40, 3, Screen, "LstOnlineCities", DrawLevel.DontGiveAFuck);

            MemoryStream TexStream = new MemoryStream(ContentManager.GetResourceFromLongID(0x8a900000001));
            Texture2D CityDescBackroundTex = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);
            m_CityDescBackground = new UIImage(221, 280, "CityDescBackgroundImg", CityDescBackroundTex, Screen);
            m_CityDescription = new UITextEdit(231, 292, 265, 141, true, 1000, "TxtCityDescription", Screen);

            TexStream = new MemoryStream(ContentManager.GetResourceFromLongID(CityServerInfo[0].ImageID));
            Texture2D CityImgTex = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);
            m_CityImg = new UIImage(28, 283, "CityThumbnailImg", CityImgTex, Screen);

            TexStream = new MemoryStream(ContentManager.GetResourceFromLongID(0x1e700000001));
            Texture2D BtnTex = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);
            m_OKBtn = new UIButton(278, 230, BtnTex, false, "BtnOK", Screen);
            m_CancelBtn = new UIButton(388, 430, BtnTex, false, "BtnCancel", Screen);

            m_Client.OnNetworkError += new NetworkErrorDelegate(m_Client_OnNetworkError);
            m_Client.OnReceivedData += new ReceivedPacketDelegate(m_Client_OnReceivedData);
        }

        private void m_Client_OnNetworkError(System.Net.Sockets.SocketException Exception)
        {
            throw new NotImplementedException();
        }

        private void m_Client_OnReceivedData(PacketStream Packet)
        {
            byte Opcode;

            switch (Packet.PacketID)
            {
            }
        }

        public override void Update(GameTime GTime, ref MouseState CurrentMouseState, ref MouseState PrevioMouseState)
        {
            base.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);

            m_OKBtn.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);
            m_CancelBtn.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);
        }

        public override void Draw(SpriteBatch SBatch)
        {
            base.Draw(SBatch);

            int Scale = GlobalSettings.Default.ScaleFactor;

            SBatch.Draw(m_DiagImg, new Rectangle(m_X, m_Y, m_DiagImg.Width * Scale, m_DiagImg.Height * Scale), Color.White);

            m_OnlineCities.Draw(SBatch);

            m_CityDescBackground.Draw(SBatch);
            m_CityDescription.Draw(SBatch);

            m_CityImg.Draw(SBatch);

            m_OKBtn.Draw(SBatch);
            m_CancelBtn.Draw(SBatch);
        }
    }
}
