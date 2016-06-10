using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.SimAntics.Engine;
using FSO.Content;

namespace FSO.SimAntics.Marshals
{
    public class VMContextMarshal : VMSerializable
    {
        public VMClockMarshal Clock;
        public VMArchitectureMarshal Architecture;
        public VMAmbientSoundMarshal Ambience;
        public ulong RandomSeed;

        public void Deserialize(BinaryReader reader)
        {
            Clock = new VMClockMarshal();
            Clock.Deserialize(reader);

            Architecture = new VMArchitectureMarshal();
            Architecture.Deserialize(reader);

            Ambience = new VMAmbientSoundMarshal();
            Ambience.Deserialize(reader);

            RandomSeed = reader.ReadUInt64();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            Clock.SerializeInto(writer);
            Architecture.SerializeInto(writer);
            Ambience.SerializeInto(writer);
            writer.Write(RandomSeed);
        }
    }

    public class VMClockMarshal : VMSerializable
    {
        public long Ticks;
        public int MinuteFractions;
        public int TicksPerMinute;
        public int Minutes;
        public int Hours;

        public void Deserialize(BinaryReader reader)
        {
            Ticks = reader.ReadInt64();
            MinuteFractions = reader.ReadInt32();
            TicksPerMinute = reader.ReadInt32();
            Minutes = reader.ReadInt32();
            Hours = reader.ReadInt32();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Ticks);
            writer.Write(MinuteFractions);
            writer.Write(TicksPerMinute);
            writer.Write(Minutes);
            writer.Write(Hours);
        }
    }

    public class VMAmbientSoundMarshal : VMSerializable
    {
        public byte[] ActiveSounds;
        public void Deserialize(BinaryReader reader)
        {
            ActiveSounds = reader.ReadBytes(reader.ReadByte());
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write((byte)ActiveSounds.Length);
            writer.Write(ActiveSounds);
        }
    }
}
