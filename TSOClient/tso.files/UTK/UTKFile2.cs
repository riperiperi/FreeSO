using System;
using System.IO;

namespace FSO.Files.UTK
{
    /// <summary>
    /// Represents a *.UTK file.
    /// It is used to store compressed wav data.
    /// </summary>
    public class UTKFile2
    {
        public float[] UTKCosine = {
            0.0f, -.99677598476409912109375f, -.99032700061798095703125f, -.983879029750823974609375f, -.977430999279022216796875f,
            -.970982015132904052734375f, -.964533984661102294921875f, -.958085000514984130859375f, -.9516370296478271484375f,
            -.930754005908966064453125f, -.904959976673126220703125f, -.879167020320892333984375f, -.853372991085052490234375f,
            -.827579021453857421875f, -.801786005496978759765625f, -.775991976261138916015625f, -.75019800662994384765625f,
            -.724404990673065185546875f, -.6986110210418701171875f, -.6706349849700927734375f, -.61904799938201904296875f,
            -.567460000514984130859375f, -.515873014926910400390625f, -.4642859995365142822265625f, -.4126980006694793701171875f,
            -.361110985279083251953125f, -.309523999691009521484375f, -.257937014102935791015625f, -.20634900033473968505859375f,
            -.1547619998455047607421875f, -.10317499935626983642578125f, -.05158700048923492431640625f,
            0.0f,
            +.05158700048923492431640625f, +.10317499935626983642578125f, +.1547619998455047607421875f, +.20634900033473968505859375f,
            +.257937014102935791015625f, +.309523999691009521484375f, +.361110985279083251953125f, +.4126980006694793701171875f,
            +.4642859995365142822265625f, +.515873014926910400390625f, +.567460000514984130859375f, +.61904799938201904296875f,
            +.6706349849700927734375f, +.6986110210418701171875f, +.724404990673065185546875f, +.75019800662994384765625f,
            +.775991976261138916015625f, +.801786005496978759765625f, +.827579021453857421875f, +.853372991085052490234375f,
            +.879167020320892333984375f, +.904959976673126220703125f, +.930754005908966064453125f, +.9516370296478271484375f,
            +.958085000514984130859375f, +.964533984661102294921875f, +.970982015132904052734375f, +.977430999279022216796875f,
            +.983879029750823974609375f, +.99032700061798095703125f, +.99677598476409912109375f
        };

        private byte[] UTKCodebook = {
            4,  6,  5,  9,  4,  6,  5, 13,  4,  6,  5, 10,  4,  6,  5, 17,
            4,  6,  5,  9,  4,  6,  5, 14,  4,  6,  5, 10,  4,  6,  5, 21,
            4,  6,  5,  9,  4,  6,  5, 13,  4,  6,  5, 10,  4,  6,  5, 18,
            4,  6,  5,  9,  4,  6,  5, 14,  4,  6,  5, 10,  4,  6,  5, 25,
            4,  6,  5,  9,  4,  6,  5, 13,  4,  6,  5, 10,  4,  6,  5, 17,
            4,  6,  5,  9,  4,  6,  5, 14,  4,  6,  5, 10,  4,  6,  5, 22,
            4,  6,  5,  9,  4,  6,  5, 13,  4,  6,  5, 10,  4,  6,  5, 18,
            4,  6,  5,  9,  4,  6,  5, 14,  4,  6,  5, 10,  4,  6,  5,  0,
            4,  6,  5,  9,  4,  6,  5, 13,  4,  6,  5, 10,  4,  6,  5, 17,
            4,  6,  5,  9,  4,  6,  5, 14,  4,  6,  5, 10,  4,  6,  5, 21,
            4,  6,  5,  9,  4,  6,  5, 13,  4,  6,  5, 10,  4,  6,  5, 18,
            4,  6,  5,  9,  4,  6,  5, 14,  4,  6,  5, 10,  4,  6,  5, 26,
            4,  6,  5,  9,  4,  6,  5, 13,  4,  6,  5, 10,  4,  6,  5, 17,
            4,  6,  5,  9,  4,  6,  5, 14,  4,  6,  5, 10,  4,  6,  5, 22,
            4,  6,  5,  9,  4,  6,  5, 13,  4,  6,  5, 10,  4,  6,  5, 18,
            4,  6,  5,  9,  4,  6,  5, 14,  4,  6,  5, 10,  4,  6,  5,  2,
            4, 11,  7, 15,  4, 12,  8, 19,  4, 11,  7, 16,  4, 12,  8, 23,
            4, 11,  7, 15,  4, 12,  8, 20,  4, 11,  7, 16,  4, 12,  8, 27,
            4, 11,  7, 15,  4, 12,  8, 19,  4, 11,  7, 16,  4, 12,  8, 24,
            4, 11,  7, 15,  4, 12,  8, 20,  4, 11,  7, 16,  4, 12,  8,  1,
            4, 11,  7, 15,  4, 12,  8, 19,  4, 11,  7, 16,  4, 12,  8, 23,
            4, 11,  7, 15,  4, 12,  8, 20,  4, 11,  7, 16,  4, 12,  8, 28,
            4, 11,  7, 15,  4, 12,  8, 19,  4, 11,  7, 16,  4, 12,  8, 24,
            4, 11,  7, 15,  4, 12,  8, 20,  4, 11,  7, 16,  4, 12,  8,  3,
            4, 11,  7, 15,  4, 12,  8, 19,  4, 11,  7, 16,  4, 12,  8, 23,
            4, 11,  7, 15,  4, 12,  8, 20,  4, 11,  7, 16,  4, 12,  8, 27,
            4, 11,  7, 15,  4, 12,  8, 19,  4, 11,  7, 16,  4, 12,  8, 24,
            4, 11,  7, 15,  4, 12,  8, 20,  4, 11,  7, 16,  4, 12,  8,  1,
            4, 11,  7, 15,  4, 12,  8, 19,  4, 11,  7, 16,  4, 12,  8, 23,
            4, 11,  7, 15,  4, 12,  8, 20,  4, 11,  7, 16,  4, 12,  8, 28,
            4, 11,  7, 15,  4, 12,  8, 19,  4, 11,  7, 16,  4, 12,  8, 24,
            4, 11,  7, 15,  4, 12,  8, 20,  4, 11,  7, 16,  4, 12,  8,  3
        };

        private byte[] m_UTKCodeSkips = { 8, 7, 8, 7, 2, 2, 2, 3, 3, 4, 4, 3, 3, 5, 5, 4, 4, 6, 6, 5, 5, 7, 7, 6, 6, 8, 8, 7, 7 };

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

        private MemoryStream m_DecompressedStream;
        private BinaryReader m_Reader;
        private BinaryWriter m_Writer;
        private int m_UnreadBitsValue, m_UnreadBitsCount;
        private bool m_HalvedExcitation;
        private byte m_VoicedThreshold;
        private float[] m_InnovationPower = new float[64];
        private float[] m_RC = new float[12];
        private float[] m_History = new float[12];
        private float[] m_DecompressedFrame = new float[756];

        /// <summary>
        /// Returns a decompressed wav stream.
        /// </summary>
        public byte[] DecompressedWav
        {
            get { return m_DecompressedStream.ToArray(); }
        }

        /// <summary>
        /// Creates a new UTKFile2 instance.
        /// </summary>
        /// <param name="FileData">The file data to read from.</param>
        public UTKFile2(byte[] FileData)
        {
            m_DecompressedStream = new MemoryStream();
            m_Reader = new BinaryReader(new MemoryStream(FileData));

            ReadHeader();
        }

        /// <summary>
        /// Creates a new UTKFile2 instance.
        /// </summary>
        /// <param name="Filepath">The path to a *.utk file to read from.</param>
        public UTKFile2(string Filepath)
        {
            m_DecompressedStream = new MemoryStream();
            m_Reader = new BinaryReader(File.Open(Filepath, FileMode.Open, FileAccess.Read, FileShare.Read));

            ReadHeader();
        }

        /// <summary>
        /// Reads the header of a .utk file.
        /// </summary>
        private void ReadHeader()
        {
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

            m_UnreadBitsValue = m_Reader.ReadByte();
            m_UnreadBitsCount = 8;
            m_HalvedExcitation = (ReadBits(1) != 0);
            m_VoicedThreshold = (byte)(32 - ReadBits(4));

            m_InnovationPower[0] = (ReadBits(4) + 1) * 8; //Significand.

            float Base = 1.04f + (float)(ReadBits(6)) / 1000.0f;
            for (int i = 1; i < 64; i++)
                m_InnovationPower[i] = m_InnovationPower[i - 1] * Base;
        }

        private byte ReadBits(byte Bits)
        {
            byte Value = (byte)(m_UnreadBitsValue & (255 >> (8 - Bits)));
            m_UnreadBitsValue >>= Bits;
            m_UnreadBitsCount -= Bits;

            if ((m_UnreadBitsCount < 8) && (m_Reader.BaseStream.Position < m_Reader.BaseStream.Length))
            {
                m_UnreadBitsValue |= m_Reader.ReadByte() << m_UnreadBitsCount;
                m_UnreadBitsCount += 8;
            }

            return Value;
        }

        /// <summary>
        /// Finds the lesser of two ints.
        /// </summary>
        /// <param name="A">The first value.</param>
        /// <param name="B">The second value.</param>
        /// <returns>The lesser of the two ints.</returns>
        private int Lesser(int A, int B)
        {
            return (A < B ? A : B);
        }

        /// <summary>
        /// Clamps a value between an upper and lower bound.
        /// </summary>
        /// <typeparam name="T">The type of the value to clamp.</typeparam>
        /// <param name="value">The value to clamp.</param>
        /// <param name="max">Upper bound.</param>
        /// <param name="min">Lower bound.</param>
        /// <returns></returns>
        public static T Clamp<T>(T value, T max, T min)
         where T : System.IComparable<T>
        {
            T result = value;
            if (value.CompareTo(max) < 0)
                result = max;
            if (value.CompareTo(min) > 0)
                result = min;
            return result;
        }

        /// <summary>
        /// Decodes the UTK data in this file.
        /// </summary>
        public void UTKDecode()
        {
            uint Frames = m_DecompressedSize / m_BlockAlign;

            while (Frames > 0)
            {
                int BlockSize = Lesser((int)Frames, 432);
                DecodeFrame();

                for (int i = 0; i < BlockSize; i++)
                {
                    int Value = (int)Math.Round(m_DecompressedFrame[324 + i]);
                    Value = Clamp<int>(Value, -32768, 32767);
                    m_Writer.Write((ushort)Value);
                }

                Frames -= (uint)BlockSize;
            }

            m_Writer.Flush();

            m_Reader.Close();
        }

        private void DecodeFrame()
        {
            float[] Excitation = new float[118]; //includes 5 0-valued samples to both the left and the right.
            float[] RCDelta = new float[12];
            bool Voiced = false;

            for (int i = 0; i < 12; i++)
            {
                byte index = ReadBits((byte)((i < 4) ? 6 : 5));

                if (i == 0 && index < m_VoicedThreshold)
                    Voiced = true;

                RCDelta[i] = (UTKCosine[index + ((i < 4) ? 0 : 16)] - m_RC[i]) / 4.0f;
            }

            for (int i = 0; i < 4; i++)
            {
                int Phase = ReadBits(8);

                if (i == 0 && Phase > 216)
                    Phase = 216;

                float PitchGain = (float)(ReadBits(4)) / 15.0f;
                float InnovationGain = m_InnovationPower[ReadBits(6)];

                if (m_HalvedExcitation == false)
                {
                    GenerateExcitation(5, ref Excitation, Voiced, 1);
                }
                else
                {
                    //Fill the excitation window with half as many samples and interpolate the rest.
                    int Alignment = ReadBits(1); //whether to fill the even or odd samples.
                    bool FillWithZero = (ReadBits(1) != 0);
                    int Offset = 5 + (1 - Alignment);

                    GenerateExcitation(5 + Alignment, ref Excitation, Voiced, 2);

                    if (FillWithZero)
                    {
                        for (int j = Offset; j < Offset + 108; j += 2)
                            Excitation[j] = 0.0f;
                    }
                    else
                    {
                        //Use sinc interpolation with 6 neighboring samples.
                        for (int j = Offset; j < Offset + 108; j += 2)
                            Excitation[j] =
                              (Excitation[j - 1] + Excitation[j + 1]) * .5973859429f
                            - (Excitation[j - 3] + Excitation[j + 3]) * .1145915613f
                            + (Excitation[j - 5] + Excitation[j + 5]) * .0180326793f;

                        InnovationGain /= 2.0f;
                    }
                }

                for (int j = 0; j < 108; j++)
                    m_DecompressedFrame[324 + 108 * i + j] = InnovationGain * Excitation[5 + j] + PitchGain * m_DecompressedFrame[108 * i + j + (216 - Phase)];
            }

            Array.Copy(m_DecompressedFrame, 324 + 108, m_DecompressedFrame, 0, 324);

            for (int i = 0; i < 4; i++)
            {
                //Linearly interpolate the reflection coefficients for the current subframe.
                for (int j = 0; j < 12; j++)
                    m_RC[j] += RCDelta[j];

                Synthesize(i * 12, (i != 3) ? 12 : 396);
            }
        }

        private void GenerateExcitation(int Offset, ref float[] Excitation, bool Voiced, int Interval)
        {
            if (Voiced)
            {
                int Table = 0;
                int i = Offset;

                while (i < Offset + 108)
                {
                    byte code = UTKCodebook[(Table << 8) | (m_UnreadBitsValue & 0xFF)];
                    //Table = (code < 2 || code > 8);
                    Table = (code < 2 || code > 8) ? 1 : 0;
                    ReadBits(m_UTKCodeSkips[code]);

                    if (code >= 4)
                    {
                        //Fill a sample with a value specified by the code; magnitude is limited to 6.0
                        Excitation[i] = (code - 1) / 4;
                        if ((code & 1) != 0)
                            Excitation[i] *= -1.0f;

                        i += Interval;
                    }
                    else if (code >= 2)
                    {
                        //Fill between 7 and 70 samples with 0s
                        int x = ReadBits(6) + 7;
                        x = Lesser(x, (Offset + 108 - i) / Interval);

                        while (x > 0)
                        {
                            Excitation[i] = 0.0f;

                            i += Interval;
                            x--;
                        }
                    }
                    else
                    {
                        //Fill a sample with a custom value with magnitude >= 7.0
                        Excitation[i] = 7.0f;
                        while (ReadBits(1) != 0)
                            Excitation[i]++;

                        if (ReadBits(1) == 0)
                            Excitation[i] *= -1.0f;

                        i += Interval;
                    }
                }
            }
            else
            {
                //Unvoiced: restrict all samples to 0.0, -2.0, or +2.0 without using the codebook
                for (int i = Offset; i < Offset + 108; i += Interval)
                {
                    if (ReadBits(1) == 0) Excitation[i] = 0.0f;
                    else if (ReadBits(1) == 0) Excitation[i] = -2.0f;
                    else Excitation[i] = 2.0f;
                }
            }
        }

        private void Synthesize(int Sample, int Samples)
        {
            float[] LPC = new float[12];
            int offset = -1;
            RCtoLPC(ref m_RC, ref LPC);

            while (Samples > 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    if (++offset == 12) offset = 0;
                    m_DecompressedFrame[324 + Sample] += LPC[i] * m_History[offset];
                }

                m_History[offset--] = m_DecompressedFrame[324 + Sample++];
                Samples--;
            }
        }

        private void RCtoLPC(ref float[] RC, ref float[] LPC)
        {
            int i, j;
            float[] RCTemp = new float[12], LPCTemp = new float[12];
            RCTemp[0] = 1.0f;
            Array.Copy(RC, 0, RCTemp, 1, 11);

            for (i = 0; i < 12; i++)
            {
                LPC[i] = 0.0f;

                for (j = 11; j >= 0; j--)
                {
                    LPC[i] -= RC[j] * RCTemp[j];
                    if (j != 11)
                        RCTemp[j + 1] = RCTemp[j] + RC[j] * LPC[i];
                }

                RCTemp[0] = LPCTemp[i] = LPC[i];

                for (j = 0; j < i; j++)
                    LPC[i] -= LPCTemp[i - j - 1] * LPC[j];
            }
        }
    }
}