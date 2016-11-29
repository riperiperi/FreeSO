using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Marshals
{
    public class VMStandaloneObjectMarshal
    {

        public int Version = VMMarshal.LATEST_VERSION;
        public bool Compressed = true;

        public VMEntityMarshal[] Entities;
        public VMMultitileGroupMarshal MultitileGroup;

        public void Deserialize(BinaryReader reader)
        {
            if (new string(reader.ReadChars(4)) != "FSOo") return;

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

            int ents = reader.ReadInt32();
            Entities = new VMEntityMarshal[ents];
            for (int i = 0; i < ents; i++)
            {
                var ent = new VMGameObjectMarshal(Version);
                ent.Deserialize(reader);
                Entities[i] = ent;
            }
            
            MultitileGroup = new VMMultitileGroupMarshal(Version);
            MultitileGroup.Deserialize(reader);
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(new char[] { 'F', 'S', 'O', 'o' });
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

            writer.Write(Entities.Length);
            foreach (var ent in Entities)
            {
                ent.SerializeInto(writer);
            }

            MultitileGroup.SerializeInto(writer);

            if (Compressed)
            {
                writer.Close();
                zipStream.Close();
                var data = cStream.ToArray();
                uWriter.Write(data.Length);
                uWriter.Write(data);
            }
        }

        public VMStandaloneObjectMarshal() { }
        public VMStandaloneObjectMarshal(VMMultitileGroup group)
        {
            if (group.BaseObject is VMAvatar) return; //do not attempt to create standalone object marshals for avatars.
            Entities = new VMEntityMarshal[group.Objects.Count];
            MultitileGroup = group.Save();

            int i = 0;
            foreach (var ent in group.Objects)
            {
                var sent = ((VMGameObject)ent).Save();
                sent.Contained = new short[sent.Contained.Length];
                Entities[i++] = sent;
            }
        }

        /// <summary>
        /// Creates an instance of this (multitile) object in the target VM, out of world.
        /// </summary>
        /// <param name="vm"></param>
        public VMMultitileGroup CreateInstance(VM vm)
        {
            int i = 0;
            foreach (var ent in Entities)
            {
                VMEntity realEnt;
                var objDefinition = FSO.Content.Content.Get().WorldObjects.Get(ent.GUID);

                var worldObject = new ObjectComponent(objDefinition);
                var obj = new VMGameObject(objDefinition, worldObject);
                obj.Load((VMGameObjectMarshal)ent);
                obj.Contained = new VMEntity[ent.Contained.Length]; //we aren't loading slot data, but we need to initialize this.
                obj.Position = LotTilePos.OUT_OF_WORLD;
                if (VM.UseWorld)
                {
                    vm.Context.Blueprint.AddObject((ObjectComponent)obj.WorldUI);
                    vm.Context.Blueprint.ChangeObjectLocation((ObjectComponent)obj.WorldUI, obj.Position);
                }
                realEnt = obj;
                obj.FetchTreeByName(vm.Context);
                obj.Thread = new VMThread(vm.Context, obj, obj.Object.OBJ.StackSize);

                vm.AddEntity(obj);
                MultitileGroup.Objects[i++] = obj.ObjectID; //update saved group, in multitile group order (as saved)
                if (VM.UseWorld) obj.WorldUI.ObjectID = obj.ObjectID;
            }
            
            return new VMMultitileGroup(MultitileGroup, vm.Context); //should self register
        }
    }


}
