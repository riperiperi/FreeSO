/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

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
