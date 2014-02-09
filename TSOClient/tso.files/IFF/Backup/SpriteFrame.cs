using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Iffinator.Flash
{
    class SpriteFrame
    {
        private ushort m_Width, m_Height;
        private ushort m_Flag;
        private ushort m_PaletteID;
        private PaletteMap m_PalMap;
        //private int m_TransparentPixel;
        private Color m_TransparentPixel;
        private ushort m_X, m_Y;
        private Bitmap m_BitmapData;

        public ushort Width
        {
            get { return m_Width; }
            set { m_Width = value; }
        }

        public ushort Height
        {
            get { return m_Height; }
            set { m_Height = value; }
        }

        public ushort Flag
        {
            get { return m_Flag; }
            set { m_Flag = value; }
        }

        public ushort PaletteID
        {
            get { return m_PaletteID; }
            set { m_PaletteID = value; }
        }

        public PaletteMap PalMap
        {
            get { return m_PalMap; }
            set { m_PalMap = value; }
        }

        /*public int TransparentPixel
        {
            get { return m_TransparentPixel; }
            set { m_TransparentPixel = value; }
        }*/

        public Color TransparentPixel
        {
            get { return m_TransparentPixel; }
            set { m_TransparentPixel = value; }
        }

        public ushort XLocation
        {
            get { return m_X; }
            set { m_X = value; }
        }

        public ushort YLocation
        {
            get { return m_Y; }
            set { m_Y = value; }
        }

        public Bitmap BitmapData
        {
            get { return m_BitmapData; }
            set { m_BitmapData = value; }
        }

        public SpriteFrame()
        {
        }

        public void Init()
        {
            if (m_Width > 0 && m_Height > 0)
                m_BitmapData = new Bitmap(m_Width, m_Height);
            else
                m_BitmapData = new Bitmap(1, 1);
        }
    }
}
