// This source code is property of Atalasoft, Inc. (www.atalasoft.com)
// No warrantees expressed or implied.
// You are free to use this in your own code, provided that you
// leave this notice in place.

// Change History:
// December 11, 2009 - Initial version, Steve Hawley

using System;
using System.Collections.Generic;
using System.IO;

namespace TSO_LoginServer.Network.Encryption
{
    /// <summary>
    /// From: http://www.atalasoft.com/cs/blogs/stevehawley/archive/2009/12/11/rc4-in-c.aspx
    /// </summary>
    public class ARC4
    {
        private byte[] _S;
        int _x, _y;

        public ARC4()
        {
        }

        public ARC4(byte[] key)
            : this(key, key.Length)
        {
        }

        public ARC4(byte[] key, int length)
        {
            SetKey(key, length);
        }

        public void SetKey(byte[] key)
        {
            SetKey(key, key.Length);
        }

        public void SetKey(byte[] key, int length)
        {
            if (_S == null)
                _S = new byte[256];

            if (key == null)
                throw new ArgumentNullException("key");
            if (length < 1 || length > key.Length)
                throw new ArgumentOutOfRangeException("length");

            for (int i = 0; i < 256; i++)
                _S[i] = (byte)i;
            for (int i = 0, j = 0; i < 256; i++)
            {
                j = (j + key[i % length] + _S[i]) & 0xff;
                byte temp = _S[i];
                _S[i] = _S[j];
                _S[j] = temp;
            }
            Reset();
        }

        public void Reset()
        {
            _x = _y = 0;
        }

        private byte GetNextMask()
        {
            _x = (_x + 1) & 0xff;
            _y = (_y + _S[_x]) & 0xff;
            byte temp = _S[_x];
            _S[_x] = _S[_y];
            _S[_y] = temp;
            return _S[(_S[_x] + _S[_y]) & 0xff];
        }

        public IEnumerable<byte> Process(IEnumerable<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (_S == null)
                throw new Exception("Need to call SetKey before calling Process");

            foreach (byte b in data)
                yield return (byte)(b ^ GetNextMask());
        }

        public void ProcessInPlace(byte[] data)
        {
            ProcessInPlace(data, 0, data.Length);
        }

        public void ProcessInPlace(byte[] data, int index, int length)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (_S == null)
                throw new Exception("Need to call SetKey before calling ProcessInPlace");
            if (index < 0 || index >= data.Length)
                throw new ArgumentOutOfRangeException("index");
            if (length <= 0 || index + length > data.Length)
                throw new ArgumentOutOfRangeException("length");

            for (int i = 0; i < length; i++)
                data[i + index] = (byte)(data[i + index] ^ GetNextMask());
        }

        public byte[] Process(byte[] data)
        {
            return Process(data, 0, data.Length);
        }

        public byte[] Process(byte[] data, int index, int length)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (_S == null)
                throw new Exception("Need to call SetKey before calling Process");
            if (index < 0 || index >= data.Length)
                throw new ArgumentOutOfRangeException("index");
            if (length <= 0 || index + length > data.Length)
                throw new ArgumentOutOfRangeException("length");

            byte[] output = new byte[length];

            for (int i = 0; i < length; i++)
                output[i] = (byte)(data[i + index] ^ GetNextMask());
            return output;
        }

        public void Process(Stream data, Stream output)
        {
            if (_S == null)
                throw new Exception("Need to call SetKey before calling Process");
            int databyte;
            while ((databyte = data.ReadByte()) >= 0)
                output.WriteByte((byte)(databyte ^ GetNextMask()));
        }

        public void Process(Stream data, Stream output, long length)
        {
            if (length <= 0 || data.Position + length > data.Length)
                throw new ArgumentOutOfRangeException("length");
            if (_S == null)
                throw new Exception("Need to call SetKey before calling Process");

            for (long i = 0; i < length; i++)
            {
                int databyte = data.ReadByte();
                if (databyte < 0)
                    throw new EndOfStreamException("unexpected end of stream processing RC4");
                output.WriteByte((byte)(databyte ^ GetNextMask()));
            }
        }
    }
}