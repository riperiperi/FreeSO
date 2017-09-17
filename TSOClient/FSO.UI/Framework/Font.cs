/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public FontEntry GetIdeal(int pxSize)
        {
            return EntryList.First(x => x.Size == pxSize);
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
