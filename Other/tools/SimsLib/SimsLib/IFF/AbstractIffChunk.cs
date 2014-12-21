/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SimsLib.IFF
{
    public abstract class AbstractIffChunk
    {
        public ushort ChunkID;
        public ushort ChunkFlags;
        public string ChunkLabel;
        public bool ChunkProcessed;
        public byte[] ChunkData;
        public Iff ChunkParent;

        public abstract void Read(Iff iff, Stream stream);

        public override string ToString()
        {
            return "#" + ChunkID + " " + ChunkLabel;
        }
    }
}