using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.IO;
using FSO.Common;

namespace FSO.SimAntics.Engine.TSOGlobalLink
{
    public class VMTSOStandaloneDatabase : VMSerializable
    {
        public static readonly int CURRENT_VERSION = 2;
        public int Version = CURRENT_VERSION;

        public Dictionary<string, uint> IpNameToPersist;
        public HashSet<uint> TakenAvatarPersist; //COPY of above.Values, range 65536-int.maxvalue-1
        public HashSet<uint> Administrators;

        public Dictionary<uint, Dictionary<uint, byte[]>> PluginStorage;
        //todo: inventory
        //todo: skills, motives...

        public VMTSOStandaloneDatabase()
        {
            //attempt to load first
            try
            {
                using (var db = File.OpenRead(Path.Combine(FSOEnvironment.UserDir, "stubdb.fsodb")))
                {
                    Deserialize(new BinaryReader(db));
                    return;
                }
            } catch (Exception) {
                try
                {
                    using (var db = File.OpenRead(Path.Combine(FSOEnvironment.UserDir, "stubdb_backup.fsodb")))
                    {
                        Deserialize(new BinaryReader(db));
                        return;
                    }
                }
                catch (Exception) { };
            }

            IpNameToPersist = new Dictionary<string, uint>();
            TakenAvatarPersist = new HashSet<uint>();
            Administrators = new HashSet<uint>();
            PluginStorage = new Dictionary<uint, Dictionary<uint, byte[]>>();
        }

        public void Save()
        {
            try
            {
                File.Copy(Path.Combine(FSOEnvironment.UserDir, "stubdb.fsodb"), Path.Combine(FSOEnvironment.UserDir, "stubdb_backup.fsodb"), true);
            }
            catch (Exception e) { }
            using (var writer = new BinaryWriter(File.Open(Path.Combine(FSOEnvironment.UserDir, "stubdb.fsodb"), FileMode.Create))) SerializeInto(writer);
        }

        public uint FindOrAddAvatar(string idString)
        {
            uint result = 0;
            if (IpNameToPersist.TryGetValue(idString, out result))
            {
                return result;
            } else
            {
                var rand = new Random();
                uint ID = ((uint)rand.Next())+65536;
                while (TakenAvatarPersist.Contains(ID)) ID = ((uint)rand.Next()) + 65536;
                if (idString == "local:server") ID = uint.MaxValue - 1;
                //ok so we got one, add it to db
                IpNameToPersist.Add(idString, ID);
                TakenAvatarPersist.Add(ID);
                if (idString.StartsWith("local:")) Administrators.Add(ID);
                return ID;
            }
        }

        public void SavePluginPersist(uint obj, uint plugin, byte[] data)
        {
            Dictionary<uint, byte[]> objDat = null;
            if (!PluginStorage.TryGetValue(obj, out objDat))
            {
                objDat = new Dictionary<uint, byte[]>();
                PluginStorage.Add(obj, objDat);
                objDat.Add(plugin, data);
                return;
            }
            objDat[plugin] = data;
        }

        public byte[] LoadPluginPersist(uint obj, uint plugin)
        {
            Dictionary<uint, byte[]> objDat = null;
            if (!PluginStorage.TryGetValue(obj, out objDat))
            {
                return null;
            }
            byte[] dat = null;
            objDat.TryGetValue(plugin, out dat);
            return dat;
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(new char[] { 'F', 'S', 'O', 'd' });
            writer.Write(CURRENT_VERSION);
            writer.Write(IpNameToPersist.Count);
            foreach (var avaP in IpNameToPersist)
            {
                writer.Write(avaP.Key);
                writer.Write(avaP.Value);
            }

            writer.Write(Administrators.Count);
            foreach (var admin in Administrators) writer.Write(admin);

            writer.Write(PluginStorage.Count);
            foreach (var owner in PluginStorage)
            {
                writer.Write(owner.Key);
                writer.Write(owner.Value.Count);
                foreach (var data in owner.Value)
                {
                    writer.Write(data.Key);
                    writer.Write(data.Value.Length);
                    writer.Write(data.Value);
                }
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            if (new string(reader.ReadChars(4)) != "FSOd") return;
            Version = reader.ReadInt32();
            var avaCount = reader.ReadInt32();
            IpNameToPersist = new Dictionary<string, uint>();
            for (int i=0; i<avaCount; i++)
            {
                IpNameToPersist.Add(reader.ReadString(), reader.ReadUInt32());
            }
            TakenAvatarPersist = new HashSet<uint>(IpNameToPersist.Values);

            var adminCount = reader.ReadInt32();
            Administrators = new HashSet<uint>();
            for (int i = 0; i < adminCount; i++)
            {
                Administrators.Add(reader.ReadUInt32());
            }

            PluginStorage = new Dictionary<uint, Dictionary<uint, byte[]>>();
            if (Version > 1)
            {
                var objs = reader.ReadInt32();
                for (int i = 0; i < objs; i++)
                {
                    var ownerID = reader.ReadUInt32();
                    var ownerPlugins = new Dictionary<uint, byte[]>();
                    var plugins = reader.ReadInt32();
                    for (int j=0; j<plugins; j++)
                    {
                        var plugin = reader.ReadUInt32();
                        var byteCount = reader.ReadInt32();
                        ownerPlugins.Add(plugin, reader.ReadBytes(byteCount));
                    }
                    PluginStorage.Add(ownerID, ownerPlugins);
                }
            }
        }
    }
}
