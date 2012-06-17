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
using System.IO;
using System.Text;

namespace SimsLib.UTK
{
    public class UTKParams
    {
        public byte UnreadBitsValue, UnreadBitsCount;

        public int UseLattice;
        public uint NoiseFloor;
        public float[] FixedCodeBook; //Fixed codebook gain matrix
        public float[] ImpulseTrain;  //Impulse train matrix
        public float[] R;             //Autocorrelation coefficient matrix
        public float[] Delay;         

        public float[] DecompressedBlock;

        public UTKParams()
        {
            FixedCodeBook = new float[64];
            ImpulseTrain = new float[12];
            R = new float[12];
            Delay = new float[324];

            DecompressedBlock = new float[432];
        }
    }

    public class UTKFile
    {
        private BinaryReader m_Reader;
        private MemoryStream m_DecompressedStream;
        private BinaryWriter m_Writer;

        private string m_ID = "";        //A 4-byte string identifier equal to "UTM0".
        private uint m_DecompressedSize; //The decompressed size of the audio stream.
        private uint m_WaveFormatXSize;  //The size in bytes of the WAVEFORMATEX structure to follow; must be 20.

        //WAVEFORMATEX
        //The decoded audio format; set to WAVE_FORMAT_PCM (0x0001).
        private ushort m_FormatTag;
        //Number of channels in the decoded audio data.
        private ushort m_NumChannels;
        //Sampling rate used in the decoded audio data.
        private uint m_SamplesPerSec;
        //Bytes per second consumed by the decoded audio data; equal to 
        //nChannels*nSamplesPerSec*wBitsPerSample/8 or nSamplesPerSec*nBlockAlign
        private uint m_AvgBytesPerSec;
        //The number of bytes consumed by an audio frame (one sample for each channel) in the decoded audio data; 
        //equal to nChannels*wBitsPerSample/8.
        private ushort m_BlockAlign;
        //The bits per sample for one audio channel in the decoded audio data; 8-, 16-, or 24-bit, etc.
        private ushort m_BitsPerSample;
        //The size in bytes of extra format information appended to the end of the WAVEFORMATEX structure; 
        //must be 0. Note that in the original WAVEFORMATEX, this parameter is a WORD, not a DWORD.
        private uint m_AppendSize;

        private float[]      m_UTKTable1;
        private byte[]       m_UTKTable2;
        private byte[] m_UTKTable3 = {8,7,8,7,2,2,2,3,3,4,4,3,3,5,5,4,4,6,6,5,5,7,7,6,6,8,8,7,7};
        private float[]         m_UTKTable4;

        /// <summary>
        /// The decompressed wave data as a byte array.
        /// Will not contain any data unless DecompressFile() has been called!
        /// </summary>
        public byte[] DecompressedData
        {
            get { return m_DecompressedStream.ToArray(); }
        }

        private int Round(float X)
        {
            return (int)((X) >= 0 ? (X) + 0.5 : (X) - 0.5);
        }
         
        private void UTKGenerateTables()
        {
            m_UTKTable1 = new float[64];
            m_UTKTable2 = new byte[512];
            m_UTKTable4 = new float[29];

            /* UTKTable1 */
            m_UTKTable1[0] = 0;
            for(int i=-31; i<32; i++)
            {
                int s = (i>=0) ? 1 : -1;
                if     (s*i<14) m_UTKTable1[i+32] = i*.051587f;
                else if(s*i<25) m_UTKTable1[i+32] = i*.051587f/2 + s*.337503f;
                else            m_UTKTable1[i+32] = i*.051587f/8 + s*.796876f;
            }
         
            /* UTKTable2 */
            for(int i=0; i<512; i++)
            {
                switch(i%4){
                case 0: m_UTKTable2[i] = 4; break;
                case 1: m_UTKTable2[i] = (byte)((i < 256) ? 6 : (11 + Convert.ToInt32(i % 8 > 4))); break;
                case 2: m_UTKTable2[i] = (byte)((i < 256) ? 5 : (7 + Convert.ToInt32(i % 8 > 4))); break;
                case 3: 
                    byte[] l1 = {9,15,13,19,10,16};
                    byte[] l2 = {17,21,18,25,17,22,18,00,17,21,18,26,17,22,18,02,
                                    23,27,24,01,23,28,24,03,23,27,24,01,23,28,24,03};
                    if (i % 16 < 4) 
                        m_UTKTable2[i] = l1[0 + Convert.ToInt32(i > 256)];
                    else if (i % 16 < 8) 
                        m_UTKTable2[i] = (byte)(l1[2 + Convert.ToInt32(i > 256)] + Convert.ToInt32(i % 32 > 16));
                    else if (i % 16 < 12) 
                        m_UTKTable2[i] = l1[4 + Convert.ToInt32(i > 256)];
                    else 
                        m_UTKTable2[i] = l2[i / 16];
                    break;
                }
            }
         
            /* UTKTable4 */
            m_UTKTable4[0] = 0;
            for(int i=0; i<7; i++)
            {
                m_UTKTable4[4 * i + 1] = -i;
                m_UTKTable4[4 * i + 2] = +i;
                m_UTKTable4[4 * i + 3] = -i;
                m_UTKTable4[4 * i + 4] = +i;
            }
        }

        public UTKFile()
        {
            UTKGenerateTables();
        }

        /// <summary>
        /// Loads a *.utk file, setting things up for decompression.
        /// Should always be called before DecompressFile().
        /// </summary>
        /// <param name="Path">The path to the *.utk file to load.</param>
        public void LoadFile(string Path)
        {
            m_Reader = new BinaryReader(File.Open(Path, FileMode.Open));

            m_ID = new string(m_Reader.ReadChars(4));
            m_DecompressedSize = m_Reader.ReadUInt32();
            m_WaveFormatXSize = m_Reader.ReadUInt32();
            m_FormatTag = m_Reader.ReadUInt16();
            m_NumChannels = m_Reader.ReadUInt16();
            m_SamplesPerSec = m_Reader.ReadUInt32();
            m_AvgBytesPerSec = m_Reader.ReadUInt32();
            m_BlockAlign = m_Reader.ReadUInt16();
            m_BitsPerSample = m_Reader.ReadUInt16();
            m_AppendSize = m_Reader.ReadUInt32();

            m_DecompressedStream = new MemoryStream();
            m_Writer = new BinaryWriter(m_DecompressedStream);

            //Write wav-header.
            uint dwDataSize = m_DecompressedSize;
            uint dwFMTSize = 16;
            uint dwRIFFSize = 36 + dwDataSize;

            m_Writer.Write(new char[] { 'R', 'I', 'F', 'F' });
            m_Writer.Write(dwRIFFSize); //Size of file minus this field and the above field.
            m_Writer.Write(new char[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            m_Writer.Write(dwFMTSize); //Size of WAVEFORMATEX structure (all the data that comes after this field).
            m_Writer.Write(m_FormatTag);
            m_Writer.Write(m_NumChannels);
            m_Writer.Write(m_SamplesPerSec);
            m_Writer.Write(m_AvgBytesPerSec);
            m_Writer.Write(m_BlockAlign);
            m_Writer.Write(m_BitsPerSample);
            m_Writer.Write(new char[] { 'd', 'a', 't', 'a' });
            m_Writer.Write(dwDataSize);

            UTKParams P = new UTKParams();
            P.UnreadBitsValue = m_Reader.ReadByte();
            P.UnreadBitsCount = 8;

            P.UseLattice = (int)ReadBits(P, 1);
            P.NoiseFloor = (uint)32 - ReadBits(P, 4);
            P.FixedCodeBook[0] = (ReadBits(P, 4) + 1) * 8;

            float S = (float)ReadBits(P, 6) / 1000 + 1.04f;

            for (int i = 1; i < 64; i++)
                P.FixedCodeBook[i] = P.FixedCodeBook[i - 1] * S;

            //memset(p->ImpulseTrain, 0, 12*sizeof(float));
            Array.Clear(P.ImpulseTrain, 0, 12);

            //memset(p->R, 0, 12*sizeof(float));
            Array.Clear(P.R, 0, 12);

            //memset(p->Delay, 0, 324*sizeof(float));
            Array.Clear(P.Delay, 0, 324);

            uint Frames = (uint)(m_DecompressedSize / m_BlockAlign);

            UTKDecode(P, Frames);
        }

        private byte ReadBits(UTKParams P, byte NumBits)
        {
            byte ReturnValue = (byte)((255 >> (8 - NumBits)) & P.UnreadBitsValue);
            P.UnreadBitsValue >>= NumBits;
            P.UnreadBitsCount -= NumBits;

            try
            {
                if (P.UnreadBitsCount < 8)
                {
                    uint Value = (byte)m_Reader.ReadByte();
                    P.UnreadBitsValue |= (byte)(Value << P.UnreadBitsCount/* & 0xFF*/);
                    P.UnreadBitsCount += 8;
                }
            }
            catch (EndOfStreamException)
            {
                return ReturnValue;
            }

            return ReturnValue;
        }

        private void UTKDecode(UTKParams P, uint Frames)
        {
            while (Frames > 0)
            {
                uint BlockSize = (Frames > 432) ? 432 : Frames;
                DecompressBlock(ref P);

                for (uint i = 0; i < BlockSize; i++)
                {
                    int value = Round(P.DecompressedBlock[i]);

                    if (value < -32767)
                        value = 32767;
                    else if (value > 32768)
                        value = 32768;

                    //Writing out as byte seems to fix the filesize.
                    m_Writer.Write((byte)((value & 0x00FF/*u*/) >> (8 * 0)));
                    m_Writer.Write((byte)((value & 0xFF00/*u*/) >> (8 * 1)));
                }

                Frames -= BlockSize;
            }
        }

        private unsafe void DecompressBlock(ref UTKParams P)
        {
            float[] Window = new float[118];
            float[] Matrix = new float[12];
            int Voiced = 0;

            //memset(&Window[0], 0, 5*sizeof(float));
            Array.Clear(Window, 0, 5);

            //memset(&Window[113], 0, 5 * sizeof(float));
            Array.Clear(Window, 113, 5);

            for (int i = 0; i < 12; i++)
            {
                uint result = ReadBits(P, (byte)((i < 4) ? 6 : 5));
                if (i == 0 && P.NoiseFloor > result) Voiced++;
                Matrix[i] = (m_UTKTable1[result + ((i < 4) ? 0 : 16)] - P.ImpulseTrain[i]) / 4;
            }

            for (int i = 0; i < 4; i++)
            {
                float PitchGain, InnovationGain;
                int Phase = (int)ReadBits(P, 8);
                PitchGain = ((float)ReadBits(P, 4)) / 15;
                InnovationGain = P.FixedCodeBook[ReadBits(P, 6)];

                if (P.UseLattice == 0)
                {
                    fixed (float* Wnd = &Window[5])
                    {
                        LatticeFilter(ref P, Voiced, Wnd, 1);
                    }
                }
                else
                {
                    int o = ReadBits(P, 1); //Order
                    int y = ReadBits(P, 1);

                    fixed (float* Wnd = &Window[5 + o])
                    {
                        LatticeFilter(ref P, Voiced, Wnd, 2);
                    }

                    if (y != 0)
                    {
                        for (int j = 0; j < 108; j += 2)
                            Window[6 - o + j] = 0;
                    }
                    else
                    {
                        //Vector quantization
                        fixed (float* Wnd = &Window[6 - o])
                        {
                            float* Z = Wnd;
                            for (int j = 0; j < 54; j++, Z += 2)
                                *Z =
                                      (Z[-5] + Z[+5]) * .0180326793f
                                    - (Z[-3] + Z[+3]) * .1145915613f
                                    + (Z[-1] + Z[+1]) * .5973859429f;

                            InnovationGain /= 2;
                        }
                    }
                }

                //Excitation
                for (int j = 0; j < 108; j++)
                {
                    try
                    {
                        P.DecompressedBlock[108 * i + j] = InnovationGain * Window[5 + j] + PitchGain * P.Delay[216 - Phase + 108 * i + j];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        break;
                    }
                }
            }

            //Not sure about the last part of this statement...
            Array.Copy(P.Delay, 0, P.DecompressedBlock, 108, 324 /** sizeof(uint)*/);

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 12; j++)
                    P.ImpulseTrain[j] += Matrix[j];

                Synthesize(P, i * 12, (uint)((i != 3) ? 1 : 33));
            }
        }

        private unsafe void LatticeFilter(ref UTKParams P, int Voiced, float* Window, int Interval)
        {
            if (Voiced != 0)
            {
                int t = 0;
                int i = 0;

                while (i < 108)
                {
                    uint code = m_UTKTable2[(t << 8) | (P.UnreadBitsValue & 0xFF)];

                    t = (code < 2 || code > 8) ? 1 : 0;

                    ReadBits(P, m_UTKTable3[code]);

                    if (code >= 4)
                    {
                        Window[i] = m_UTKTable4[code];
                        i += Interval;
                    }
                    else
                    {
                        if (code > 1)
                        {
                            int x = (int)ReadBits(P, 6) + 7;
                            if (x > (108 - i) / Interval)
                                x = (108 - i) / Interval;

                            while(x > 0)
                            {
                                x--;
                                Window[i] = 0;
                                i += Interval;
                            }
                        }
                        else
                        {
                            Window[i] = 7;
                            while (ReadBits(P, 1) > 0)
                                Window[i]++;

                            if (ReadBits(P, 1) != 0)
                                Window[i] *= -1;

                            i += Interval;
                        }
                    }
                }
            }
            else
            {
                //Unvoiced signal; load noise
                int i;
                for (i = 0; i < 108; i += Interval)
                {
                    byte b;

                    switch (P.UnreadBitsValue & 3)
                    {
                        case 3:
                            Window[i] = 2.0f;
                            b = 2;
                            break;
                        case 1:
                            Window[i] = -2.0f;
                            b = 2;
                            break;
                        default:
                            Window[i] = 0.0f;
                            b = 1;
                            break;
                    }

                    ReadBits(P, b);
                }
            }
        }

        private unsafe void Synthesize(UTKParams P, int Sample, uint Blocks)
        {
            float[] Residual = new float[12];
            uint Samples = Blocks*12;
            int offset = -1;

            fixed (float* C64 = &P.ImpulseTrain[0])
            {
                PredictionFilter(C64, ref Residual);
            }
         
            while(Samples > 0)
            {
                Samples--;

                float x = P.DecompressedBlock[Sample];

                for (int i = 0; i < 12; i++)
                {
                    if (++offset == 12) offset = 0;
                    x += P.R[offset] * Residual[i];
                }

                P.R[offset--] = x;
                P.DecompressedBlock[Sample++] = x;
            }
        }

        private unsafe void PredictionFilter(float* C2, ref float[] Residual)
        {
            int j, k;
            float[] Matrix1 = new float[12];
            float[] Matrix2 = new float[12];
            Matrix2[0] = 1;

            for (int i = 1; i < 12; i++)
                Matrix2[i] = *C2++;

            for (j = 0; j < 12; j++)
            {
                float x = 0;
                for (k = 11; k >= 0; k--)
                {
                    x -= C2[k] * Matrix2[k];
                    if (k != 11)
                        Matrix2[k + 1] = x * C2[k] + Matrix2[k];
                }
                Matrix2[0] = x;
                Matrix1[j] = x;

                for (int l = 0; l < j; l++)
                    x -= Matrix1[j - l - 1] * Residual[l];

                Residual[j] = x;
            }
        }
    }
}
