using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Framework
{
    /// <summary>
    /// Combines multiple vector fonts into a single font
    /// to better support px font sizes
    /// </summary>
    public class Font
    {
        private List<FontEntry> EntryList;

        /// <summary>
        /// Default metrics for Sim font
        /// </summary>
        public int WinAscent = 1067;
        public int WinDescent = 279;
        public int CapsHeight = 730;
        public int XHeight = 516;
        public int UnderlinePosition = -132;
        public int UnderlineWidth = 85;

        public float BaselineOffset;

        public Font()
        {
            EntryList = new List<FontEntry>();
            ComputeMetrics();
        }

        public void ComputeMetrics()
        {
            BaselineOffset = (float)WinDescent / (float)(CapsHeight - WinDescent);
        }


        public FontEntry GetNearest(int pxSize)
        {
            return 
                EntryList.OrderBy(x => Math.Abs(pxSize - x.Size)).FirstOrDefault();
        }

        public void AddSize(int pxSize, SpriteFont font)
        {
            EntryList.Add(new FontEntry {
                Size = pxSize,
                Font = font
            });
        }
    }

    public class FontEntry {
        public int Size;
        public SpriteFont Font;
    }
}
