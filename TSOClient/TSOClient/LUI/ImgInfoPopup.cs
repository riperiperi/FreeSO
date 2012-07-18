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

namespace TSOClient.LUI
{
    /// <summary>
    /// A drawable, clickable popup-box that contains an image as well as text.
    /// </summary>
    public class ImgInfoPopup
    {
        //private FAR3Archive m_Archive;
        //The texture for the hint image that is part of the dialog.
        private Texture2D m_HintImg;
        //The texture for the dialog itself.
        private Texture2D m_DiagImg;
        //The texture for the corner of the dialog.
        private Texture2D m_DiagCorner;

        private UIButton m_OKCheckBtn;
        
        private string m_Text;
        private int m_X, m_Y;
        private int m_ID;

        public int ID
        {
            get { return m_ID; }
        }

        private UIScreen m_Screen;

        public ImgInfoPopup(int X, int Y, int ID, string ImgFile, int TextID, UIScreen Screen)
        {
            //m_Archive = new FAR3Archive(GlobalSettings.Default.StartupPath + "uigraphics\\dialogs\\dialogs.dat");
            m_HintImg = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, GlobalSettings.Default.StartupPath + 
                "uigraphics\\hints\\" + ImgFile);

            //dialog_backgroundtemplate.tga
            MemoryStream TexStream = new MemoryStream(ContentManager.GetResourceFromLongID(0xe500000002));
            m_DiagImg = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);

            TexStream = new MemoryStream(ContentManager.GetResourceFromLongID(0x18500000002));
            m_DiagCorner = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);

            TexStream = new MemoryStream(ContentManager.GetResourceFromLongID(0x89200000001));
            Texture2D BtnTex = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);
            ManualTextureMask(ref BtnTex, new Color(255, 0, 255));
            m_OKCheckBtn = new UIButton(X + (m_DiagImg.Width + 200) - 88, Y + (m_DiagImg.Height + 50) - 65,
                37, 23, BtnTex, "OKCheckBtn", Screen);

            m_Text = Screen.ScreenMgr.TextDict[TextID];
            m_X = X;
            m_Y = Y;
            m_ID = ID;
           
            m_Screen = Screen;

            ManualTextureMask(ref m_HintImg, new Color(255, 0, 255));
            //AddDownRightCorner(ref m_DiagImg);
        }

        public void Draw(SpriteBatch SBatch)
        {
            float Scale = GlobalSettings.Default.ScaleFactor;

            SBatch.Draw(m_DiagImg, new Vector2(m_X * Scale, m_Y * Scale), null, Color.White, 0.0f, new Vector2(0.0f, 0.0f),
                new Vector2(Scale + .99f, Scale + .25f), SpriteEffects.None, 0.0f);
            
            //Draw the corner of the dialog in the lower right corner...
            SBatch.Draw(m_DiagCorner, new Vector2((m_X + (m_DiagImg.Width * Scale) - (m_DiagCorner.Width * Scale) * Scale),
            ((m_Y + (m_DiagImg.Height * Scale) - (m_DiagCorner.Height * Scale)) * Scale)), null, Color.White, 0.0f, new Vector2(0.0f, 0.0f),
            new Vector2(Scale + .77f, Scale + .13f), SpriteEffects.None, 0.0f);
            
            SBatch.Draw(m_HintImg, new Vector2((m_X + 20) * Scale, (m_Y + 45) * Scale), null, Color.White,
                0.0f, new Vector2(0.0f, 0.0f), Scale, SpriteEffects.None, 0.0f);
            
            SBatch.DrawString(m_Screen.ScreenMgr.SprFontSmall, m_Text,
                new Vector2((m_X + (m_HintImg.Width * GlobalSettings.Default.ScaleFactor) + 30), (m_Y + 45)), 
                new Color(205, 216, 160, 205));

            m_OKCheckBtn.Draw(SBatch);
        }

        public void Update(GameTime GTime, ref MouseState CurrentMouseState, ref MouseState PrevioMouseState)
        {
            m_OKCheckBtn.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);
        }

        /// <summary>
        /// Manually replaces a specified color in a texture with transparent black,
        /// thereby masking it.
        /// </summary>
        /// <param name="Texture">The texture on which to apply the mask.</param>
        /// <param name="ColorFrom">The color to mask away.</param>
        private void ManualTextureMask(ref Texture2D Texture, Color ColorFrom)
        {
            Color ColorTo = Color.TransparentBlack;

            Color[] data = new Color[Texture.Width * Texture.Height];
            Texture.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == ColorFrom)
                    data[i] = ColorTo;
            }

            Texture.SetData(data);
        }
    }
}
