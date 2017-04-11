using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework.IO;

namespace FSO.Client.UI.Controls
{
    public class UISlotsImage : UIElement
    {
        private DoubleDrawMargins DoubleDrawMargins;
        private TripleDrawMargins TripleDrawMargins;
        private Texture2D m_Texture;
        private UISlotsImageDrawTypes DrawType;
        private Rectangle SpecialBounds;
        private float m_Width;
        private float m_Height;

        public UISlotsImage(Texture2D texture)
        {
            this.Texture = texture;
        }

        public Texture2D Texture
        {
            get { return m_Texture; }
            set { m_Texture = value; }
        }

        public float Width
        {
            get { return m_Width; }
        }

        public float Height
        {
            get { return m_Height; }
        }
        public UISlotsImage DoubleTextureDraw(int firstDrawX, int firstDrawY, int firstDrawWidth, int firstDrawHeight,
            int secondDrawX, int secondDrawY, int secondDrawWidth, int secondDrawHeight, bool repeatX, bool repeatY)
        {
            DrawType = UISlotsImageDrawTypes.Double;
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
        public UISlotsImage TripleTextureDraw(int firstDrawX, int firstDrawY, int firstDrawWidth, int firstDrawHeight, int secondDrawX,
            int secondDrawY, int secondDrawWidth, int secondDrawHeight, int thirdDrawX, int thirdDrawY, int thirdDrawWidth,
            int thirdDrawHeight, bool repeatX, bool repeatY)
        {
            DrawType = UISlotsImageDrawTypes.Triple;
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
        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, (int)m_Width, (int)m_Height);
        }

        public void SetBounds(int x, int y, int width, int height)
        {
            m_Width = width;
            m_Height = height;
            DrawType = UISlotsImageDrawTypes.Single;
            SpecialBounds = new Rectangle(x, y, (int)m_Width, (int)m_Height);
        }
        public override void Draw(UISpriteBatch SBatch)
        {
            if ((!Visible) || (m_Texture == null)) { return; }

            switch (DrawType)
            {

                case UISlotsImageDrawTypes.Double:
                    {
                        // draw both pieces
                        DrawLocalTexture(SBatch, m_Texture, DoubleDrawMargins.FirstDrawSource, DoubleDrawMargins.FirstTarget);
                        DrawLocalTexture(SBatch, m_Texture, DoubleDrawMargins.SecondDrawSource, DoubleDrawMargins.SecondTarget);
                        break;
                    }

                case UISlotsImageDrawTypes.Triple:
                    {

                        // draw all three pieces
                        DrawLocalTexture(SBatch, m_Texture, TripleDrawMargins.FirstDrawSource, TripleDrawMargins.FirstTarget);
                        DrawLocalTexture(SBatch, m_Texture, TripleDrawMargins.SecondDrawSource, TripleDrawMargins.SecondTarget);
                        DrawLocalTexture(SBatch, m_Texture, TripleDrawMargins.ThirdDrawSource, TripleDrawMargins.ThirdTarget);
                        break;
                    }
                default:
                    {
                        DrawLocalTexture(SBatch, m_Texture, SpecialBounds, Vector2.Zero);
                        break;
                    }
            }
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

        public Rectangle FirstDrawSource;
        public Rectangle SecondDrawSource;

        public Vector2 FirstTarget;
        public Vector2 SecondTarget;

        public void CalculateOrigins(bool repeatX, bool repeatY)
        {
            FirstDrawSource = new Rectangle(FirstX, FirstY, FirstWidth, FirstHeight);
            SecondDrawSource = new Rectangle(SecondX, SecondY, SecondWidth, SecondHeight);

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

        public Rectangle FirstDrawSource;
        public Rectangle SecondDrawSource;
        public Rectangle ThirdDrawSource;

        public Vector2 FirstTarget;
        public Vector2 SecondTarget;
        public Vector2 ThirdTarget;

        public void CalculateOrigins(bool repeatX, bool repeatY)
        {
            FirstDrawSource = new Rectangle(FirstX, FirstY, FirstWidth, FirstHeight);
            SecondDrawSource = new Rectangle(SecondX, SecondY, SecondWidth, SecondHeight);
            ThirdDrawSource = new Rectangle(ThirdX, ThirdY, ThirdWidth, ThirdHeight);

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
    public enum UISlotsImageDrawTypes : byte
    {
        Single = 0,
        Double = 1,
        Triple = 2
    }
}
