using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.SimAntics.Engine.TSOGlobalLink
{
    public class VMTSOStandaloneDatabase : VMSerializable
    {
        public int Version = 1;
        public Dictionary<string, uint> IpNameToPersist;
        public HashSet<uint> TakenAvatarPersist; //COPY of above.Values, range 65536-int.maxvalue-1
        public HashSet<uint> Administrators;

        public Dictionary<uint, string> SignsPluginStorage;
        //todo: inventory
        //todo: skills, motives...

        public VMTSOStandaloneDatabase()
        {
            //attempt to load first
            try
            {
                using (var db = File.OpenRead("stubdb.fsodb"))
                {
                    Deserialize(new BinaryReader(db));
                    return;
                }
            } catch (Exception) {
                try
                {
                    using (var db = File.OpenRead("stubdb_backup.fsodb"))
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
            SignsPluginStorage = new Dictionary<uint, string>();
        }

        public void Save()
        {
            try
            {
                File.Copy("stubdb.fsodb", "stubdb_backup.fsodb", true);
            }
            catch (Exception e) { }
            using (var writer = new BinaryWriter(File.Open("stubdb.fsodb", FileMode.Create))) SerializeInto(writer);
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
                //ok so we got one, add it to db
                IpNameToPersist.Add(idString, ID);
                TakenAvatarPersist.Add(ID);
                if (idString.StartsWith("local:")) Administrators.Add(ID);
                return ID;
            }
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(new char[] { 'F', 'S', 'O', 'd' });
            writer.Write(Version);
            writer.Write(IpNameToPersist.Count);
            foreach (var avaP in IpNameToPersist)
            {
                writer.Write(avaP.Key);
                writer.Write(avaP.Value);
            }

            writer.Write(Administrators.Count);
            foreach (var admin in Administrators) writer.Write(admin);

            writer.Write(SignsPluginStorage.Count);
            foreach (var sign in SignsPluginStorage)
            {
                writer.Write(sign.Key);
                writer.Write(sign.Value);
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

            var signs = reader.ReadInt32();
            SignsPluginStorage = new Dictionary<uint, string>();
            for (int i = 0; i < signs; i++)
            {
                SignsPluginStorage.Add(reader.ReadUInt32(), reader.ReadString());
            }
        }
    }
}
