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
using System.IO;
using System.Drawing;

namespace SimsLib.IFF
{
    /// <summary>
    /// Represents a BMP_ chunk.
    /// A BMP_ chunk is like a regular bitmap file.
    /// </summary>
    class BMP_ : IffChunk
    {
        private Bitmap m_BitmapData;

        /// <summary>
        /// Creates a new BMP_ file.
        /// </summary>
        /// <param name="Chunk">The chunk to create the BMP_ file from.</param>
        public BMP_(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);

            m_BitmapData = new Bitmap(MemStream);
        }
    }
}
