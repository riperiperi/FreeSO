using FSO.Files.Utils;
using System.IO;
using System.Text;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class ARRY : IffChunk
    {
        public int Width;
        public int Height;
        public ARRYType Type;
        public byte[] Data;
        public byte[] TransposeData
        {
            get
            {
                var stride = (int)Type;
                byte[] result = new byte[Data.Length];
                for (int i = 0; i < Data.Length; i += stride)
                {
                    var divI = i / stride;
                    var x = divI % Width;
                    var y = divI / Width;
                    int targetIndex = y * stride + x * stride * Width;
                    for (int j = 0; j < stride; j++)
                    {
                        result[targetIndex++] = Data[i + j];
                    }
                }
                return result;
            }
        }

        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var zero = io.ReadInt32();
                Width = io.ReadInt32();
                Height = io.ReadInt32();
                Type = (ARRYType)io.ReadInt32();
                var dataByteSize = ByteSize();
                Data = new byte[Width * Height * dataByteSize];
                var unknown = io.ReadInt32();


                int currentPosition = 0;
                while (io.HasMore)
                {
                    switch (Type)
                    {
                        default:
                            //bit format:
                            //1000 yyyy yyxx xxxx

                            var flre = io.ReadUInt16();
                            if (flre == 0) break;
                            bool rawFill = (flre & 0x8000) == 0;
                            if (rawFill)
                            {
                                for (int i=0; i<flre; i++)
                                {
                                    if (!io.HasMore) return;
                                    Data[currentPosition++] = io.ReadByte();
                                    if (currentPosition > Data.Length) return;
                                }
                                if ((flre & 1) == 1) io.ReadByte();
                                continue;
                            }

                            var lastPosition = currentPosition;
                            currentPosition += flre & 0x7FFF;
                            currentPosition %= Data.Length; //wrap to data size
                            if (currentPosition == 0) return;

                            var pad = io.ReadByte();//ReadElement(io); //pad the previous entries with this data
                                                    //if ((dataByteSize & 1) == 1) 
                            io.ReadByte(); //padded to 16 bits

                            while (lastPosition < currentPosition)
                            {
                                Data[lastPosition++] = pad;
                            }

                            if (!io.HasMore) return;

                            var size = io.ReadInt16();
                            if ((size & 0x8000) != 0)
                            {
                                io.Seek(SeekOrigin.Current, -2);
                                continue;
                            }
                            for (int i = 0; i < size; i++)
                            {
                                Data[currentPosition++] = io.ReadByte();//ReadElement(io);
                                currentPosition %= Data.Length;
                            }

                            if ((size & 1) == 1) io.ReadByte(); //16-bit pad

                            currentPosition %= Data.Length; //12 bit wrap
                            break;
                    }

                }
            }
        }

        public int ByteSize()
        {
            return (int)Type;
        }


        public string DebugPrint()
        {
            var dataByteSize = ByteSize();
            var result = new StringBuilder();
            int index = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var item = Data[index];
                    var str = item.ToString().PadLeft(3, ' ');
                    result.Append((str == "  0") ? "  ." : str);
                    index += dataByteSize;
                }
                result.AppendLine();
            }
            return result.ToString();
        }
    }

    public enum ARRYType : int
    {
        RLEAlt = 4, //altitude
        RLEFloor = 1, //floors, "ground", grass, flags, pool "ARRY(9)", water
        RLEWalls = 8, //walls
        Objects = 2, //objects "ARRY(3)"
    }
}
