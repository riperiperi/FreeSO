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
using System.IO;

namespace TSO.Files.formats.iff
{
    /// <summary>
    /// An IFF is made up of chunks.
    /// </summary>
    public abstract class IffChunk 
    {
        public ushort ChunkID;
        public ushort ChunkFlags;
        public string ChunkLabel;
        public bool ChunkProcessed;
        public byte[] ChunkData;
        public Iff ChunkParent;

        /// <summary>
        /// Reads this chunk from an IFF.
        /// </summary>
        /// <param name="iff">The IFF to read from.</param>
        /// <param name="stream">The stream to read from.</param>
        public abstract void Read(Iff iff, Stream stream);

        /// <summary>
        /// The name of this chunk.
        /// </summary>
        /// <returns>The name of this chunk as a string.</returns>
        public override string ToString()
        {
            return "#" + ChunkID.ToString() + " " + ChunkLabel;
        }
    }
}
