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
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace SimsLib._3D
{
    public struct Light
    {
        private uint m_Offset, m_Width, m_Height;

        public uint Offset
        {
            get { return m_Offset; }
        }

        public uint Width
        {
            get { return m_Width; }
        }

        public uint Height
        {
            get { return m_Height; }
        }

        public Light(uint Offset, uint Width, uint Height)
        {
            m_Offset = Offset;
            m_Width = Width;
            m_Height = Height;
        }
    }

    public class Lightmaps
    {
        private byte[] m_Data;
        private readonly Light[] m_Lights = new Light[] { new Light(0, 320, 232), new Light(74240, 320, 232),
                                                       new Light(148480, 256, 232), new Light(207872, 192, 232),
                                                       new Light(252416,  16, 2220), new Light(287936,  32, 4440),
                                                       new Light(430016,  64, 8880), new Light(1744256, 224, 112),
                                                       new Light(1769344, 448, 224), new Light(1869696, 896, 448) };

        public Lightmaps(byte[] Data)
        {
            if (Data.Length != 2271104)
                throw new Exception("Invalid 'lightmap.dat' - Lightmap.cs!");

            m_Data = Data;
        }

        /// <summary>
        /// Returns the specified lightmap as a bitmap (in the form of an array of bytes).
        /// </summary>
        /// <param name="Index">The index of a lightmap (0 - 9).</param>
        /// <returns>The bitmapdata of the lightmap + a bitmap header as an array of bytes.</returns>
        public byte[] GetLightmap(int Index)
        {
            MemoryStream ReturnStream = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(ReturnStream);

            BinaryReader Reader = new BinaryReader(new MemoryStream(m_Data));
            Reader.BaseStream.Seek(m_Lights[Index].Offset, SeekOrigin.Begin);

            Writer.Write('B');
            Writer.Write('M');
            Writer.Write((uint)(m_Lights[Index].Width * m_Lights[Index].Height * 3 + 54));
            Writer.Write((uint)0x00);                                                      //Reserved          never changes
            Writer.Write(BitConverter.ToUInt32(new byte[] { 0x36, 0x00, 0x00, 0x00 }, 0)); //bfOffbits         never changes
            Writer.Write(BitConverter.ToUInt32(new byte[] { 0x28, 0x00, 0x00, 0x00 }, 0)); //biSize            never changes
            Writer.Write((uint)m_Lights[Index].Width);                                     //biWidth
            Writer.Write((uint)m_Lights[Index].Height);                                    //biHeight
            Writer.Write(BitConverter.ToUInt16(new byte[] { 0x01, 0x00 }, 0));             //biPlanes          never changes
            Writer.Write(BitConverter.ToUInt16(new byte[] { 0x18, 0x00 }, 0));             //never changes
            Writer.Write((uint)0x00);                                                      //biCompression     never changes
            Writer.Write((uint)(m_Lights[Index].Width * m_Lights[Index].Height * 3));      //biSizeImage
            Writer.Write(BitConverter.ToUInt32(new byte[] { 0x12, 0x0B, 0x00, 0x00 }, 0)); //biXPelsPerMeter   never changes
            Writer.Write(BitConverter.ToUInt32(new byte[] { 0x12, 0x0B, 0x00, 0x00 }, 0)); //biYPelsPerMeter   never changes
            Writer.Write((uint)0x00);                                                      //biClrUsed         never changes
            Writer.Write((uint)0x00);                                                      //biClrImportant    never changes

            for (int i = 0; i < m_Lights[Index].Width * m_Lights[Index].Height; i++)
            {
                for (int j = 0; j < 3; j++)
                    Writer.Write(Reader.ReadByte());
            }

            Reader.Close();

            return ReturnStream.ToArray();
        }
    }
}
