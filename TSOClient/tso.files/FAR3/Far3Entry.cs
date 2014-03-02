/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
        //A 4-byte unsigned integer specifying the uncompressed size of the file.
        public uint DecompressedFileSize;
        // A 3-byte unsigned integer specifying the compressed size of the file (including 
        //the Persist header); if the data is raw, this field is ignored (though TSO's game 
        //files have this set to the same first three bytes as the previous field).
        public uint CompressedFileSize;
        //Data type - A single byte used to describe what type of data is pointed to by the Data offset field. 
        //The value can be 0x80 to denote that the data is a Persist container or 0x00 to denote that it is raw data.
        public byte DataType;
        //A 4-byte unsigned integer specifying the offset of the file from the beginning of the archive.
        public uint DataOffset;
        //A 2-byte unsigned integer set to 0 or 1 specifying whether or not this file has a filename.
        //public ushort HasFilename;
        public byte IsCompressed;
        public byte AccessNumber;
        //A 2-byte unsigned integer specifying the length of the filename field.
        public ushort FilenameLength;
        //A 4-byte integer describing what type of file is held.
        public uint TypeID;
        //A 4-byte ID assigned to the file which, together with the Type ID, is assumed to be unique all throughout the game.
        public uint FileID;
        //The name of the archived file; size depends on the filename length field.
        public string Filename;
    }
}
