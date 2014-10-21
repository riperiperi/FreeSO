/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Files.utils;

namespace TSO.Files.formats.iff.chunks
{
    public class TTAB : IffChunk
    {
        public TTABInteraction[] Interactions;
        public Dictionary<uint, TTABInteraction> InteractionByIndex;

        public override void Read(Iff iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                Interactions = new TTABInteraction[io.ReadUInt16()];
                if (Interactions.Length == 0) return; //no interactions, don't bother reading remainder.
                InteractionByIndex = new Dictionary<uint, TTABInteraction>();
                var version = io.ReadUInt16();
                IOProxy iop;
                if (version != 9 && version != 10) iop = new TTABNormal(io);
                else
                {
                    var compressionCode = io.ReadByte();
                    if (compressionCode != 1) throw new Exception("hey what!!");
                    iop = new TTABFieldEncode(io); //haven't guaranteed that this works, since none of the objects in the test lot use it.
                }
                for (int i = 0; i < Interactions.Length; i++)
                {
                    var result = new TTABInteraction();
                    result.ActionFunction = iop.ReadUInt16();
                    result.TestFunction = iop.ReadUInt16();
                    result.MotiveEntries = new TTABMotiveEntry[iop.ReadUInt32()];
                    result.Flags = iop.ReadUInt32();
                    result.TTAIndex = iop.ReadUInt32();
                    if (version > 6) result.AttenuationCode = iop.ReadUInt32();
                    result.AttenuationValue = iop.ReadFloat();
                    result.AutonomyThreshold = iop.ReadUInt32();
                    result.JoiningIndex = iop.ReadInt32();
                    for (int j = 0; j < result.MotiveEntries.Length; j++)
                    {
                        var motive = new TTABMotiveEntry();
                        if (version > 6) motive.EffectRangeMinimum = iop.ReadInt16();
                        motive.EffectRangeMaximum = iop.ReadInt16();
                        if (version > 6) motive.PersonalityModifier = iop.ReadUInt16();
                        result.MotiveEntries[j] = motive;
                    }
                    if (version > 9) result.Unknown = iop.ReadUInt32();
                    Interactions[i] = result;
                    InteractionByIndex.Add(result.TTAIndex, result);
                }
            }
        }
    }

    abstract class IOProxy
    {
        public abstract ushort ReadUInt16();
        public abstract short ReadInt16();
        public abstract int ReadInt32();
        public abstract uint ReadUInt32();
        public abstract float ReadFloat();

        public IoBuffer io;
        public IOProxy(IoBuffer io)
        {
            this.io = io;
        }
    }

   class TTABNormal : IOProxy
    {
        public override ushort ReadUInt16() { return io.ReadUInt16(); }
        public override short ReadInt16() { return io.ReadInt16(); }
        public override int ReadInt32() { return io.ReadInt32(); }
        public override uint ReadUInt32() { return io.ReadUInt32(); }
        public override float ReadFloat() { return io.ReadFloat(); }

        public TTABNormal(IoBuffer io) : base(io) { }
    }

    class TTABFieldEncode : IOProxy
    {
        private byte bitPos = 0;
        private byte curByte = 0;
        static byte[] widths = { 5, 8, 13, 16 };
        static byte[] widths2 = { 6, 11, 21, 32 };

        public void setBytePos(int n)
        {
            io.Seek(SeekOrigin.Begin, n);
            curByte = io.ReadByte();
            bitPos = 0;
        }

        public override ushort ReadUInt16() {
            return (ushort)ReadField(false);
        }

        public override short ReadInt16()
        {
            return (short)ReadField(false);
        }

        public override int ReadInt32()
        {
            return (int)ReadField(true);
        }

        public override uint ReadUInt32()
        {
            return (uint)ReadField(true);
        }

        public override float ReadFloat()
        {
            return (float)ReadField(true);
            //this is incredibly wrong
        }

        private long ReadField(bool big)
        {
            if (ReadBit() == 0) return 0;

            uint code = ReadBits(2);
            byte width = (big)?widths2[code]:widths[code];
            long value = ReadBits(width);
            value |= -(value & (1 << (width-1)));

            return value;
        }

        private uint ReadBits(int n)
        {
            uint total = 0;
            for (int i = 0; i < n; i++)
            {
                total += (uint)(ReadBit() << ((n - i)-1));
            }
            return total;
        }

        private byte ReadBit()
        {
            byte result = (byte)((curByte & (1 << (7 - bitPos))) >> (7 - bitPos));
            if (++bitPos > 7)
            {
                bitPos = 0;
                try
                {
                    curByte = io.ReadByte();
                }
                catch (Exception)
                {
                    curByte = 0; //no more data, read 0
                }
            }
            return result;
        }

        public TTABFieldEncode(IoBuffer io) : base(io) {
            curByte = io.ReadByte();
            bitPos = 0;
        }
    }

    public struct TTABInteraction
    {
        public ushort ActionFunction;
        public ushort TestFunction;
        public TTABMotiveEntry[] MotiveEntries;
        public uint Flags;
        public uint TTAIndex;
        public uint AttenuationCode;
        public float AttenuationValue;
        public uint AutonomyThreshold;
        public int JoiningIndex;
        public uint Unknown;
    }

    public struct TTABMotiveEntry
    {
        public short EffectRangeMinimum;
        public short EffectRangeMaximum;
        public ushort PersonalityModifier;
    }

    public enum TTABFlags
    {
        Debug = 1<<7
    }
}
