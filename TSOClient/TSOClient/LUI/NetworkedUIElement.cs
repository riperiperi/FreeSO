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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TSOClient.Network;

namespace TSOClient.LUI
{
    public abstract class NetworkedUIElement
    {
        protected UIScreen m_Screen;
        protected string m_StringID;
        private DrawLevel m_DrawLevel;

        /// <summary>
        /// Determines what level this NetworkedUIElement will be drawn at.
        /// </summary>
        public DrawLevel DrawingLevel
        {
            get { return m_DrawLevel; }
        }

        //All classes inheriting from NetworkedUIElement MUST subscribe to
        //m_Client.OnNetworkError in order to respond to network errors!!
        protected NetworkClient m_Client;

        public string StrID
        {
            get { return m_StringID; }
        }

        public NetworkedUIElement(string IP, int Port, UIScreen Screen, string StrID, DrawLevel DLevel)
        {
            m_Screen = Screen;
            m_StringID = StrID;

            m_Client = new NetworkClient(IP, Port);
        }

        public NetworkedUIElement(NetworkClient Client, UIScreen Screen, string StrID, DrawLevel DLevel)
        {
            m_Screen = Screen;
            m_StringID = StrID;

            m_Client = Client;
        }

        public virtual void Draw(SpriteBatch SBatch) { }

        public virtual void Update(GameTime GTime) { }

        public virtual void Update(GameTime GTime, ref MouseState CurrentMouseState,
            ref MouseState PrevioMouseState) { }

        /// <summary>
        /// Manually replaces a specified color in a texture with transparent black,
        /// thereby masking it.
        /// </summary>
        /// <param name="Texture">The texture on which to apply the mask.</param>
        /// <param name="ColorFrom">The color to mask away.</param>
        public void ManualTextureMask(ref Texture2D Texture, Color ColorFrom)
        {
            Color ColorTo = Color.TransparentBlack;

            Color[] data = new Color[Texture.Width * Texture.Height];
            Texture.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                data[i].A = 255;
                if (data[i] == ColorFrom)
                    data[i] = ColorTo;
            }

            //This is a hack for non-existing (?) masking colors. It doesn't look vey pretty
            //with non-rectangular images, but it works...
            if (Texture.Format != SurfaceFormat.Color)
            {
                Texture = new Texture2D(Texture.GraphicsDevice, Texture.Width, Texture.Height, 4,
                    TextureUsage.Linear, SurfaceFormat.Color);
            }

            Texture.SetData(data);
        }
    }
}
