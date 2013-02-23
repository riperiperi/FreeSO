using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TSOClient.Code.UI.Controls
{
    public class UITextBox : UIContainer
    {
        /**
         * Background texture & resize info
         */
        private Texture2D m_BackgroundTex;
        private NineSliceMargins NineSliceMargins;
        private float m_Width;
        private float m_Height;

        /**
         * Text box vars
         */
        private StringBuilder m_SBuilder = new StringBuilder();
        private bool m_HasFocus = false;
        private KeyboardState m_CurrentKeyState;
        private KeyboardState m_OldKeyState;


        public UITextBox()
        {
            this.SetBackgroundTexture(
                GetTexture((ulong)TSOClient.FileIDs.UIFileIDs.dialog_textboxbackground),
                13, 13, 13, 13);
        }


        /**
         * Functionality
         */

        /// <summary>
        /// Returns the current text (input) in this textbox.
        /// </summary>
        public string CurrentText
        {
            get { return m_SBuilder.ToString(); }
        }



        /**
         * Properties
         */
        
        /// <summary>
        /// Background texture
        /// </summary>
        public Texture2D BackgroundTexture
        {
            get { return m_BackgroundTex; }
        }


        public void SetBackgroundTexture(Texture2D texture, int marginLeft, int marginRight, int marginTop, int marginBottom)
        {
            m_BackgroundTex = texture;
            NineSliceMargins = new NineSliceMargins {
                Left = marginLeft,
                Right = marginRight,
                Top = marginTop,
                Bottom = marginBottom
            };
            NineSliceMargins.CalculateOrigins(texture);
        }

        /// <summary>
        /// Component width
        /// </summary>
        public float Width
        {
            get { return m_Height; }
        }

        /// <summary>
        /// Component height
        /// </summary>
        public float Height
        {
            get { return m_Height; }
        }


        public void SetSize(float width, float height)
        {
            m_Width = width;
            m_Height = height;
            
            NineSliceMargins.CalculateScales(m_Width, m_Height);
            /*
            if (m_MouseEvent != null)
            {
                m_MouseEvent.Region = new Rectangle(0, 0, (int)m_Width, (int)m_Height);
            }*/
        }


        /// <summary>
        /// Render
        /// </summary>
        /// <param name="batch"></param>
        public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch batch)
        {
            NineSliceMargins.DrawOnto(batch, this, m_BackgroundTex, m_Width, m_Height);

            /**
             * Draw text
             */
            //batch.DrawString(m_Screen.ScreenMgr.SprFontSmall,
            //    ClipTextLeft(m_Screen.ScreenMgr.SprFontSmall, m_SBuilder.ToString(), ((m_Width * Scale) - 23), 2),
            //    new Vector2((m_X + 3) * Scale, (m_Y + 8) * Scale), Color.Wheat);

        }
    }
}
