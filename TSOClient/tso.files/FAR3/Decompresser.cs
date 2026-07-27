using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace FSO.Files.FAR3
{
    /// <summary>
    /// Represents a decompresser that can decompress files in a FAR3
    /// archive. If you have some kind of need to understand this code, go to:
    /// http://wiki.niotso.org/RefPack
    /// The code in this class was ported from DBPF4J:
    /// http://sc4dbpf4j.cvs.sourceforge.net/viewvc/sc4dbpf4j/DBPF4J/
    /// </summary>
    public class Decompresser
    {
        private long m_CompressedSize = 0;
        private long m_DecompressedSize = 0;

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
        /// <param name="src">The source array.</param>
        /// <param name="srcPos">The source Position.</param>
        /// <param name="dest">The destination array.</param>
        /// <param name="destPos">The destination Position.</param>
        /// <param name="length">The length.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ArrayCopy2(Span<byte> src, int srcPos, Span<byte> dest, int destPos, int length)
        {
            src.Slice(srcPos, length).CopyTo(dest.Slice(destPos, length));
        }

        /// <summary>
        /// Copies data from array at destPos-srcPos to array at destPos.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="offset">The Position to copy from (reverse from end of array!)</param>
        /// <param name="destPos">The Position to copy to.</param>
        /// <param name="length">The length of data to copy.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void OffsetCopy(Span<byte> array, int offset, int destPos, int length)
        {
            int srcPos = destPos - offset;

            // This is a little complicated.
            // If the length exceeds the offset, then we will start copying the data in a feedback loop.
            // Normally memcpy could do this, but there's no c# equivalent.

            if (length > offset || length < 8)
            {
                // This copy is also a bit faster if there's not much to copy.
                for (int i = 0; i < length; i++)
                {
                    array[destPos + i] = array[srcPos + i];
                }
            }
            else
            {
                array.Slice(srcPos, length).CopyTo(array.Slice(destPos, length));
            }
        }

        /// <summary>
        /// Compresses data and returns it as an array of bytes.
        /// Assumes that the array of bytes passed contains 
        /// uncompressed data.
        /// </summary>
        /// <param name="Data">The data to be compressed.</param>
        /// <returns>An array of bytes with compressed data.</returns>
        public byte[] Compress(byte[] Data)
        {
            // if data is big enough for compress
            if (Data.Length > 6)
            {
	            // some Compression Data
	            const int MAX_OFFSET = 0x20000;
	            const int MAX_COPY_COUNT = 0x404;
	            // used to finetune the lookup (small values increase the
	            // compression for Big Files)
	            const int QFS_MAXITER = 0x80;

	            // contains the latest offset for a combination of two
	            // characters
	            Dictionary<int, ArrayList> cmpmap2 = new Dictionary<int, ArrayList>();

	            // will contain the compressed data (maximal size =
	            // uncompressedSize+MAX_COPY_COUNT)
	            byte[] cData = new byte[Data.Length + MAX_COPY_COUNT];

	            // init some vars
	            int writeIndex = 9; // leave 9 bytes for the header
	            int lastReadIndex = 0;
	            ArrayList indexList = null;
	            int copyOffset = 0;
	            int copyCount = 0;
	            int index = -1;
	            bool end = false;

                Span<byte> dataSpan = Data;
                Span<byte> compressedSpan = cData;

	            // begin main compression loop
	            while (index < Data.Length - 3)
                {
		            // get all Compression Candidates (list of offsets for all
		            // occurances of the current 3 bytes)
		            do 
                    {
			            index++;
			            if (index >= Data.Length - 2)
                        {
				            end = true;
				            break;
			            }
			            int mapindex = Data[index] + (Data[index + 1] << 8)
					            + (Data[index + 2] << 16);

			            indexList = cmpmap2[mapindex];
			            if (indexList == null)
                        {
				            indexList = new ArrayList();
				            cmpmap2.Add(mapindex, indexList);
			            }
			            indexList.Add(index);
		            } while (index < lastReadIndex);
		            if (end)
			            break;

		            // find the longest repeating byte sequence in the index
		            // List (for offset copy)
		            int offsetCopyCount = 0;
		            int loopcount = 1;
		            while ((loopcount < indexList.Count) && (loopcount < QFS_MAXITER))
                    {
			            int foundindex = (int) indexList[(indexList.Count - 1) - loopcount];
			            if ((index - foundindex) >= MAX_OFFSET)
                        {
				            break;
			            }

			            loopcount++;
			            copyCount = 3;

			            while ((Data.Length > index + copyCount)&& (Data[index + copyCount] == Data[foundindex + copyCount]) && (copyCount < MAX_COPY_COUNT))
                        {
				            copyCount++;
			            }

			            if (copyCount > offsetCopyCount)
                        {
				            offsetCopyCount = copyCount;
				            copyOffset = index - foundindex;
			            }
		            }

		            // check if we can compress this
		            // In FSH Tool stand additionally this:
		            if (offsetCopyCount > Data.Length - index)
                    {
			            offsetCopyCount = index - Data.Length;
		            }
		            if (offsetCopyCount <= 2)
                    {
			            offsetCopyCount = 0;
		            } 
                    else if ((offsetCopyCount == 3) && (copyOffset > 0x400)) 
                    { // 1024
			            offsetCopyCount = 0;
		            } 
                    else if ((offsetCopyCount == 4) && (copyOffset > 0x4000)) 
                    { // 16384
			            offsetCopyCount = 0;
		            }

		            // this is offset-compressable? so do the compression
		            if (offsetCopyCount > 0)
                    {
			            // plaincopy

			            // In FSH Tool stand this (A):
			            while (index - lastReadIndex >= 4)
                        {
				            copyCount = (index - lastReadIndex) / 4 - 1;
				            if (copyCount > 0x1B)
                            {
					            copyCount = 0x1B;
				            }
                            cData[writeIndex++] = (byte)(0xE0 + copyCount);
				            copyCount = 4 * copyCount + 4;

				            ArrayCopy2(dataSpan, lastReadIndex, compressedSpan, writeIndex, copyCount);
				            lastReadIndex += copyCount;
				            writeIndex += copyCount;
			            }

			            // offsetcopy
			            copyCount = index - lastReadIndex;
			            copyOffset--;
			            if ((offsetCopyCount <= 0x0A) && (copyOffset < 0x400))
                        {
				            cData[writeIndex++] = (byte) (((copyOffset >> 8) << 5)
						            + ((offsetCopyCount - 3) << 2) + copyCount);
                            cData[writeIndex++] = (byte)(copyOffset & 0xff);
			            } 
                        else if ((offsetCopyCount <= 0x43) && (copyOffset < 0x4000))
                        {
                            cData[writeIndex++] = (byte)(0x80 + (offsetCopyCount - 4));
				            cData[writeIndex++] = (byte) ((copyCount << 6) + (copyOffset >> 8));
				            cData[writeIndex++] = (byte) (copyOffset & 0xff);
			            } 
                        else if ((offsetCopyCount <= MAX_COPY_COUNT) && (copyOffset < MAX_OFFSET))
                        {
                            cData[writeIndex++] = (byte)(0xc0
						            + ((copyOffset >> 16) << 4)
						            + (((offsetCopyCount - 5) >> 8) << 2) + copyCount);
                            cData[writeIndex++] = (byte)((copyOffset >> 8) & 0xff);
                            cData[writeIndex++] = (byte)(copyOffset & 0xff);
                            cData[writeIndex++] = (byte)((offsetCopyCount - 5) & 0xff);
			            }

			            // do the offset copy
			            ArrayCopy2(dataSpan, lastReadIndex, compressedSpan, writeIndex, copyCount);
			            writeIndex += copyCount;
			            lastReadIndex += copyCount;
			            lastReadIndex += offsetCopyCount;
		            }
	            }

	            // add the End Record
	            index = Data.Length;
	            // in FSH Tool stand the same as above (A)
	            while (index - lastReadIndex >= 4)
                {
		            copyCount = (index - lastReadIndex) / 4 - 1;
		            
                    if (copyCount > 0x1B)
			            copyCount = 0x1B;

                    cData[writeIndex++] = (byte)(0xE0 + copyCount);
		            copyCount = 4 * copyCount + 4;

		            ArrayCopy2(dataSpan, lastReadIndex, compressedSpan, writeIndex, copyCount);
		            lastReadIndex += copyCount;
		            writeIndex += copyCount;
	            }

	            copyCount = index - lastReadIndex;
	            cData[writeIndex++] = (byte) (0xfc + copyCount);
	            ArrayCopy2(dataSpan, lastReadIndex, compressedSpan, writeIndex, copyCount);
	            writeIndex += copyCount;
	            lastReadIndex += copyCount;

                MemoryStream DataStream = new MemoryStream();
                BinaryWriter Writer = new BinaryWriter(DataStream);

	            // write the header for the compressed data
	            // set the compressed size
                Writer.Write((uint)writeIndex);
                m_CompressedSize = writeIndex;
	            // set the MAGICNUMBER
                Writer.Write((ushort)0xFB10);
	            // set the decompressed size
	            byte[] revData = BitConverter.GetBytes(Data.Length);
                Writer.Write((revData[2] << 16) | (revData[1] << 8) | revData[0]);
                Writer.Write(cData);

                //Avoid nasty swearing here!
                Writer.Flush();

                m_DecompressedSize = Data.Length;

	            return DataStream.ToArray();
            }

            return Data;
        }

        /// <summary>
        /// Decompresses data and returns it as an
        /// uncompressed array of bytes.
        /// </summary>
        /// <param name="Data">The data to decompress.</param>
        /// <returns>An uncompressed array of bytes.</returns>
        public byte[] Decompress(byte[] Data)
        {
            if (Data.Length > 6)
            {
                byte[] DecompressedData = new byte[(int)m_DecompressedSize];

                Span<byte> dataSpan = Data;
                Span<byte> decompressedSpan = DecompressedData;

                int DataPos = 0;

                int Pos = 0;
                byte Control1 = 0;

                while (Control1 != 0xFC && Pos < dataSpan.Length)
                {
                    Control1 = dataSpan[Pos];
                    Pos++;

                    if (Pos == dataSpan.Length)
                        break;

                    if (Control1 >= 0 && Control1 <= 127)
                    {
                        // 0x00 - 0x7F
                        byte control2 = dataSpan[Pos];
                        Pos++;
                        int numberOfPlainText = Control1 & 0x03;
                        ArrayCopy2(dataSpan, Pos, decompressedSpan, DataPos, numberOfPlainText);
                        DataPos += numberOfPlainText;
                        Pos += numberOfPlainText;

                        if (DataPos == (decompressedSpan.Length))
                            break;

                        int offset = (((Control1 & 0x60) << 3) + (control2) + 1);
                        int numberToCopyFromOffset = ((Control1 & 0x1C) >> 2) + 3;
                        OffsetCopy(decompressedSpan, offset, DataPos, numberToCopyFromOffset);
                        DataPos += numberToCopyFromOffset;

                        if (DataPos == (decompressedSpan.Length))
                            break;
                    }
                    else if ((Control1 >= 128 && Control1 <= 191))
                    {
                        // 0x80 - 0xBF
                        byte control2 = dataSpan[Pos];
                        Pos++;
                        byte control3 = dataSpan[Pos];
                        Pos++;

                        int numberOfPlainText = (control2 >> 6) & 0x03;
                        ArrayCopy2(dataSpan, Pos, decompressedSpan, DataPos, numberOfPlainText);
                        DataPos += numberOfPlainText;
                        Pos += numberOfPlainText;

                        if (DataPos == (decompressedSpan.Length))
                            break;

                        int offset = (((control2 & 0x3F) << 8) + (control3) + 1);
                        int numberToCopyFromOffset = (Control1 & 0x3F) + 4;
                        OffsetCopy(decompressedSpan, offset, DataPos, numberToCopyFromOffset);
                        DataPos += numberToCopyFromOffset;

                        if (DataPos == (decompressedSpan.Length))
                            break;
                    }
                    else if (Control1 >= 192 && Control1 <= 223)
                    {
                        // 0xC0 - 0xDF
                        int numberOfPlainText = (Control1 & 0x03);
                        byte control2 = dataSpan[Pos];
                        Pos++;
                        byte control3 = dataSpan[Pos];
                        Pos++;
                        byte control4 = dataSpan[Pos];
                        Pos++;
                        ArrayCopy2(dataSpan, Pos, decompressedSpan, DataPos, numberOfPlainText);
                        DataPos += numberOfPlainText;
                        Pos += numberOfPlainText;

                        if (DataPos == (decompressedSpan.Length))
                            break;

                        int offset = (((Control1 & 0x10) << 12) + (control2 << 8) + (control3) + 1);
                        int numberToCopyFromOffset = ((Control1 & 0x0C) << 6) + (control4) + 5;
                        OffsetCopy(decompressedSpan, offset, DataPos, numberToCopyFromOffset);
                        DataPos += numberToCopyFromOffset;

                        if (DataPos == (decompressedSpan.Length))
                            break;
                    }
                    else if (Control1 >= 224 && Control1 <= 251)
                    {
                        // 0xE0 - 0xFB
                        int numberOfPlainText = ((Control1 & 0x1F) << 2) + 4;
                        ArrayCopy2(dataSpan, Pos, decompressedSpan, DataPos, numberOfPlainText);
                        DataPos += numberOfPlainText;
                        Pos += numberOfPlainText;

                        if (DataPos == (decompressedSpan.Length))
                            break;
                    }
                    else
                    {
                        int numberOfPlainText = (Control1 & 0x03);
                        ArrayCopy2(dataSpan, Pos, decompressedSpan, DataPos, numberOfPlainText);

                        DataPos += numberOfPlainText;
                        Pos += numberOfPlainText;

                        if (DataPos == (decompressedSpan.Length))
                            break;
                    }
                }

                return DecompressedData;
            }

            //No data to decompress
            return Data;
        }
    }
}