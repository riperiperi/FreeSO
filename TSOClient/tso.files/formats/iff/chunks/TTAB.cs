using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using tso.files.utils;

namespace tso.files.formats.iff.chunks
{
    public class TTAB : IffChunk
    {
        public TTABInteraction[] Interactions;

        public override void Read(Iff iff, System.IO.Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                Interactions = new TTABInteraction[io.ReadUInt16()];
                var version = io.ReadUInt16();
                if (version != 9 && version != 10)
                {
                    for (int i = 0; i < Interactions.Length; i++)
                    {
                        var result = new TTABInteraction();
                        result.ActionFunction = io.ReadUInt16();
                        result.TestFunction = io.ReadUInt16();
                        result.MotiveEntries = new TTABMotiveEntry[io.ReadUInt32()];
                        result.Flags = io.ReadUInt32();
                        result.TTAIndex = io.ReadUInt32();
                        if (version > 6) result.AttenuationCode = io.ReadUInt32();
                        result.AttenuationValue = io.ReadFloat();
                        result.AutonomyThreshold = io.ReadUInt32();
                        result.JoiningIndex = io.ReadInt32();
                        for (int j = 0; j < result.MotiveEntries.Length; j++)
                        {
                            var motive = new TTABMotiveEntry();
                            if (version > 6) motive.EffectRangeMinimum = io.ReadInt16();
                            motive.EffectRangeMaximum = io.ReadInt16();
                            if (version > 6) motive.PersonalityModifier = io.ReadUInt16();
                            result.MotiveEntries[j] = motive;
                        }
                        if (version > 9) result.Unknown = io.ReadUInt32();
                        Interactions[i] = result;
                    }
                } else {
                    //need to read with weird field encoding thing.
                }
            }
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
}
