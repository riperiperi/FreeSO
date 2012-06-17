/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SimsLib.UTK
{
    /// <summary>
    /// Implements a wrapper for the original UTalk decompression code written by X-Fi6 (alias Fatbag).
    /// NOTE: A program that uses this wrapper HAS to be run as an administrator in Win7/Vista, or it will
    ///       cause an exception in the DLL for trying to access protected memory.
    /// </summary>
    public class UTKWrapper
    {
        private ushort m_FormatTag, m_NumChannels;
        private uint m_SamplesPerSec, m_AvgBytesPerSec;
        private ushort m_BlockAlign, m_BitsPerSample;
        private uint m_AppendSize;

        private uint m_Frames;      //How many frames the UTK-file consists of.
        private byte[] m_OutBuffer; //The buffer containing the decompressed UTK data.

        /// <summary>
        /// Returns the decompressed buffer plus a wav header, so that the UTalk
        /// sound can be played back as a wav-file.
        /// </summary>
        public byte[] Wav
        {
            get
            {
                MemoryStream MemStream = new MemoryStream();
                BinaryWriter Writer = new BinaryWriter(MemStream);

                Writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                Writer.Write((uint)(m_OutBuffer.Length + 36)); //Size of file minus this field and the above field.
                Writer.Write(new char[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                Writer.Write((uint)16); //Size of WAVEFORMATEX structure (all the data that comes after this field).
                Writer.Write(m_FormatTag);
                Writer.Write(m_NumChannels);
                Writer.Write(m_SamplesPerSec);
                Writer.Write(m_AvgBytesPerSec);
                Writer.Write(m_BlockAlign);
                Writer.Write(m_BitsPerSample);
                Writer.Write(new char[] { 'd', 'a', 't', 'a' });
                Writer.Write((uint)m_OutBuffer.Length);

                Writer.Write(m_OutBuffer);

                return MemStream.ToArray();
            }
        }

        public UTKWrapper()
        {
            m_OutBuffer = new byte[10];
        }

        public unsafe void LoadUTK(string Path)
        {
            BinaryReader Reader = new BinaryReader(File.Open(Path, FileMode.Open));

            string ID = new string(Reader.ReadChars(4));
            uint DecompressedSize = Reader.ReadUInt32();
            uint WaveFormatXSize = Reader.ReadUInt32();
            m_FormatTag = Reader.ReadUInt16();
            m_NumChannels = Reader.ReadUInt16();
            m_SamplesPerSec = Reader.ReadUInt32();
            m_AvgBytesPerSec = Reader.ReadUInt32();
            m_BlockAlign = Reader.ReadUInt16();
            m_BitsPerSample = Reader.ReadUInt16();
            m_AppendSize = Reader.ReadUInt32();

            m_Frames = DecompressedSize / m_BlockAlign;
            byte[] InBuffer = Reader.ReadBytes((int)(Reader.BaseStream.Length - 36));
            m_OutBuffer = new byte[DecompressedSize];

            fixed (byte* InBufferPtr = &InBuffer[0])
            {
                fixed (byte* OutBufferPtr = &m_OutBuffer[0])
                    UTKFunctions.utk_decode(InBufferPtr, OutBufferPtr, m_Frames);
            }
        }
    }
}
