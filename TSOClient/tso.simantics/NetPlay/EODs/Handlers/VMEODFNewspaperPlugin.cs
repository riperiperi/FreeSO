using FSO.SimAntics.NetPlay.EODs.Archetypes;
using FSO.SimAntics.NetPlay.EODs.Utils;
using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODFNewspaperPlugin : VMBasicEOD<object>
    {
        private VMEODFNewspaperData Data;

        public VMEODFNewspaperPlugin(VMEODServer server) : base(server, "newspaper")
        {
        }

        protected override void OnConnected(VMEODClient client)
        {
            //all this plugin needs to do is get the info from the database, and send it
            //on it's merry way
            Server.vm.GlobalLink.GetDynPayouts((data) =>
            {
                client.Send("newspaper_state", data);
            });
        }
    }

    /// <summary>
    /// Datapoint for the newspaper's payout graph. Encodes a single day for a specific skill.
    /// </summary>
    public class VMEODFNewspaperPoint : VMSerializable
    {
        public int Day { get; set; }
        public int Skilltype { get; set; }
        public float Multiplier { get; set; }
        public int Flags { get; set; }

        public void Deserialize(BinaryReader reader)
        {
            Day = reader.ReadInt32();
            Skilltype = reader.ReadInt32();
            Multiplier = reader.ReadSingle();
            Flags = reader.ReadInt32();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Day);
            writer.Write(Skilltype);
            writer.Write(Multiplier);
            writer.Write(Flags);
        }
    }

    public class VMEODFNewspaperNews : VMSerializable
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public long StartDate { get; set; }
        public long EndDate { get; set; }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ID);
            writer.Write(Name);
            writer.Write(Description);
            writer.Write(StartDate);
            writer.Write(EndDate);
        }

        public void Deserialize(BinaryReader reader)
        {
            ID = reader.ReadInt32();
            Name = reader.ReadString();
            Description = reader.ReadString();
            StartDate = reader.ReadInt64();
            EndDate = reader.ReadInt64();
        }
    }

    public class VMEODFNewspaperData : VMSerializable
    {
        public List<VMEODFNewspaperNews> News = new List<VMEODFNewspaperNews>();
        public List<VMEODFNewspaperPoint> Points = new List<VMEODFNewspaperPoint>();

        public void Deserialize(BinaryReader reader)
        {
            var totalNews = reader.ReadInt32();
            News.Clear();
            for (int i = 0; i < totalNews; i++)
            {
                var dat = new VMEODFNewspaperNews();
                dat.Deserialize(reader);
                News.Add(dat);
            }

            var totalPoints = reader.ReadInt32();
            Points.Clear();
            for (int i=0; i<totalPoints; i++)
            {
                var dat = new VMEODFNewspaperPoint();
                dat.Deserialize(reader);
                Points.Add(dat);
            }
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(News.Count);
            foreach (var data in News)
                data.SerializeInto(writer);
            
            writer.Write(Points.Count);
            foreach (var data in Points)
                data.SerializeInto(writer);
        }

        public VMEODFNewspaperData() { }

        public VMEODFNewspaperData(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                Deserialize(new BinaryReader(stream));
            }
        }
    }
}
