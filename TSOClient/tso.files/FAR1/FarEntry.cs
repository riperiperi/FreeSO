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

namespace SimsLib.FAR1
{
    /// <summary>
    /// Represents an entry in a FAR1 archive.
    /// </summary>
    public class FarEntry
    {
        //Decompressed data size - A 4-byte unsigned integer specifying the uncompressed size of the file.
        public int DataLength;
        //A 4-byte unsigned integer specifying the compressed size of the file; if this and the previous field are the same, 
        //the file is considered uncompressed. (It is the responsibility of the archiver to only store data compressed when 
        //its size is less than the size of the original data.) Note that The Sims 1 does not actually support any form 
        //of compression.
        public int DataLength2;
        //A 4-byte unsigned integer specifying the offset of the file from the beginning of the archive.
        public int DataOffset;
        //A 4-byte unsigned integer specifying the length of the filename field that follows.
        public short FilenameLength;
        //Filename - The name of the archived file; size depends on the previous field.
        public string Filename;
    }
}
