using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.UI.Framework
{
    /// <summary>
    /// Combines multiple vector fonts into a single font
    /// to better support px font sizes
    /// </summary>
    public class Font
    {
        private List<FontEntry> EntryList;

        public Font()
        {
            EntryList = new List<FontEntry>();
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
