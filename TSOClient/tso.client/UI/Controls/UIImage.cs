/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework.IO;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// A drawable image that is part of the GUI.
    /// Cannot be clicked.
    /// </summary>
    public class UIImage : UIElement
    {
        private Texture2D m_Texture;
        /** Used for complex textures that resize etc **/
        private ITextureRef m_TextureRef;

        private bool NineSlice;
        private NineSliceMargins NineSliceMargins;
        private bool tripleTexture;
        private TripleDrawMargins TripleDrawMargins;
        private bool doubleTexture;
        private DoubleDrawMargins DoubleDrawMargins;
        private bool hasSpecialBounds;
        private Rectangle specialBounds;

        private float m_Width;
        private float m_Height;

        public UIImage()
        {
        }

        public UIImage(Texture2D Texture)
        {
            this.Texture = Texture;
        }

        public UIImage(ITextureRef Texture)
        {
            this.m_TextureRef = Texture;
        }

        public void Reset()
        {
            m_Width = 0;
            m_Height = 0;
        }

        private UIMouseEventRef m_MouseEvent;
        /// <summary>
        /// Listen for mouse events on the whole image
        /// </summary>
        /// <param name="callback"></param>
        public void ListenForMouse(UIMouseEvent callback)
        {
            m_MouseEvent = ListenForMouse(new Rectangle(0, 0, (int)Width, (int)Height), callback);
        }

        [UIAttribute("image")]
        public Texture2D Texture
        {
            get { return m_Texture; }
            set
            {
                m_Texture = value;
                if (Width == 0)
                {
                    m_Width = m_Texture.Width;
                }
                if (Height == 0)
                {
                    m_Height = m_Texture.Height;
                }
            }
        }

        public void BlockInput()
        {
            ListenForMouse(new UIMouseEvent(BlockMouseEvent));
        }

        private void BlockMouseEvent(UIMouseEventType type, UpdateState state)
        {
            //do nothing! that's the whole idea of blocking input
        }

        /// <summary>
        /// Sets 9 slice options on the image, this allows it to be cut
        /// into 9 pieces for scaling
        /// </summary>
        /// <returns></returns>
        public UIImage With9Slice(int marginLeft, int marginRight, int marginTop, int marginBottom)
        {
            NineSlice = true;
            NineSliceMargins = new NineSliceMargins
            {
                Left = marginLeft,
                Right = marginRight,
                Top = marginTop,
                Bottom = marginBottom
            };
            NineSliceMargins.CalculateOrigins(m_Texture);

            return this;
        }
        public UIImage DoubleTextureDraw(int firstDrawX, int firstDrawY, int firstDrawWidth, int firstDrawHeight,
            int secondDrawX, int secondDrawY, int secondDrawWidth, int secondDrawHeight, bool repeatX, bool repeatY)
        {
            doubleTexture = true;
            DoubleDrawMargins = new DoubleDrawMargins
            {
                FirstX = firstDrawX,
                FirstY = firstDrawY,
                FirstWidth = firstDrawWidth,
                FirstHeight = firstDrawHeight,
                SecondX = secondDrawX,
                SecondY = secondDrawY,
                SecondWidth = secondDrawWidth,
                SecondHeight = secondDrawHeight
            };
            DoubleDrawMargins.CalculateOrigins(repeatX, repeatY);
            return this;
        }
        public UIImage TripleTextureDraw(int firstDrawX, int firstDrawY, int firstDrawWidth, int firstDrawHeight, int secondDrawX,
            int secondDrawY, int secondDrawWidth, int secondDrawHeight, int thirdDrawX, int thirdDrawY, int thirdDrawWidth,
            int thirdDrawHeight, bool repeatX, bool repeatY)
        {
            tripleTexture = true;
            TripleDrawMargins = new TripleDrawMargins
            {
                FirstX = firstDrawX,
                FirstY = firstDrawY,
                FirstWidth = firstDrawWidth,
                FirstHeight = firstDrawHeight,
                SecondX = secondDrawX,
                SecondY = secondDrawY,
                SecondWidth = secondDrawWidth,
                SecondHeight = secondDrawHeight,
                ThirdX = thirdDrawX,
                ThirdY = thirdDrawY,
                ThirdWidth = thirdDrawWidth,
                ThirdHeight = thirdDrawHeight
            };
            TripleDrawMargins.CalculateOrigins(repeatX, repeatY);
            return this;
        }
        public float Width
        {
            get { return m_Width; }
        }

        public float Height
        {
            get { return m_Height; }
        }

        public void SetSize(float width, float height)
        {
            m_Width = width;
            m_Height = height;
            if (NineSlice)
            {
                NineSliceMargins.CalculateScales(m_Width, m_Height);
            }

            if (m_MouseEvent != null)
            {
                m_MouseEvent.Region = new Rectangle(0, 0, (int)m_Width, (int)m_Height);
            }
        }

        [UIAttribute("size")]
        public new Point Size
        {
            get
            {
                return new Point((int)m_Width, (int)m_Height);
            }
            set
            {
                SetSize(value.X, value.Y);
            }
        }

        public override void Update(UpdateState statex)
        {
            base.Update(statex);
        }


        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, (int)m_Width, (int)m_Height);
        }

        public void SetBounds(int x, int y, int width, int height)
        {
            m_Width = width;
            m_Height = height;
            hasSpecialBounds = true;
            specialBounds = new Rectangle(x, y, (int)m_Width, (int)m_Height);
        }

        public override void Draw(UISpriteBatch SBatch)
        {
            if (!Visible) { return; }

            if (NineSlice)
            {

                if (m_Texture == null) { return; }

                /**
                 * We need to draw 9 pieces
                 */

                /** TL **/
                DrawLocalTexture(SBatch, m_Texture, NineSliceMargins.TL, Vector2.Zero);

                /** TC **/
                DrawLocalTexture(SBatch, m_Texture, NineSliceMargins.TC, new Vector2(NineSliceMargins.Left, 0), NineSliceMargins.TC_Scale);

                /** TR **/
                DrawLocalTexture(SBatch, m_Texture, NineSliceMargins.TR, new Vector2(Width - NineSliceMargins.Right, 0));

                /** ML **/
                DrawLocalTexture(SBatch, m_Texture, NineSliceMargins.ML, new Vector2(0, NineSliceMargins.Top), NineSliceMargins.ML_Scale);

                /** MC **/
                DrawLocalTexture(SBatch, m_Texture, NineSliceMargins.MC, new Vector2(NineSliceMargins.Left, NineSliceMargins.Top), NineSliceMargins.MC_Scale);

                /** MR **/
                DrawLocalTexture(SBatch, m_Texture, NineSliceMargins.MR, new Vector2(Width - NineSliceMargins.Right, NineSliceMargins.Top), NineSliceMargins.MR_Scale);

                /** BL **/
                var bottomY = Height - NineSliceMargins.Bottom;
                DrawLocalTexture(SBatch, m_Texture, NineSliceMargins.BL, new Vector2(0, bottomY));

                /** BC **/
                DrawLocalTexture(SBatch, m_Texture, NineSliceMargins.BC, new Vector2(NineSliceMargins.Left, bottomY), NineSliceMargins.BC_Scale);

                /** BR **/
                DrawLocalTexture(SBatch, m_Texture, NineSliceMargins.BR, new Vector2(Width - NineSliceMargins.Right, bottomY));


            }
            else if (doubleTexture)
            {
                if (m_Texture == null) { return; }

                // draw both pieces
                DrawLocalTexture(SBatch, m_Texture, DoubleDrawMargins.firstDrawSource, DoubleDrawMargins.FirstTarget);
                DrawLocalTexture(SBatch, m_Texture, DoubleDrawMargins.secondDrawSource, DoubleDrawMargins.SecondTarget);
            }
            else if (tripleTexture)
            {
                if (m_Texture == null) { return; }

                // draw all three pieces
                DrawLocalTexture(SBatch, m_Texture, TripleDrawMargins.firstDrawSource, TripleDrawMargins.FirstTarget);
                DrawLocalTexture(SBatch, m_Texture, TripleDrawMargins.secondDrawSource, TripleDrawMargins.SecondTarget);
                DrawLocalTexture(SBatch, m_Texture, TripleDrawMargins.thirdDrawSource, TripleDrawMargins.ThirdTarget);
            }
            else if (m_TextureRef != null)
            {
                m_TextureRef.Draw(SBatch, this, 0, 0, m_Width, m_Height);
            }
            else
            {
                if (m_Texture == null) { return; }

                if (hasSpecialBounds)
                {
                    DrawLocalTexture(SBatch, m_Texture, specialBounds, Vector2.Zero);
                }
                else if (m_Width != 0 && m_Height != 0)
                {
                    DrawLocalTexture(SBatch, m_Texture, null, Vector2.Zero, new Vector2(m_Width / m_Texture.Width, m_Height / m_Texture.Height));
                }
                else
                {
                    DrawLocalTexture(SBatch, m_Texture, Vector2.Zero);
                }
            }
        }
    }

    class NineSliceMargins
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;

        public Rectangle TL;
        public Rectangle TC;
        public Rectangle TR;

        public Rectangle ML;
        public Rectangle MC;
        public Rectangle MR;

        public Rectangle BL;
        public Rectangle BC;
        public Rectangle BR;

        public Vector2 TC_Scale;
        public Vector2 MC_Scale;
        public Vector2 BC_Scale;

        public Vector2 ML_Scale;
        public Vector2 MR_Scale;

        public void CalculateOrigins(Texture2D texture)
        {
            int texWidth = texture.Width;
            int texHeight = texture.Height;

            TL = new Rectangle(0, 0, Left, Top);
            TC = new Rectangle(Left, 0, texWidth - (Left + Right), Top);
            TR = new Rectangle(TC.X + TC.Width, 0, Right, Top);

            ML = new Rectangle(0, Top, Left, texHeight - (Top + Bottom));
            MC = new Rectangle(Left, Top, texWidth - (Left + Right), ML.Height);
            MR = new Rectangle(TC.X + TC.Width, Top, Right, ML.Height);

            BL = new Rectangle(0, texHeight - Bottom, Left, Bottom);
            BC = new Rectangle(Left, BL.Y, texWidth - (Left + Right), Bottom);
            BR = new Rectangle(TC.X + TC.Width, BL.Y, Right, Bottom);
        }

        public void CalculateScales(float width, float height)
        {
            TC_Scale = new Vector2((width - (Left + Right)) / (TC.Width), 1);
            MC_Scale = new Vector2(
                            (width - (Left + Right)) / (MC.Width),
                            (height - (Top + Bottom)) / (MC.Height)
                       );
            BC_Scale = new Vector2((width - (Left + Right)) / (BC.Width), 1);


            ML_Scale = new Vector2(1, (height - (Top + Bottom)) / (ML.Height));
            MR_Scale = new Vector2(1, (height - (Top + Bottom)) / (MR.Height));
        }

        public void DrawOnto(SpriteBatch SBatch, UIElement element, Texture2D m_Texture, float width, float height)
        {
            /** TL **/
            element.DrawLocalTexture(SBatch, m_Texture, this.TL, Vector2.Zero);

            /** TC **/
            element.DrawLocalTexture(SBatch, m_Texture, this.TC, new Vector2(this.Left, 0), this.TC_Scale);

            /** TR **/
            element.DrawLocalTexture(SBatch, m_Texture, this.TR, new Vector2(width - this.Right, 0));

            /** ML **/
            element.DrawLocalTexture(SBatch, m_Texture, this.ML, new Vector2(0, this.Top), this.ML_Scale);

            /** MC **/
            element.DrawLocalTexture(SBatch, m_Texture, this.MC, new Vector2(this.Left, this.Top), this.MC_Scale);

            /** MR **/
            element.DrawLocalTexture(SBatch, m_Texture, this.MR, new Vector2(width - this.Right, this.Top), this.MR_Scale);

            /** BL **/
            var bottomY = height - this.Bottom;
            element.DrawLocalTexture(SBatch, m_Texture, this.BL, new Vector2(0, bottomY));

            /** BC **/
            element.DrawLocalTexture(SBatch, m_Texture, this.BC, new Vector2(this.Left, bottomY), this.BC_Scale);

            /** BR **/
            element.DrawLocalTexture(SBatch, m_Texture, this.BR, new Vector2(width - this.Right, bottomY));

        }

        public void DrawOntoPosition(SpriteBatch SBatch, UIElement element, Texture2D m_Texture, float width, float height, Vector2 position)
        {
            /** TL **/
            element.DrawLocalTexture(SBatch, m_Texture, this.TL, position);

            /** TC **/
            element.DrawLocalTexture(SBatch, m_Texture, this.TC, position + new Vector2(this.Left, 0), this.TC_Scale);

            /** TR **/
            element.DrawLocalTexture(SBatch, m_Texture, this.TR, position + new Vector2(width - this.Right, 0));

            /** ML **/
            element.DrawLocalTexture(SBatch, m_Texture, this.ML, position + new Vector2(0, this.Top), this.ML_Scale);

            /** MC **/
            element.DrawLocalTexture(SBatch, m_Texture, this.MC, position + new Vector2(this.Left, this.Top), this.MC_Scale);

            /** MR **/
            element.DrawLocalTexture(SBatch, m_Texture, this.MR, position + new Vector2(width - this.Right, this.Top), this.MR_Scale);

            /** BL **/
            var bottomY = height - this.Bottom;
            element.DrawLocalTexture(SBatch, m_Texture, this.BL, position + new Vector2(0, bottomY));

            /** BC **/
            element.DrawLocalTexture(SBatch, m_Texture, this.BC, position + new Vector2(this.Left, bottomY), this.BC_Scale);

            /** BR **/
            element.DrawLocalTexture(SBatch, m_Texture, this.BR, position + new Vector2(width - this.Right, bottomY));
        }
    }
    class DoubleDrawMargins
    {
        public int FirstX;
        public int FirstY;
        public int FirstWidth;
        public int FirstHeight;
        public int SecondX;
        public int SecondY;
        public int SecondWidth;
        public int SecondHeight;

        public Rectangle firstDrawSource;
        public Rectangle secondDrawSource;

        public Vector2 FirstTarget;
        public Vector2 SecondTarget;

        public void CalculateOrigins(bool repeatX, bool repeatY)
        {
            firstDrawSource = new Rectangle(FirstX, FirstY, FirstWidth, FirstHeight);
            secondDrawSource = new Rectangle(SecondX, SecondY, SecondWidth, SecondHeight);

            FirstTarget = Vector2.Zero;

            if (repeatX == true)
            {
                if (repeatY == true)
                { // repeat both X and Y
                    SecondTarget = new Vector2(FirstWidth, FirstHeight);
                }
                else // only repeat X
                    SecondTarget = new Vector2(FirstWidth, 0);
            }
            else // only repeat Y
            {
                SecondTarget = new Vector2(0, FirstHeight);
            }
        }
    }
    class TripleDrawMargins
    {
        public int FirstX;
        public int FirstY;
        public int FirstWidth;
        public int FirstHeight;
        public int SecondX;
        public int SecondY;
        public int SecondWidth;
        public int SecondHeight;
        public int ThirdX;
        public int ThirdY;
        public int ThirdWidth;
        public int ThirdHeight;

        public Rectangle firstDrawSource;
        public Rectangle secondDrawSource;
        public Rectangle thirdDrawSource;

        public Vector2 FirstTarget;
        public Vector2 SecondTarget;
        public Vector2 ThirdTarget;

        public void CalculateOrigins(bool repeatX, bool repeatY)
        {
            firstDrawSource = new Rectangle(FirstX, FirstY, FirstWidth, FirstHeight);
            secondDrawSource = new Rectangle(SecondX, SecondY, SecondWidth, SecondHeight);
            thirdDrawSource = new Rectangle(ThirdX, ThirdY, ThirdWidth, ThirdHeight);

            FirstTarget = Vector2.Zero;

            if (repeatX == true)
            {
                if (repeatY == true)
                { // repeat both X and Y
                    SecondTarget = new Vector2(FirstWidth, FirstHeight);
                    ThirdTarget = new Vector2(FirstWidth + SecondWidth, FirstHeight + SecondHeight);
                }
                else // only repeat X
                {
                    SecondTarget = new Vector2(FirstWidth, 0);
                    ThirdTarget = new Vector2(FirstWidth + SecondWidth, 0);
                }
            }
            else // only repeat Y
            {
                SecondTarget = new Vector2(0, FirstHeight);
                ThirdTarget = new Vector2(0, FirstHeight + SecondHeight);
            }
        }
    }
}
