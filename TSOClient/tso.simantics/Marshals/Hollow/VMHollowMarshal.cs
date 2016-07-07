using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Marshals.Hollow
{
    public class VMHollowMarshal : VMSerializable
    {
        public int Version = VMMarshal.LATEST_VERSION;
        public bool Compressed = true;
        public VMContextMarshal Context;

        public VMHollowGameObjectMarshal[] Entities;
        public VMMultitileGroupMarshal[] MultitileGroups;

        public void Deserialize(BinaryReader reader)
        {
            if (new string(reader.ReadChars(4)) != "FSOh") return;

            Version = reader.ReadInt32();
            Compressed = reader.ReadBoolean();

            var uReader = reader;
            MemoryStream cStream = null;
            GZipStream zipStream = null;
            if (Compressed)
            {
                var length = reader.ReadInt32();
                cStream = new MemoryStream(reader.ReadBytes(length));
                zipStream = new GZipStream(cStream, CompressionMode.Decompress);
                reader = new BinaryReader(zipStream);
            }

            Context = new VMContextMarshal(Version);
            Context.Deserialize(reader);

            int ents = reader.ReadInt32();
            Entities = new VMHollowGameObjectMarshal[ents];
            for (int i = 0; i < ents; i++)
            {
                var ent = new VMHollowGameObjectMarshal(Version);
                ent.Deserialize(reader);
                Entities[i] = ent;
            }

            int mtgN = reader.ReadInt32();
            MultitileGroups = new VMMultitileGroupMarshal[mtgN];
            for (int i = 0; i < mtgN; i++)
            {
                MultitileGroups[i] = new VMMultitileGroupMarshal();
                MultitileGroups[i].Deserialize(reader);
            }
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(new char[] { 'F', 'S', 'O', 'h' });
            writer.Write(Version);
            writer.Write(Compressed);

            var uWriter = writer;
            MemoryStream cStream = null;
            GZipStream zipStream = null;
            if (Compressed)
            {
                cStream = new MemoryStream();
                zipStream = new GZipStream(cStream, CompressionMode.Compress);
                writer = new BinaryWriter(zipStream);
            }

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            Context.SerializeInto(writer);

            writer.Write(Entities.Length);
            foreach (var ent in Entities)
            {
                ent.SerializeInto(writer);
            }

            writer.Write(MultitileGroups.Length);
            foreach (var grp in MultitileGroups) grp.SerializeInto(writer);

            if (Compressed)
            {
                writer.Close();
                zipStream.Close();
                var data = cStream.ToArray();
                uWriter.Write(data.Length);
                uWriter.Write(data);
            }
        }
    }
}
