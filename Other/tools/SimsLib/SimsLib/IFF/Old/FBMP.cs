/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace SimsLib.IFF
{
    /// <summary>
    /// A chunk that holds a regular Windows bitmap.
    /// Acts as a palettemap (PALT) for SPR and SPR2.
    /// </summary>
    public class FBMP : IffChunk
    {
        private Bitmap m_BitmapData;

        /// <summary>
        /// Creates a FBMP instance.
        /// </summary>
        /// <param name="Chunk">The data to create this FBMP instance from.</param>
        public FBMP(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);

            m_BitmapData = new Bitmap(MemStream);
        }
    }
}
