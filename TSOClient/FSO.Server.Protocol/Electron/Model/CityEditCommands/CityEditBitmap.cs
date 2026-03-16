using FSO.Common.Serialization;
using Mina.Core.Buffer;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FSO.Server.Protocol.Electron.Model.CityEditCommands
{
    public class CityEditBitmap
    {
        public const int MaxWidth = 512;
        public const int MaxHeight = 512;
        private const int BitsPerItem = 64;
        private const ulong AllBits = 0xFFFF_FFFF_FFFF_FFFF;

        public int X;
        public int Y;
        public int Width;
        public int Height;
        public ulong[] Data;

        public CityEditBitmap(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Data = new ulong[GetDataCount()];
        }

        public CityEditBitmap(int width, int height) : this(0, 0, width, height)
        {
        }

        public CityEditBitmap(IoBuffer input)
        {
            Deserialize(input);
        }

        private int GetDataCount()
        {
            return (Width * Height + BitsPerItem - 1) / BitsPerItem;
        }

        public virtual void Deserialize(IoBuffer input)
        {
            X = input.GetInt32();
            Y = input.GetInt32();
            Width = input.GetInt32();
            Height = input.GetInt32();

            if (Width < 0 || Height < 0 || Width > MaxWidth || Height > MaxHeight)
            {
                throw new Exception("City edit bitmap too large");
            }

            if (X < 0 || Y < 0 || X + Width > MaxWidth || Y + Height > MaxHeight)
            {
                throw new Exception("City edit bitmap out of bounds");
            }

            var bytes = input.GetSlice(GetDataCount() * sizeof(ulong)).GetBytes();

            Data = MemoryMarshal.Cast<byte, ulong>(bytes).ToArray();
        }

        public virtual void Serialize(IoBuffer output)
        {
            output.PutInt32(X);
            output.PutInt32(Y);
            output.PutInt32(Width);
            output.PutInt32(Height);

            var cast = MemoryMarshal.Cast<ulong, byte>(Data).ToArray();
            output.Put(cast);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ((int x, int y, int count) line, bool hasMore) FragmentLine(ref (int index, int count) builder)
        {
            int width = Width;
            int startY = builder.index / width;
            int endY = (builder.index + builder.count - 1) / width;

            int x = builder.index % width;

            if (endY > startY)
            {
                int newCount = width - x;
                builder.index += newCount;
                builder.count -= newCount;

                return ((x, startY, newCount), true);
            }
            else
            {
                return ((x, startY, builder.count), false);
            }
        }

        /// <summary>
        /// Get horizontal lines that have been set. Iterates through coordinate start points
        /// and the number of pixels in the line that are set.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<(int x, int y, int count)> GetSetLines()
        {
            int dataIndex = 0;
            int bitIndex = 0;

            (int index, int count) builder = default;

            while (dataIndex < Data.Length)
            {
                ulong data = Data[dataIndex++];

                while (true)
                {
                    int toStart = 0;

                    if (builder.count == 0)
                    {
                        // Try find the start of a range.
                        // Get zero count (number of bits to skip)
                        toStart = BitOperations.TrailingZeroCount(data);

                        if (toStart == BitsPerItem)
                        {
                            break;
                        }

                        builder = (bitIndex + toStart, 0);
                    }
                    // If the above case isn't true, then we're continuing a range from the previous data.

                    ulong endMask = 0xFFFFFFFFFFFFFFFFul << toStart;

                    // Count the number of set bits:
                    // - invert everything after the start, so we can count 0s again until the next "1" (actually 0)
                    ulong inverted = data ^ endMask;
                    var toEnd = BitOperations.TrailingZeroCount(inverted);

                    builder.count += toEnd - toStart;
                    if (toEnd < BitsPerItem)
                    {
                        ulong remainingMask = 0xFFFFFFFFFFFFFFFFul << toEnd;
                        data &= remainingMask;

                        bool hasMore;
                        do
                        {
                            var fragment = FragmentLine(ref builder);
                            hasMore = fragment.hasMore;
                            yield return fragment.line;
                        }
                        while (hasMore);

                        builder = default;
                    }
                    else
                    {
                        break;
                    }
                }

                bitIndex += BitsPerItem;
            }

            if (builder.count != 0)
            {
                bool hasMore;
                do
                {
                    var fragment = FragmentLine(ref builder);
                    hasMore = fragment.hasMore;
                    yield return fragment.line;
                }
                while (hasMore);
            }

            yield break;
        }

        public void Set(int x, int y)
        {
            int index = y * Width + x;

            int dataIndex = index / BitsPerItem;
            int dataBit = index % BitsPerItem;
            ulong bit = 1ul << dataBit;

            Data[dataIndex] |= bit;
        }

        public void Set(int x, int y, int count)
        {
            int startIndex = y * Width + x;
            int endIndex = startIndex + count;

            int startDataIndex = startIndex / BitsPerItem;
            int endDataIndex = (endIndex + BitsPerItem - 1) / BitsPerItem;

            int startBit = startIndex % BitsPerItem;
            int endBit = endIndex % BitsPerItem;

            ulong startMask = (AllBits << startBit);
            ulong endMask = (AllBits >> (BitsPerItem - endBit));

            int index = startDataIndex;
            int dataCount = endDataIndex - startDataIndex;

            for (int i = 0; i < dataCount; i++)
            {
                ulong bits = i == 0 ? startMask : AllBits;

                if (i == dataCount - 1)
                {
                    bits &= endMask;
                }

                Data[index++] |= bits;
            }
        }

        public bool IsSet(int x, int y)
        {
            int index = y * Width + x;

            int dataIndex = index / BitsPerItem;
            int dataBit = index % BitsPerItem;
            ulong bit = 1ul << dataBit;

            return (Data[dataIndex] & bit) != 0;
        }

        public CityEditBitmap Trim()
        {
            int minX = 512, minY = 512, maxX = 0, maxY = 0;

            foreach (var line in GetSetLines())
            {
                if (line.x < minX) minX = line.x;
                if (line.x + line.count - 1 > maxX) maxX = line.x + line.count - 1;
                if (line.y < minY) minY = line.y;
                if (line.y > maxY) maxY = line.y;
            }

            if (minX > maxX)
            {
                // This bitmap is empty.
                return null;
            }

            var newBitmap = new CityEditBitmap(minX, minY, 1 + maxX - minX, 1 + maxY - minY);

            foreach (var (x, y, count) in GetSetLines())
            {
                newBitmap.Set(x - minX, y - minY, count);
            }

            return newBitmap;
        }

        public void Clear()
        {
            Array.Clear(Data);
        }
    }
}
