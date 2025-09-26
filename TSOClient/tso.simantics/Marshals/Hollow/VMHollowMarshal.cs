using FSO.SimAntics.NetPlay.Model;
using System.IO;
using System.IO.Compression;

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

            if (Compressed)
            {
                var length = reader.ReadInt32();
                var cStream = new MemoryStream(reader.ReadBytes(length));
                var zipStream = new GZipStream(cStream, CompressionMode.Decompress);
                var decompStream = new MemoryStream();
                zipStream.CopyTo(decompStream);
                decompStream.Seek(0, SeekOrigin.Begin);
                reader = new BinaryReader(decompStream);
                cStream.Close();
                zipStream.Close();
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
                MultitileGroups[i] = new VMMultitileGroupMarshal(Version);
                MultitileGroups[i].Deserialize(reader);
            }

            if (Compressed)
            {
                reader.BaseStream.Close();
            }
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(new char[] { 'F', 'S', 'O', 'h' });
            writer.Write(VMMarshal.LATEST_VERSION);
            writer.Write(Compressed);


            var uWriter = writer;
            MemoryStream cStream = null;
            if (Compressed)
            {
                cStream = new MemoryStream();
                writer = new BinaryWriter(cStream);
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
                //zipStream.Close();
                var data = cStream.ToArray();

                var zipMStream = new MemoryStream();
                var zipStream = new GZipStream(zipMStream, CompressionMode.Compress);
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();

                var cData = zipMStream.ToArray();

                uWriter.Write(cData.Length);
                uWriter.Write(cData);

                cStream.Close();
                zipMStream.Close();
            }
        }
    }
}
