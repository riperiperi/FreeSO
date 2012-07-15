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
using TSOClient.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Un4seen.Bass;

namespace TSOClient.LUI
{
    public delegate void NetworkButtonClickDelegate(UINetworkButton button);

    /// <summary>
    /// A drawable, clickable button that is part of the GUI.
    /// This class inherits from NetworkedUIElement and is responsible
    /// for calling all appropriate packethandlers in the game that are
    /// caused by clicking a button.
    /// </summary>
    public class UINetworkButton : NetworkedUIElement
    {
        public delegate void ButtonClickDelegateWithSender(NetworkedUIElement sender);

        private int m_X, m_Y, m_ScaleX, m_ScaleY, m_CurrentFrame;
        private Texture2D m_Texture;
        private string m_Caption;
        private int m_Width;
        private bool m_Disabled;
        private bool m_HighlightNextDraw;
        private bool m_Invisible;

        private bool m_Clicking = false;

        public event NetworkButtonClickDelegate OnButtonClick;

        /// <summary>
        /// Gets or sets the x-coordinate for where to render this button.
        /// </summary>
        public int X
        {
            get { return m_X; }
            set { m_X = value; }
        }

        /// <summary>
        /// Gets or sets the y-coordinate for where to render this button.
        /// </summary>
        public int Y
        {
            get { return m_Y; }
            set { m_Y = value; }
        }

        public bool Highlight
        {
            set { m_HighlightNextDraw = value; }
        }

        /// <summary>
        /// Gets or sets the current frame of this button.
        /// </summary>
        /// <summary>
        /// Gets or sets the current frame for this button.
        /// </summary>
        public int CurrentFrame
        {
            get
            {
                if (m_CurrentFrame == 0)
                    return 0;
                else
                    return m_Texture.Width / 4 * m_CurrentFrame;
            }

            set
            {
                //Frames go from 0 to 3.
                if (value < 4)
                {
                    m_CurrentFrame = value;
                }
            }
        }

        public UINetworkButton(int X, int Y, Texture2D Texture, string Text, NetworkClient Client, 
            UIScreen Screen, string StrID) : base(Client, Screen, StrID, DrawLevel.AlwaysOnTop)
        {
            m_X = X;
            m_Y = Y;
            m_Caption = Text;

            m_Texture = Texture;
            m_Width = Texture.Width / 4;

            m_CurrentFrame = 0;

            OnButtonClick += new NetworkButtonClickDelegate(delegate(UINetworkButton btn) { Screen.RegisterClick(this); });

            //All classes inheriting from NetworkedUIElement MUST subscribe to these events!
            m_Client.OnReceivedData += new ReceivedPacketDelegate(m_Client_OnReceivedData);
            m_Client.OnNetworkError += new NetworkErrorDelegate(m_Client_OnNetworkError);
        }

        public UINetworkButton(int X, int Y, Texture2D Texture, NetworkClient Client, UIScreen Screen, string StrID)
            : base(Client, Screen, StrID, DrawLevel.AlwaysOnTop)
        {
            m_X = X;
            m_Y = Y;

            m_Texture = Texture;
            m_Width = Texture.Width / 4;

            m_Caption = "";
            m_CurrentFrame = 0;

            OnButtonClick += new NetworkButtonClickDelegate(delegate(UINetworkButton btn) { Screen.RegisterClick(this); });

            //All classes inheriting from NetworkedUIElement MUST subscribe to these events!
            m_Client.OnReceivedData += new ReceivedPacketDelegate(m_Client_OnReceivedData);
            m_Client.OnNetworkError += new NetworkErrorDelegate(m_Client_OnNetworkError);
        }

        public override void Update(GameTime GTime, ref MouseState CurrentMouseState,
                ref MouseState PrevioMouseState)
        {
            base.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);

            if (CurrentMouseState.X >= m_X && CurrentMouseState.X <= (m_X + (m_Width + m_ScaleX)) &&
                CurrentMouseState.Y > m_Y && CurrentMouseState.Y < (m_Y + (25 + m_ScaleY)))
            {
                if (!m_Clicking)
                    CurrentFrame = 2;

                if (CurrentMouseState.LeftButton == ButtonState.Pressed &&
                    PrevioMouseState.LeftButton == ButtonState.Released)
                {
                    m_Clicking = true;
                    //Setting this to 1 seems to cause the animation to be somewhat glitchy,
                    //and I haven't been able to figure out why.
                    CurrentFrame = 0;


                    Bass.BASS_ChannelPlay(UISounds.GetSound(0x01).ThisChannel, false);

                    //This event ususally won't be subscribed to,
                    //it is only used by dialogs that creates buttons
                    //and wants to handle them internally.
                    if (OnButtonClick != null)
                        OnButtonClick(this);
                }
                else
                    m_Clicking = false;
            }
            else
            {
                m_Clicking = false;
                CurrentFrame = 0;
            }
        }

        public void BtnHandle()
        {
            LuaInterfaceManager.CallFunction("ButtonHandler", this);
        }

        public override void Draw(SpriteBatch SBatch)
        {
            base.Draw(SBatch);

            int GlobalScale = GlobalSettings.Default.ScaleFactor;

            if (m_ScaleX == 0 && m_ScaleY == 0)
            {
                //WARNING: Do NOT refer to m_CurrentFrame, as the accessor ensures the right
                //value is returned.
                SBatch.Draw(m_Texture, new Vector2(m_X, m_Y),
                    new Rectangle(CurrentFrame, 0, m_Width * GlobalScale, m_Texture.Height * GlobalScale), Color.White);

                Color c = Color.White;
                switch (CurrentFrame)
                {
                    case 0: c = Color.AliceBlue; break;
                    case 1: c = Color.Wheat; break;
                    case 2: c = Color.White; break;
                    case 3: c = Color.Gray; break;
                }

                if (m_Caption != "")
                {
                    Vector2 CaptionSize = m_Screen.ScreenMgr.SprFontSmall.MeasureString(m_Caption);

                    SBatch.DrawString(m_Screen.ScreenMgr.SprFontSmall, m_Caption,
                        new Vector2(m_X + ((m_Width - CaptionSize.X) / 2),
                        m_Y + ((m_Texture.Height - CaptionSize.Y) / 2)), c);
                }
            }
            else
            {
                //WARNING: Do NOT refer to m_CurrentFrame, as the accessor ensures the right
                //value is returned.
                SBatch.Draw(m_Texture, new Rectangle(m_X, m_Y, (m_Width + m_ScaleX) * GlobalScale, 
                    (m_Texture.Height + m_ScaleY) * GlobalScale), new Rectangle(CurrentFrame, 0, m_Width, m_Texture.Height), Color.White);

                if (m_Caption != "")
                {
                    Vector2 CaptionSize = m_Screen.ScreenMgr.SprFontSmall.MeasureString(m_Caption);
                    int ButtonWidth = m_Width + m_ScaleX;
                    int ButtonHeight = m_Texture.Height + m_ScaleY;

                    SBatch.DrawString(m_Screen.ScreenMgr.SprFontSmall, m_Caption,
                        new Vector2(m_X + ((ButtonWidth - CaptionSize.X) / 2),
                            m_Y + ((ButtonHeight - CaptionSize.Y) / 2)), Color.Wheat);
                }
            }
        }

        private void m_Client_OnNetworkError(System.Net.Sockets.SocketException Exception)
        {
            throw new NotImplementedException();
        }

        private void m_Client_OnReceivedData(PacketStream Packet)
        {
            switch (Packet.PacketID)
            {

            }
        }
    }
}
