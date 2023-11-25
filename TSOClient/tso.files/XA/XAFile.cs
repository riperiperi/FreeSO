using System.IO;

namespace FSO.Files.XA
{
    public enum SoundType
    {
        Mono = 0x00,
        Stereo = 0x01
    }

    /// <summary>
    /// Represents a *.xa file.
    /// It is used to store compressed wav data.
    /// </summary>
    public class XAFile
    {
        private int m_CurSampleLeft = 0;
        private int m_PrevSampleLeft = 0;

        private int m_CurSampleRight = 0;
        private int m_PrevSampleRight = 0;

        BinaryReader m_Reader;
        BinaryWriter m_Writer;
        MemoryStream m_DecompressedStream;

        // Note that the part of the header from (wTag) until 
        //(wBits) is really WAVEFORMATEX structure (the contents of PCM .WAV fmt chunk). 
        private string m_ID = "";        //string ID, which is equal to "XAI\0" (sound/speech) or "XAJ\0" (music).
        private uint m_DecompressedSize; //the output size of the audio stream stored in the file (in bytes).
        private ushort m_Tag;            //seems to be PCM waveformat tag (0x0001). This corresponds to the (decompressed) output audio stream, of course.
        private ushort m_Channels;       //number of channels for the file.
        private uint m_SampleRate;       //sample rate for the file.
        private uint m_AvgByteRate;      //average byte rate for the file (equal to (dwSampleRate)*(wAlign)). Note that this also corresponds to the decompressed output audio stream.
        private ushort m_Align;          //the sample align value for the file (equal to (wBits/8)*(wChannels)). Again, this corresponds to the decompressed output audio stream.
        private ushort m_Bits;           //resolution of the file (8 (8-bit), 16 (16-bit), etc.).

        /// <summary>
        /// The decompressed wave data as a byte array.
        /// Will not contain any data unless DecompressFile() has been called!
        /// </summary>
        public byte[] DecompressedData
        {
            get { return m_DecompressedStream.ToArray(); }
        }

        /// <summary>
        /// The decompressed wave data as a MemoryStream instance.
        /// Will not contain any data unless DecompressFile() has been called!
        /// </summary>
        public MemoryStream DecompressedStream
        {
            get
            {
                m_DecompressedStream.Position = 0;
                return m_DecompressedStream;
            }
        }

        public XAFile(byte[] data)
        {
            LoadFile(data);
            DecompressFile();
        }

        public XAFile(string path)
        {
            LoadFile(path);
            DecompressFile();
        }

        /// <summary>
        /// Loads a *.xa file, setting things up for decompression.
        /// Should always be called before DecompressFile().
        /// </summary>
        /// <param name="Path">The path to the *.xa file to load.</param>
        public void LoadFile(string Path)
        {
            m_Reader = new BinaryReader(File.Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read));

            m_ID = new string(m_Reader.ReadChars(4));
            m_DecompressedSize = m_Reader.ReadUInt32();
            m_Tag = m_Reader.ReadUInt16();
            m_Channels = m_Reader.ReadUInt16();
            m_SampleRate = m_Reader.ReadUInt32();
            m_AvgByteRate = m_Reader.ReadUInt32();
            m_Align = m_Reader.ReadUInt16();
            m_Bits = m_Reader.ReadUInt16();

            m_DecompressedStream = new MemoryStream((int)m_DecompressedSize);
            m_Writer = new BinaryWriter(m_DecompressedStream);
        }

        /// <summary>
        /// Loads an *.xa file, setting things up for decompression.
        /// Should always be called before DecompressFile().
        /// </summary>
        /// <param name="Data">The data of the *.xa file to process.</param>
        public void LoadFile(byte[] Data)
        {
            m_Reader = new BinaryReader(new MemoryStream(Data));

            m_ID = new string(m_Reader.ReadChars(4));
            m_DecompressedSize = m_Reader.ReadUInt32();
            m_Tag = m_Reader.ReadUInt16();
            m_Channels = m_Reader.ReadUInt16();
            m_SampleRate = m_Reader.ReadUInt32();
            m_AvgByteRate = m_Reader.ReadUInt32();
            m_Align = m_Reader.ReadUInt16();
            m_Bits = m_Reader.ReadUInt16();

            m_DecompressedStream = new MemoryStream((int)m_DecompressedSize);
            m_Writer = new BinaryWriter(m_DecompressedStream);
        }

        /// <summary>
        /// Decompresses the sounddata and stores it in the the decompressed stream in this class.
        /// </summary>
        public void DecompressFile()
        {
            uint dwDataSize = /*((m_DecompressedSize - 24) - (m_DecompressedSize - 24) / 15) * 4;*/m_DecompressedSize;
            uint dwFMTSize = 16;
            uint dwRIFFSize = /*dwFMTSize + 8 + dwDataSize + 8 + 4;*/ 8 + 4 + dwFMTSize + 4 + 4 + dwDataSize;

            m_Writer.Write(new char[] { 'R', 'I', 'F', 'F' });
            m_Writer.Write(dwRIFFSize); //Size of file minus this field and the above field.
            m_Writer.Write(new char[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            m_Writer.Write(dwFMTSize); //Size of WAVEFORMATEX structure (all before 'data').
            m_Writer.Write(m_Tag);
            m_Writer.Write(m_Channels);
            m_Writer.Write(m_SampleRate);
            m_Writer.Write(m_AvgByteRate);
            m_Writer.Write(m_Align);
            m_Writer.Write(m_Bits);
            m_Writer.Write(new char[] { 'd', 'a', 't', 'a' });
            m_Writer.Write(dwDataSize);

            if (m_Channels == 1) //Mono
            {
                while (m_Reader.BaseStream.Position < m_Reader.BaseStream.Length)
                {
                    DecompressMono(m_Reader.ReadBytes(0xF));
                }
            }
            else if (m_Channels == 2) //Stereo
            {
                while (m_Reader.BaseStream.Position < m_Reader.BaseStream.Length)
                {
                    DecompressStereo(m_Reader.ReadBytes(0x1E));
                }
            }
        }

        /// <summary>
        /// Decompresses a stereo sample.
        /// </summary>
        /// <param name="InputBuffer">The data containing the stereo sample.</param>
        private void DecompressStereo(byte[] InputBuffer)
        {
            byte bInput;
            uint i;
            int c1left, c2left, c1right, c2right, left, right;
            byte dleft, dright;

            bInput = InputBuffer[0];
            c1left = (int)EATable[HINIBBLE(bInput)];   // predictor coeffs for left channel
            c2left = (int)EATable[HINIBBLE(bInput) + 4];
            dleft = (byte)(LONIBBLE(bInput) + 8);   // shift value for left channel

            bInput = InputBuffer[1];
            c1right = (int)EATable[HINIBBLE(bInput)];  // predictor coeffs for right channel
            c2right = (int)EATable[HINIBBLE(bInput) + 4];
            dright = (byte)(LONIBBLE(bInput) + 8);  // shift value for right channel

            for (i = 2; i < InputBuffer.Length-1; i += 2)
            {
                left = HINIBBLE(InputBuffer[i]);  // HIGHER nibble for left channel
                left = (left << 0x1c) >> dleft;
                left = (left + m_CurSampleLeft * c1left + m_PrevSampleLeft * c2left + 0x80) >> 8;
                left = Clip16BitSample(left);
                m_PrevSampleLeft = m_CurSampleLeft;
                m_CurSampleLeft = left;

                right = HINIBBLE(InputBuffer[i + 1]); // HIGHER nibble for right channel
                right = (right << 0x1c) >> dright;
                right = (right + m_CurSampleRight * c1right + m_PrevSampleRight * c2right + 0x80) >> 8;
                right = Clip16BitSample(right);
                m_PrevSampleRight = m_CurSampleRight;
                m_CurSampleRight = right;

                // Now we've got lCurSampleLeft and lCurSampleRight which form one stereo
                // sample and all is set for the next step...
                //Output((SHORT)lCurSampleLeft,(SHORT)lCurSampleRight); // send the sample to output
                m_Writer.Write((short)m_CurSampleLeft);
                m_Writer.Write((short)m_CurSampleRight);

                // now do just the same for LOWER nibbles...
                // note that nubbles for each channel are packed pairwise into one byte

                left = LONIBBLE(InputBuffer[i]);  // LOWER nibble for left channel
                left = (left << 0x1c) >> dleft;
                left = (left + m_CurSampleLeft * c1left + m_PrevSampleLeft * c2left + 0x80) >> 8;
                left = Clip16BitSample(left);
                m_PrevSampleLeft = m_CurSampleLeft;
                m_CurSampleLeft = left;

                right = LONIBBLE(InputBuffer[i + 1]); // LOWER nibble for right channel
                right = (right << 0x1c) >> dright;
                right = (right + m_CurSampleRight * c1right + m_PrevSampleRight * c2right + 0x80) >> 8;
                right = Clip16BitSample(right);
                m_PrevSampleRight = m_CurSampleRight;
                m_CurSampleRight = right;

                // Now we've got lCurSampleLeft and lCurSampleRight which form one stereo
                // sample and all is set for the next step...
                m_Writer.Write((short)m_CurSampleLeft);
                m_Writer.Write((short)m_CurSampleRight);
            }
        }

        /// <summary>
        /// Decompresses a mono sample.
        /// </summary>
        /// <param name="InputBuffer">The data containing the mono sample.</param>
        private void DecompressMono(byte[] InputBuffer)
        {
            byte bInput = InputBuffer[0];
            uint i;
            int c1 = (int)EATable[HINIBBLE(bInput)];	// predictor coeffs
            int c2 = (int)EATable[HINIBBLE(bInput) + 4];
            int left, c1left, c2left;
            byte d, dleft;

            d = (byte)(LONIBBLE(bInput) + 8);  // shift value
            c1left = (int)EATable[HINIBBLE(bInput)];   // predictor coeffs for left channel
            c2left = (int)EATable[HINIBBLE(bInput) + 4];
            dleft = (byte)(LONIBBLE(bInput) + 8);   // shift value for left channel

            for (i = 1; i < 0xF; i++)
            {
                left = HINIBBLE(InputBuffer[i]);  // HIGHER nibble for left channel
                left = (left << 0x1c) >> dleft;
                left = (left + m_CurSampleLeft * c1left + m_PrevSampleLeft * c2left + 0x80) >> 8;
                left = Clip16BitSample(left);
                m_PrevSampleLeft = m_CurSampleLeft;
                m_CurSampleLeft = left;

                // Now we've got lCurSampleLeft which is one mono sample and all is set
                // for the next input nibble...
                //Output((SHORT)lCurSampleLeft); // send the sample to output
                m_Writer.Write((short)m_CurSampleLeft);

                left = LONIBBLE(InputBuffer[i]);  // LOWER nibble for left channel
                left = (left << 0x1c) >> dleft;
                left = (left + m_CurSampleLeft * c1left + m_PrevSampleLeft * c2left + 0x80) >> 8;
                left = Clip16BitSample(left);
                m_PrevSampleLeft = m_CurSampleLeft;
                m_CurSampleLeft = left;

                // Now we've got lCurSampleLeft which is one mono sample and all is set
                // for the next input byte...
                //Output((SHORT)lCurSampleLeft); // send the sample to output
                m_Writer.Write((short)m_CurSampleLeft);
            }
        }

        private int Clip16BitSample(int sample)
        {
            if (sample > 32767)
                return 32767;
            else if (sample < -32768)
                return (-32768);
            else
                return sample;
        }

        private byte HINIBBLE(byte B)
        {
            return (byte)(((B) >> 4));
        }

        private byte LONIBBLE(byte B)
        {
            return (byte)((B) & 0x0F);
        }

        private long[] EATable =
        {
             0x00000000,
             0x000000F0,
             0x000001CC,
             0x00000188,
             0x00000000,
             0x00000000,
             0xFFFFFF30,
             0xFFFFFF24,
             0x00000000,
             0x00000001,
             0x00000003,
             0x00000004,
             0x00000007,
             0x00000008,
             0x0000000A,
             0x0000000B,
             0x00000000,
             0xFFFFFFFF,
             0xFFFFFFFD,
             0xFFFFFFFC
        };
    }
}