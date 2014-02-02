using System;
using System.Collections.Generic;
using System.Text;

namespace SimsLib.IFF
{
    public struct FieldEncodingData
    {
        public byte CompressionCode;
        public byte[] FieldWidths;
        public uint EncodedDataLength;
        public int FieldDataCounter;
        public byte[] EncodedData;

        public uint ReadDataLength;
        public byte BitBufferCount;
        public long BitBuffer;
    }

    /// <summary>
    /// Used to decode fields from a set of data compressed using field encoding 
    /// (http://simtech.sourceforge.net/tech/misc.html#fields). Original code
    /// written by Propeng.
    /// </summary>
    public class FieldReader
    {
        /// <summary>
        /// Reads bits from data that has been field encoded.
        /// </summary>
        /// <param name="FieldData">The data to decode from.</param>
        /// <param name="Width">The width of the field to encode (how many bits it occupies).</param>
        /// <param name="Value">The value that will contain the decoded field.</param>
        /// <returns>1 for success, 0 for failure.</returns>
        private int ReadBits(ref FieldEncodingData FieldData, byte Width, ref long Value)
        {
            while (FieldData.BitBufferCount < Width)
            {
                if (FieldData.ReadDataLength >= FieldData.EncodedDataLength)
                    return 0;

                FieldData.BitBuffer <<= 8;
                FieldData.BitBuffer |= FieldData.EncodedData[FieldData.FieldDataCounter];
                FieldData.BitBufferCount += 8;
                FieldData.ReadDataLength++;
            }

            Value = FieldData.BitBuffer >> (FieldData.BitBufferCount - Width);
            Value &= (1L << Width) - 1;
            FieldData.BitBufferCount -= Width;

            return 1;
        }

        /// <summary>
        /// Decodes a field from data that has been field encoded.
        /// </summary>
        /// <param name="Data">The data to decode from.</param>
        /// <param name="FieldType">The type of the field to decode.</param>
        /// <param name="Value">A long that will contain the decoded value.</param>
        /// <returns>1 for success, 0 for failure.</returns>
        public int DecodeField(ref FieldEncodingData Data, byte FieldType, ref long Value)
        {
            long Prefix = 0, Width = 0;

            if(ReadBits(ref Data, 1, ref Value) == 0)
                return 0;

            if (Value == 0)
                return 1;

            if (ReadBits(ref Data, 2, ref Prefix) == 0)
                return 0;

            Width = Data.FieldWidths[FieldType * 4 + Prefix];

            if(ReadBits(ref Data, (byte)Width, ref Value) == 0)
                return 0;

            Value |= -(Value & 1L << (byte)(Width - 1));

            return 1;
        }
    }
}
