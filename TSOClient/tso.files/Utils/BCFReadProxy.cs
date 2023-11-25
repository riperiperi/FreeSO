using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FSO.Files.Utils
{
    public interface BCFReadProxy : IDisposable
    {
        byte ReadByte();
        ushort ReadUInt16();
        short ReadInt16();
        int ReadInt32();
        uint ReadUInt32();
        float ReadFloat();
        string ReadPascalString();
        string ReadLongPascalString();
    }

    public interface BCFWriteProxy : IDisposable
    {
        void WriteByte(byte data);
        void WriteUInt16(ushort data);
        void WriteInt16(short data);
        void WriteInt32(int data);
        void WriteUInt32(uint data);
        void WriteFloat(float data);
        void WritePascalString(string data);
        void WriteLongPascalString(string data);

        void SetGrouping(int groupSize);
    }

    public class BCFReadString : BCFReadProxy
    {
        private StreamReader Reader;
        public int Version;
        private string[] NumBuf = new string[0];
        private int NumInd = 1;

        public BCFReadString(Stream input, bool version)
        {
            Reader = new StreamReader(input);

            if (!version) return;
            //skip to version
            var line = "";
            while (!line.StartsWith("version "))
                line = Reader.ReadLine();
            Version = int.Parse(line.Substring(8));
        }

        private string ReadNum()
        {
            //contrary to popular belief, this function that returns a string does indeed read a number
            if (NumInd >= NumBuf.Length)
            {
                NumBuf = Reader.ReadLine().Trim().Split(' ').ToArray();
                NumInd = 0;
            }
            return NumBuf[NumInd++];
        }

        public byte ReadByte() { return byte.Parse(ReadNum()); }
        public ushort ReadUInt16() { return ushort.Parse(ReadNum()); }
        public short ReadInt16() { return short.Parse(ReadNum()); }
        public int ReadInt32() { return int.Parse(ReadNum()); }
        public uint ReadUInt32() { return uint.Parse(ReadNum()); }
        public float ReadFloat() { return float.Parse(ReadNum(), CultureInfo.InvariantCulture); }
        public string ReadPascalString() { return Reader.ReadLine(); }
        public string ReadLongPascalString() { return Reader.ReadLine(); }

        public void Dispose()
        {
            Reader.Dispose();
        }
    }

    public class BCFWriteString : BCFWriteProxy
    {
        private StreamWriter Writer;
        public int Version;
        private int GroupSize = 1;
        private int GroupInd;

        public BCFWriteString(Stream input, bool version)
        {
            Writer = new StreamWriter(input);

            if (!version) return;
            //write out default version
            Writer.WriteLine("version 300");
        }

        public void SetGrouping(int groupSize)
        {
            if (GroupInd > 0) Writer.WriteLine();
            GroupInd = 0;
            GroupSize = groupSize;
        }

        private void WriteNum(string num)
        {
            if (GroupSize-1 >= GroupInd)
            {
                Writer.WriteLine(num);
                GroupInd = 0;
            } else
            {
                Writer.Write(num + " ");
            }
        }

        public void WriteByte(byte data) { WriteNum(data.ToString()); }
        public void WriteUInt16(ushort data) { WriteNum(data.ToString()); }
        public void WriteInt16(short data) { WriteNum(data.ToString()); }
        public void WriteInt32(int data) { WriteNum(data.ToString()); }
        public void WriteUInt32(uint data) { WriteNum(data.ToString()); }
        public void WriteFloat(float data) { WriteNum(data.ToString(CultureInfo.InvariantCulture)); }
        public void WritePascalString(string data) { WriteNum(data); }
        public void WriteLongPascalString(string data) { WriteNum(data); }

        public void Dispose()
        {
            Writer.Dispose();
        }
    }
}
