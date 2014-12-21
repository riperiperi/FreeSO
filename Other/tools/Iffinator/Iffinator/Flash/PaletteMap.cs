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
using System.IO;
using System.Drawing;

namespace Iffinator.Flash
{
    public class PaletteMap : IffChunk
    {
        private const int INDEX_PALTID = 1;
		private const int INDEX_PALTPX = 84;

        //private int[] m_Colors;
        private Color[] m_Colors;

        static int numPalettes;

        public PaletteMap(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            //Reader.BaseStream.Position = INDEX_PALTID;
            /*m_ID = (uint)numPalettes++;
            m_ID = Reader.ReadUInt32();*/
            
            //m_Colors = new int[256];
            m_Colors = new Color[256];

            Reader.BaseStream.Position = 16;
            /*if (Reader.BaseStream.ReadByte() == 0)
                Reader.BaseStream.Position = 80;
            else
                Reader.BaseStream.Position = 16;*/

            for (int i = 0; i < 256; i++)
            {
                //Reader.BaseStream.Position += 3;
                byte[] colors = new byte[] {};
                if ((Reader.BaseStream.Length - Reader.BaseStream.Position) >= 3)
                    m_Colors[i] = Color.FromArgb(Reader.ReadByte(), Reader.ReadByte(), Reader.ReadByte());
                else
                    m_Colors[i] = Color.FromArgb(255, 0x80, 0x80, 0x80);
            }

            Reader.Close();
        }

        /// <summary>
        /// Creates an all-black palettemap, used by a few sprites.
        /// </summary>
        public PaletteMap() : base("PMAP")
        {
            m_Colors = new Color[256];

            for(int i = 0; i < 256; i++)
                m_Colors[i] = Color.FromArgb(255, 0, 0, 0);
        }

        public Color GetColorAtIndex(int Index)
        {
            return m_Colors[Index];
        }
    }
}
