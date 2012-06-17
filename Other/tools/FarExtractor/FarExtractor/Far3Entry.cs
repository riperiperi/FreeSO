/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FarExtractor
{
    class Far3Entry
    {
        public uint DecompressedFileSize;
        public uint CompressedFileSize;
        public byte DataType;
        public uint DataOffset;
        public byte Compressed;
        public byte AccessNumber;
        //public ushort CompressedSpecifics;
        //public byte PowerValue;
        //public byte Unknown;
        //public ushort Unknown2;
        public ushort FilenameLength;
        public uint TypeID;
        public uint FileID;
        public string Filename;

        /*public int CalculateFileSize()
        {
            if (PowerValue == 0)
                return CompressedSpecifics;
            else if (PowerValue < 0)
                return (((PowerValue + 1) * 65536) + CompressedSpecifics);
            else if (PowerValue > 0)
                return ((PowerValue + 65536) + CompressedSpecifics);

            return 0;
        }*/
    }
}
