//Code is licensed under CPOL: http://www.codeproject.com/info/cpol10.aspx
//Ported by Afr0 from: http://www.codeproject.com/script/Articles/ViewDownloads.aspx?aid=15192

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Iffinator
{
    /// <summary>
    /// Class that provides a fast way for putting pixels onto a bitmap.
    /// </summary>
    public class FastPixel
    {
        private byte[] m_RGBValues;
        private BitmapData m_BMPData;
        private IntPtr m_BMPPtr;
        private bool m_Locked = false;

        private bool m_IsAlpha = false;
        private Bitmap m_Bitmap;
        private int m_Width, m_Height;

        public int Width
        {
            get { return m_Width; }
        }

        public int Height
        {
            get { return m_Height; }
        }

        public bool IsAlphaBitmap
        {
            get { return m_IsAlpha; }
        }

        public Bitmap BitMap
        {
            get 
            {   return m_Bitmap; }
        }

        /// <summary>
        /// Constructs an instance of FastPixel.
        /// </summary>
        /// <param name="BMap">The bitmap on which to set pixels.</param>
        /// <param name="IsAlpha">Is the bitmap an alpha bitmap?</param>
        public FastPixel(Bitmap BMap, bool IsAlpha)
        {
            if (BMap.PixelFormat == PixelFormat.Indexed)
                throw new Exception("Cannot lock an indexed bitmap - fastpixel.cs!");

            m_Bitmap = BMap;

            if(IsAlpha)
                m_IsAlpha = true;

            m_Width = BMap.Width;
            m_Height = BMap.Height;
        }

        public void Lock()
        {
            if (m_Locked)
                throw new Exception("Bitmap already locked - fastpixel.cs!");

            Rectangle Rect = new Rectangle(0, 0, m_Width, m_Height);
            m_BMPData = m_Bitmap.LockBits(Rect, ImageLockMode.ReadWrite, m_Bitmap.PixelFormat);
            m_BMPPtr = m_BMPData.Scan0;

            if (m_IsAlpha)
            {
                byte[] Bytes = new byte[(m_Width * m_Height) * 4];
                m_RGBValues = new byte[Bytes.Length];
                Marshal.Copy(m_BMPPtr, m_RGBValues, 0, m_RGBValues.Length);
            }
            else
            {
                byte[] Bytes = new byte[(m_Width * m_Height) * 3];
                m_RGBValues = new byte[Bytes.Length];
                Marshal.Copy(m_BMPPtr, m_RGBValues, 0, m_RGBValues.Length);
            }

            m_Locked = true;
        }

        public void Unlock(bool SetPixels)
        {
            if (!m_Locked)
                throw new Exception("Bitmap not locked - fastpixel.cs!");

            //Copy the RGB values back to the bitmap
            if (SetPixels)
                Marshal.Copy(m_RGBValues, 0, m_BMPPtr, m_RGBValues.Length);

            m_Bitmap.UnlockBits(m_BMPData);
            m_Locked = false;
        }

        /// <summary>
        /// Clears the internal bitmap to the provided color.
        /// </summary>
        /// <param name="Clr">The color to clear to.</param>
        public void Clear(Color Clr)
        {
            if (!m_Locked)
                throw new Exception("Bitmap not locked - fastpixel.cs!");

            if (m_IsAlpha)
            {
                for (int i = 0; i < m_RGBValues.Length; i = i + 4)
                {
                    m_RGBValues[i] = Clr.B;
                    m_RGBValues[i + 1] = Clr.G;
                    m_RGBValues[i + 2] = Clr.R;
                    m_RGBValues[i + 3] = Clr.A;
                }
            }
            else
            {
                for (int i = 0; i < m_RGBValues.Length; i = i + 3)
                {
                    m_RGBValues[i] = Clr.B;
                    m_RGBValues[i + 1] = Clr.G;
                    m_RGBValues[i + 2] = Clr.R;
                }
            }
        }

        public void SetPixel(Point Location, Color Clr)
        {
            this.SetPixel(Location.X, Location.Y, Clr);
        }

        private void SetPixel(int X, int Y, Color Clr)
        {
            if (!m_Locked)
                throw new Exception("Bitmap not locked - fastpixel.cs!");

            if (m_IsAlpha)
            {
                int Index = ((Y * m_Width + X) * 4);
                m_RGBValues[Index] = Clr.B;
                m_RGBValues[Index + 1] = Clr.G;
                m_RGBValues[Index + 2] = Clr.R;
                m_RGBValues[Index + 3] = Clr.A;
            }
            else
            {
                int Index = ((Y * m_Width + X) * 3);
                m_RGBValues[Index] = Clr.B;
                m_RGBValues[Index + 1] = Clr.G;
                m_RGBValues[Index + 2] = Clr.R;
            }
        }

        public Color GetPixel(Point Location)
        {
            return GetPixel(Location.X, Location.Y);
        }

        private Color GetPixel(int X, int Y)
        {
            if (!m_Locked)
                throw new Exception("Bitmap not locked - fastpixel.cs!");

            if (m_IsAlpha)
            {
                int Index = ((Y * m_Width + X) * 4);
                int B = m_RGBValues[Index];
                int G = m_RGBValues[Index + 1];
                int R = m_RGBValues[Index + 2];
                int A = m_RGBValues[Index + 3];

                return Color.FromArgb(A, R, G, B);
            }
            else
            {
                int Index = ((Y * m_Width + X) * 3);
                int B = m_RGBValues[Index];
                int G = m_RGBValues[Index + 1];
                int R = m_RGBValues[Index + 2];

                return Color.FromArgb(R, G, B);
            }
        }
    }
}
