using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.SimAntics.Marshals.Threads;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using System.IO.Compression;
using FSO.Common.Model;
using FSO.SimAntics.Model.Platform;
using FSO.SimAntics.Model.TS1Platform;

namespace FSO.SimAntics.Marshals
{
    public class VMMarshal : VMSerializable
    {
        // 26 - add build/buy disable
        // 27 - TS1 platform state
        // 28 - chat update state
        // 29 - TS1 clock
        // 30 - LastWalkStyle (for auto run)
        // 31 - Object Flags (donated), Last Lot Owner ID (other mayor stuff)
        // 32 - Bezier Routing
        // 33 - NhoodID and Location
        // 34 - Upgrade Level
        // 35 - FSO Inventory Retrieve
        // 36 - FSO Inventory Token (inventory ops async state has temp list)
        // 37 - Inventory Token Total
        public static readonly int LATEST_VERSION = 37;

        public int Version = LATEST_VERSION;
        public bool Compressed = true;
        public bool TS1;
        public VMContextMarshal Context;

        public VMEntityMarshal[] Entities;
        public VMThreadMarshal[] Threads;
        public VMMultitileGroupMarshal[] MultitileGroups;

        public short[] GlobalState;
        public VMAbstractLotState PlatformState;
        public short ObjectId = 1;

        public DynamicTuning Tuning;

        public void Deserialize(BinaryReader reader)
        {
            if (new string(reader.ReadChars(4)) != "FSOv") return;

            Version = reader.ReadInt32();
            Compressed = reader.ReadBoolean();

            var uReader = reader;
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

            if (Version > 26) TS1 = reader.ReadBoolean();
            Context = new VMContextMarshal(Version);
            Context.Deserialize(reader);

            int ents = reader.ReadInt32();
            Entities = new VMEntityMarshal[ents];
            for (int i=0; i<ents; i++)
            {
                var type = reader.ReadByte();
                var ent = (type == 1) ? (VMEntityMarshal) new VMAvatarMarshal(Version, TS1) : new VMGameObjectMarshal(Version, TS1);
                ent.Deserialize(reader);
                Entities[i] = ent;
            }

            int thrN = reader.ReadInt32();
            Threads = new VMThreadMarshal[thrN];
            for (int i = 0; i < thrN; i++)
            {
                Threads[i] = new VMThreadMarshal(Version);
                Threads[i].Deserialize(reader);
            }

            int mtgN = reader.ReadInt32();
            MultitileGroups = new VMMultitileGroupMarshal[mtgN];
            for (int i = 0; i < mtgN; i++)
            {
                MultitileGroups[i] = new VMMultitileGroupMarshal(Version);
                MultitileGroups[i].Deserialize(reader);
            }

            int globs = reader.ReadInt32();
            GlobalState = new short[globs];
            for (int i = 0; i < globs; i++)
            {
                GlobalState[i] = reader.ReadInt16();
            }


            PlatformState = (TS1)?(VMAbstractLotState)new VMTS1LotState(Version):new VMTSOLotState(Version);
            PlatformState.Deserialize(reader);

            ObjectId = reader.ReadInt16();

            if (Version > 23)
            {
                if (reader.ReadBoolean())
                    Tuning = new DynamicTuning(reader);
            }

            if (Compressed)
            {
                reader.BaseStream.Close();
            }
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(new char[] { 'F', 'S', 'O', 'v' });
            writer.Write(Version);
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

            writer.Write(TS1);
            Context.SerializeInto(writer);
            //Console.WriteLine("== SERIAL: Context done... " + timer.ElapsedMilliseconds + " ms ==");

            writer.Write(Entities.Length);
            foreach (var ent in Entities)
            {
                byte type = (byte)((ent is VMAvatarMarshal) ? 1 : 0);
                writer.Write(type);
                ent.SerializeInto(writer);
            }
            //Console.WriteLine("== SERIAL: Ents done... " + timer.ElapsedMilliseconds + " ms ==");

            writer.Write(Threads.Length);
            foreach (var thr in Threads) thr.SerializeInto(writer);

            //Console.WriteLine("== SERIAL: Threads done... " + timer.ElapsedMilliseconds + " ms ==");

            writer.Write(MultitileGroups.Length);
            foreach (var grp in MultitileGroups) grp.SerializeInto(writer);

            //Console.WriteLine("== SERIAL: Groups done... " + timer.ElapsedMilliseconds + " ms ==");

            writer.Write(GlobalState.Length);
            foreach (var val in GlobalState)
            {
                writer.Write(val);
            }
            PlatformState.SerializeInto(writer);

            //Console.WriteLine("== SERIAL: Globals done... " + timer.ElapsedMilliseconds + " ms ==");

            writer.Write(ObjectId);

            writer.Write(Tuning != null);
            if (Tuning != null) Tuning.SerializeInto(writer);

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
