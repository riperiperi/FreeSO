using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.SimAntics.Marshals.Threads;

namespace FSO.SimAntics.Marshals
{
    public class VMMarshal : VMSerializable
    {
        public VMContextMarshal Context;

        public VMEntityMarshal[] Entities;
        public VMThreadMarshal[] Threads;
        public VMMultitileGroupMarshal[] MultitileGroups;

        public short[] GlobalState;
        public short ObjectId = 1;

        public void Deserialize(BinaryReader reader)
        {
            if (new string(reader.ReadChars(4)) != "FSOv") return;

            Context = new VMContextMarshal();
            Context.Deserialize(reader);

            int ents = reader.ReadInt32();
            Entities = new VMEntityMarshal[ents];
            for (int i=0; i<ents; i++)
            {
                var type = reader.ReadByte();
                var ent = (type == 1) ? (VMEntityMarshal) new VMAvatarMarshal() : new VMGameObjectMarshal();
                ent.Deserialize(reader);
                Entities[i] = ent;
            }

            int thrN = reader.ReadInt32();
            Threads = new VMThreadMarshal[thrN];
            for (int i = 0; i < thrN; i++)
            {
                Threads[i] = new VMThreadMarshal();
                Threads[i].Deserialize(reader);
            }

            int mtgN = reader.ReadInt32();
            MultitileGroups = new VMMultitileGroupMarshal[mtgN];
            for (int i = 0; i < mtgN; i++)
            {
                MultitileGroups[i] = new VMMultitileGroupMarshal();
                MultitileGroups[i].Deserialize(reader);
            }

            int globs = reader.ReadInt32();
            GlobalState = new short[globs];
            for (int i = 0; i < globs; i++)
            {
                GlobalState[i] = reader.ReadInt16();
            }

            ObjectId = reader.ReadInt16();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(new char[] { 'F', 'S', 'O', 'v' });

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            Context.SerializeInto(writer);
            Console.WriteLine("== SERIAL: Context done... " + timer.ElapsedMilliseconds + " ms ==");

            writer.Write(Entities.Length);
            foreach (var ent in Entities)
            {
                byte type = (byte)((ent is VMAvatarMarshal) ? 1 : 0);
                writer.Write(type);
                ent.SerializeInto(writer);
            }
            Console.WriteLine("== SERIAL: Ents done... " + timer.ElapsedMilliseconds + " ms ==");

            writer.Write(Threads.Length);
            foreach(var thr in Threads) thr.SerializeInto(writer);

            Console.WriteLine("== SERIAL: Threads done... " + timer.ElapsedMilliseconds + " ms ==");

            writer.Write(MultitileGroups.Length);
            foreach (var grp in MultitileGroups) grp.SerializeInto(writer);

            Console.WriteLine("== SERIAL: Groups done... " + timer.ElapsedMilliseconds + " ms ==");

            writer.Write(GlobalState.Length);
            foreach (var val in GlobalState)
            {
                writer.Write(val);
            }

            Console.WriteLine("== SERIAL: Globals done... " + timer.ElapsedMilliseconds + " ms ==");

            writer.Write(ObjectId);
        }
    }
}
