using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Iffinator.Flash
{
    class DrawGroupImg
    {
        private uint m_DirectionFlag;
        private uint m_Zoom;
        public Bitmap BitmapData;

        public DrawGroupImg(uint DirectionFlag, uint Zoom)
        {
            m_DirectionFlag = DirectionFlag;
            m_Zoom = Zoom;
        }
    }
}
