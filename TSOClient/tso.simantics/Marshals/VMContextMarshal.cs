using FSO.SimAntics.NetPlay.Model;
using System;
using System.IO;

namespace FSO.SimAntics.Marshals
{
    public class VMContextMarshal : VMSerializable
    {
        public VMClockMarshal Clock;
        public VMArchitectureMarshal Architecture;
        public VMAmbientSoundMarshal Ambience;
        public ulong RandomSeed;

        public int Version;
        public VMContextMarshal() { }
        public VMContextMarshal(int version) { Version = version; }

        public void Deserialize(BinaryReader reader)
        {
            Clock = new VMClockMarshal(Version);
            Clock.Deserialize(reader);

            Architecture = new VMArchitectureMarshal(Version);
            Architecture.Deserialize(reader);

            Ambience = new VMAmbientSoundMarshal(Version);
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

        public int DayOfMonth = 1;
        public int Month = 6;
        public int Year = 1997;

        public int FirePercent = 20000;
        public long UTCStart;

        private int Version;
        public VMClockMarshal() { }
        public VMClockMarshal(int version) { Version = version; }

        public void Deserialize(BinaryReader reader)
        {
            Ticks = reader.ReadInt64();
            MinuteFractions = reader.ReadInt32();
            TicksPerMinute = reader.ReadInt32();
            Minutes = reader.ReadInt32();
            Hours = reader.ReadInt32();
            if (Version > 28)
            {
                DayOfMonth = reader.ReadInt32();
                Month = reader.ReadInt32();
                Year = reader.ReadInt32();
            }
            if (Version > 17) FirePercent = reader.ReadInt32();
            if (Version > 20) UTCStart = reader.ReadInt64();
            else UTCStart = DateTime.UtcNow.Ticks;
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Ticks);
            writer.Write(MinuteFractions);
            writer.Write(TicksPerMinute);
            writer.Write(Minutes);
            writer.Write(Hours);

            writer.Write(DayOfMonth);
            writer.Write(Month);
            writer.Write(Year);

            writer.Write(FirePercent);
            writer.Write(UTCStart);
        }
    }

    public class VMAmbientSoundMarshal : VMSerializable
    {
        public ulong ActiveBits;
        int Version;
        public VMAmbientSoundMarshal() { }
        public VMAmbientSoundMarshal(int version) { Version = version; }

        public void Deserialize(BinaryReader reader)
        {
            if (Version > 8)
            {
                ActiveBits = reader.ReadUInt64();
            }
            else
                reader.ReadBytes(reader.ReadByte()); //super-legacy
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ActiveBits);
        }
    }
}
