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

namespace TSOClient.LUI
{
    public class UIProgressBar : UIElement
    {
        private Texture2D m_BackgroundTile;
        //How many tiles of m_BackgroundTile will have to be drawn to get the correct width of the progressbar.
        private int m_NumTiles;
        private int m_X, m_Y, m_Width;
        private string m_CurrentStatus;

        private bool m_ShrinkText = false;

        /// <summary>
        /// Constructor for the UIProgressbar class.
        /// </summary>
        /// <param name="X">The X-coordinate on the screen of where to draw this progressbar.</param>
        /// <param name="Y">The Y-coordinate on the screen of where to draw this progressbar.</param>
        /// <param name="Width">The width of the progressbar in pixels. Should be greater than 45.</param>
        /// <param name="Background">The background texture for this progressbar. (ID: 0x7A5)</param>
        /// <param name="Status">The initial status appearing on the progressbar.</param>
        /// <param name="Screen">A UIScreen instance.</param>
        /// <param name="StrID">The string ID of this progressbar.</param>
        public UIProgressBar(int X, int Y, int Width, Texture2D Background, string Status, 
            UIScreen Screen, string StrID) : base(Screen, StrID, DrawLevel.DontGiveAFuck)
        {
            m_BackgroundTile = Background;
            m_X = X;
            m_Y = Y;
            m_CurrentStatus = Status;

            //The Width parameter will hopefully always be greater than 39, as the
            //'dialog_textboxbackground.bmp' image is 39x39 pixels.
            if (Width > m_BackgroundTile.Width)
            {
                m_NumTiles = (int)(Width / (m_BackgroundTile.Width + 3));
                //The background consist of one half tile + one third of a tile as many times as m_NumTiles,
                //then another half tile. Therefore the total width equals the calculation below.
                m_Width = (m_NumTiles * 15) + m_BackgroundTile.Width;
            }
            else
            {
                //Originally I set m_NumTiles to m_BackgroundTex.Width here, but realized
                //that that would cause fucking ENDLESS progressbars (39 tiles!!)
                m_NumTiles = Width;
                m_Width = m_BackgroundTile.Width;
            }

            if (Screen.ScreenMgr.SprFontBig.MeasureString(Status).X > m_Width)
                m_ShrinkText = true;
        }

        /// <summary>
        /// Updates the current status of this progressbar.
        /// </summary>
        /// <param name="CurrentStatus">The current status of the progressbar.</param>
        public void UpdateStatus(string CurrentStatus)
        {
            m_CurrentStatus = CurrentStatus;
        }

        public override void Update(GameTime GTime)
        {
            base.Update(GTime);
        }

        public override void Draw(SpriteBatch SBatch)
        {
            base.Draw(SBatch);

            int Scale = GlobalSettings.Default.ScaleFactor;

            //First, draw one half tile for the beginning of the background...
            SBatch.Draw(m_BackgroundTile, new Rectangle(m_X, m_Y, (m_BackgroundTile.Width) * Scale, 
                m_BackgroundTile.Height * Scale), Color.White);

            //... then tile as many times as m_NumTiles specifies...
            int X = (m_X + 15);
            for (int i = 0; i < (m_NumTiles - 2); i++)
            {
                X = X + 15;
                SBatch.Draw(m_BackgroundTile, new Rectangle(X, m_Y, (m_BackgroundTile.Width / 3),
                    m_BackgroundTile.Height), new Rectangle(15, 0, 15, m_BackgroundTile.Height * Scale),
                    Color.White);
            }

            //...and then draw another half tile (the second half this time).
            SBatch.Draw(m_BackgroundTile, new Rectangle(X, m_Y, m_BackgroundTile.Width, m_BackgroundTile.Height),
                new Rectangle(22, 0, (m_BackgroundTile.Width / 2), m_BackgroundTile.Height), Color.White);

            if (!m_ShrinkText)
            {
                SBatch.DrawString(m_Screen.ScreenMgr.SprFontBig, m_CurrentStatus,
                    new Vector2(m_X + (m_Screen.ScreenMgr.SprFontBig.MeasureString(m_CurrentStatus).X / m_Width),
                        m_Y + 3), Color.Wheat);
            }
            else
            {
                Vector2 Size = m_Screen.ScreenMgr.SprFontBig.MeasureString(m_CurrentStatus);
                float HalfWidth = m_X + m_Width / 2;

                //Draw-origin is exactly in the middle of the text...
                SBatch.DrawString(m_Screen.ScreenMgr.SprFontBig, m_CurrentStatus, new Vector2(HalfWidth, 
                    m_Y + 10), Color.Wheat, (float)0.0, new Vector2(Size.X / 2, Size.Y / 2), 
                    (float)0.7, SpriteEffects.None, 0);
            }
        }
    }
}
