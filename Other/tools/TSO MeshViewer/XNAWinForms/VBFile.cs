/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is TSO Dressup.

The Initial Developer of the Original Code is
ddfzcsm. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Dressup
{
    /// <summary>
    /// Low-level filereader for reading files related to Vitaboy.
    /// </summary>
    public class VBReader : IDisposable
    {
        private Stream Stream;
        private BinaryReader Reader;

        public VBReader(Stream stream)
        {
            this.Stream = stream;
            this.Reader = new BinaryReader(stream);
        }

        public short ReadInt16()
        {
            return Endian.SwapInt16(Reader.ReadInt16());
        }

        public int ReadInt32()
        {
            return Endian.SwapInt32(Reader.ReadInt32());
        }

        public byte ReadByte()
        {
            return Reader.ReadByte();
        }

        public string ReadPascalString()
        {
            var length = ReadByte();
            return Encoding.ASCII.GetString(Reader.ReadBytes(length));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public virtual unsafe float ReadFloat()
        {
            var m_buffer = Reader.ReadBytes(4);
            uint tmpBuffer = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);

            var result = *((float*)&tmpBuffer);
            return result;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Reader.Close();
        }

        #endregion
    }
}
