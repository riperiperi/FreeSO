/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): Nicholas Roth.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace SimsLib.FAR3
{
    /// <summary>
    /// Represents an entry in a FAR3 archive.
    /// </summary>
    public class Far3Entry
    {
        public uint DecompressedFileSize;
        public uint CompressedFileSize;
        public byte DataType;
        public uint DataOffset;
        public byte Compressed;
        public byte AccessNumber;
        public ushort FilenameLength;
        public uint TypeID;
        public uint FileID;
        public string Filename;
    }
}
