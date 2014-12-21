/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FarExtractor
{
    public class Decompresser
    {
        private long m_CompressedSize = 0;
        private long m_DecompressedSize = 0;
        private bool m_Compressed = false;

        public long DecompressedSize
        {
            get { return m_DecompressedSize; }
            set { m_DecompressedSize = value; }
        }

        public long CompressedSize
        {
            get { return m_CompressedSize; }
            set { m_CompressedSize = value; }
        }

        /// <summary>
        ///  Copies data from source to destination array.<br>
	    ///  The copy is byte by byte from srcPos to destPos and given length.
        /// </summary>
        /// <param name="Src">The source array.</param>
        /// <param name="SrcPos">The source Position.</param>
        /// <param name="Dest">The destination array.</param>
        /// <param name="DestPos">The destination Position.</param>
        /// <param name="Length">The length.</param>
        private void ArrayCopy2(byte[] Src, int SrcPos, ref byte[] Dest, int DestPos, long Length)
        {
            if (Dest.Length < DestPos + Length)
            {
                byte[] DestExt = new byte[(int)(DestPos + Length)];
                Array.Copy(Dest, 0, DestExt, 0, Dest.Length);
                Dest = DestExt;
            }

            for (int i = 0; i < Length/* - 1*/; i++)
            {
                /*if (SrcPos == Src.Length || (SrcPos + i) == Src.Length)
                    break;*/

                Dest[DestPos + i] = Src[SrcPos + i];
            }
        }

        /// <summary>
        /// Copies data from array at destPos-srcPos to array at destPos.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="srcPos">The Position to copy from (reverse from end of array!)</param>
        /// <param name="destPos">The Position to copy to.</param>
        /// <param name="length">The length of data to copy.</param>
        private void OffsetCopy(ref byte[] array, int srcPos, int destPos, long length)
        {
            srcPos = destPos - srcPos;

            if (array.Length < destPos + length)
            {
                byte[] NewArray = new byte[(int)(destPos + length)];
                Array.Copy(array, 0, NewArray, 0, array.Length);
                array = NewArray;
            }

            for (int i = 0; i < length /*- 1*/; i++)
            {
                array[destPos + i] = array[srcPos + i];
            }
        }

        public byte[] Decompress(byte[] Data)
        {
            m_Compressed = false;

            MemoryStream MemData = new MemoryStream(Data);
            BinaryReader Reader = new BinaryReader(MemData);

            if (Data.Length > 6)
            {
                /*m_CompressedSize = Reader.ReadUInt32();
                int Signature = Reader.ReadUInt16();

                //If the data isn't compressed.
                m_DecompressedSize = m_CompressedSize;

                if (Signature == 0xFB10)
                {
                    short A = (short)Reader.ReadByte();
                    short B = (short)Reader.ReadByte();
                    short C = (short)Reader.ReadByte();
                    m_DecompressedSize = A * 65536 + B * 256 + C;*/

                    byte[] DecompressedData = new byte[(int)m_DecompressedSize];
                    int DataPos = 0;

                    m_Compressed = true;
                    //int Pos = 9;
                    int Pos = 0;
                    long Control1 = 0;

                    while (Control1 != 0xFC && Pos < Data.Length)
                    {
                        Control1 = Data[Pos];
                        Pos++;

                        if (Pos == Data.Length)
                            break;

                        if (Control1 >= 0 && Control1 <= 127)
                        {
                            // 0x00 - 0x7F
                            long control2 = Data[Pos];
                            Pos++;
	                        long numberOfPlainText = (Control1 & 0x03);
	                        ArrayCopy2(Data, Pos, ref DecompressedData, DataPos, numberOfPlainText);
                            DataPos += (int)numberOfPlainText;
	                        Pos += (int)numberOfPlainText;

                            if (DataPos == (DecompressedData.Length))
                                break;
	
	                        int offset = (int) (((Control1 & 0x60) << 3) + (control2) + 1);
	                        long numberToCopyFromOffset = ((Control1 & 0x1C) >> 2) + 3;
	                        OffsetCopy(ref DecompressedData, offset, DataPos, numberToCopyFromOffset);
	                        DataPos += (int)numberToCopyFromOffset;

                            if (DataPos == (DecompressedData.Length))
                                break;
                        }
                        else if ((Control1 >= 128 && Control1 <= 191))
                        {
                            // 0x80 - 0xBF
                            long control2 = Data[Pos];
                            Pos++;
                            long control3 = Data[Pos];
                            Pos++;

                            long numberOfPlainText = (control2 >> 6) & 0x03;
                            ArrayCopy2(Data, Pos, ref DecompressedData, DataPos, numberOfPlainText);
                            DataPos += (int)numberOfPlainText;
                            Pos += (int)numberOfPlainText;

                            if (DataPos == (DecompressedData.Length))
                                break;

                            int offset = (int)(((control2 & 0x3F) << 8) + (control3) + 1);
                            long numberToCopyFromOffset = (Control1 & 0x3F) + 4;
                            OffsetCopy(ref DecompressedData, offset, DataPos, numberToCopyFromOffset);
                            DataPos += (int)numberToCopyFromOffset;

                            if (DataPos == (DecompressedData.Length))
                                break;
                        }
                        else if (Control1 >= 192 && Control1 <= 223) 
                        {
                            // 0xC0 - 0xDF
                            long numberOfPlainText = (Control1 & 0x03);
                            long control2 = Data[Pos];
                            Pos++;
                            long control3 = Data[Pos];
                            Pos++;
                            long control4 = Data[Pos];
                            Pos++;
                            ArrayCopy2(Data, Pos, ref DecompressedData, DataPos, numberOfPlainText);
                            DataPos += (int)numberOfPlainText;
                            Pos += (int)numberOfPlainText;

                            if (DataPos == (DecompressedData.Length))
                                break;
	
	                        int offset = (int) (((Control1 & 0x10) << 12) + (control2 << 8) + (control3) + 1);
	                        long numberToCopyFromOffset = ((Control1 & 0x0C) << 6) + (control4) + 5;
	                        OffsetCopy(ref DecompressedData, offset, DataPos, numberToCopyFromOffset);
	                        DataPos += (int)numberToCopyFromOffset;

                            if (DataPos == (DecompressedData.Length))
                                break;
	                    }
                        else if (Control1 >= 224 && Control1 <= 251)
                        {
	                        // 0xE0 - 0xFB
	                        long numberOfPlainText = ((Control1 & 0x1F) << 2) + 4;
	                        ArrayCopy2(Data, Pos, ref DecompressedData, DataPos, numberOfPlainText);
	                        DataPos += (int)numberOfPlainText;
	                        Pos += (int)numberOfPlainText;

                            if (DataPos == (DecompressedData.Length))
                                break;
                        } 
                        else
                        {
	                        long numberOfPlainText = (Control1 & 0x03);
	                        ArrayCopy2(Data, Pos, ref DecompressedData, DataPos, numberOfPlainText);

	                        DataPos += (int)numberOfPlainText;
	                        Pos += (int)numberOfPlainText;

                            if (DataPos == (DecompressedData.Length))
                                break;
	                    }
                    }

                    return DecompressedData;
                }
            //}

            //No data to decompress
            return Data;
        }
    }
}
