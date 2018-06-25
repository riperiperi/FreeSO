using FSO.Common;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.TS1
{
    /// <summary>
    /// Provides families, neighbors, neighborhood structure and more given a current userdata folder. Should also allow the game to save these things.
    /// </summary>
    public class TS1NeighborhoodProvider
    {
        public IffFile MainResource;
        public IffFile LotLocations;
        public IffFile StreetNames;
        public IffFile NeighbourhoodDesc;
        public IffFile STDesc;
        public IffFile MTDesc;
        public Dictionary<short, short> ZoningDictionary = new Dictionary<short, short>();
        public NBRS Neighbors;
        public NGBH Neighborhood;
        public TATT TypeAttributes;
        public Dictionary<short, FAMI> FamilyForHouse = new Dictionary<short, FAMI>();
        public Content ContentManager;
        public TS1GameState GameState = new TS1GameState();
        public string UserPath;
        public int NextSim;

        public HashSet<uint> DirtyAvatars = new HashSet<uint>();

        public TS1NeighborhoodProvider(Content contentManager)
        {
            ContentManager = contentManager;
            InitSpecific(1);
        }

        /// <summary>
        /// Intializes a specific neighbourhood. Also counts as a save discard, since it unloads the current neighbourhood.
        /// </summary>
        /// <param name="id"></param>
        public void InitSpecific(int id)
        {
            DirtyAvatars.Clear();
            ZoningDictionary.Clear();
            FamilyForHouse.Clear();

            var udName = "UserData" + ((id == 0) ? "" : (id+1).ToString());
            //simitone shouldn't modify existing ts1 data, since our house saves are incompatible.
            //therefore we should copy to the simitone user data.

            var userPath = Path.Combine(FSOEnvironment.UserDir, udName + "/");

            if (!Directory.Exists(userPath))
            {
                var source = Path.Combine(ContentManager.TS1BasePath, udName + "/");
                var destination = userPath;

                //quick and dirty copy.

                foreach (string dirPath in Directory.GetDirectories(source, "*",
                    SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace('\\', '/').Replace(source, destination));

                foreach (string newPath in Directory.GetFiles(source, "*.*",
                    SearchOption.AllDirectories))
                    File.Copy(newPath, newPath.Replace('\\', '/').Replace(source, destination), true);
            }

            UserPath = userPath;

            MainResource = new IffFile(Path.Combine(UserPath, "Neighborhood.iff"));
            LotLocations = new IffFile(Path.Combine(UserPath, "LotLocations.iff"));
            var lotZoning = new IffFile(Path.Combine(UserPath, "LotZoning.iff"));
            StreetNames = new IffFile(Path.Combine(UserPath, "StreetNames.iff"));
            NeighbourhoodDesc = new IffFile(Path.Combine(UserPath, "Houses/NeighborhoodDesc.iff"));
            STDesc = new IffFile(Path.Combine(UserPath, "Houses/STDesc.iff"));
            MTDesc = new IffFile(Path.Combine(UserPath, "Houses/MTDesc.iff"));

            var zones = lotZoning.Get<STR>(1);
            for (int i = 0; i < zones.Length; i++)
            {
                var split = zones.GetString(i).Split(',');
                ZoningDictionary[short.Parse(split[0])] = (short)((split[1] == " community") ? 1 : 0);
            }
            Neighbors = MainResource.List<NBRS>().FirstOrDefault();
            Neighborhood = MainResource.List<NGBH>().FirstOrDefault();
            TypeAttributes = MainResource.List<TATT>().FirstOrDefault();

            FamilyForHouse = new Dictionary<short, FAMI>();
            var families = MainResource.List<FAMI>();
            foreach (var fam in families)
            {
                FamilyForHouse[(short)fam.HouseNumber] = fam;
            }

            LoadCharacters(true);

            //todo: manage avatar iffs here
        }

        public void LoadCharacters(bool clearLast)
        {
            var objs = (TS1ObjectProvider)ContentManager.WorldObjects;
            if (objs.Entries == null) return;
            if (clearLast)
            {
                foreach (var obj in objs.Entries.Where(x => x.Value.Source == GameObjectSource.User))
                    objs.RemoveObject((uint)obj.Key);
            }

            NextSim = 0;
            var path = Path.Combine(UserPath, "Characters/");
            var files = Directory.EnumerateFiles(path);
            foreach (var filename in files)
            {
                if (Path.GetExtension(filename) != ".iff") return;

                int userID;
                var name = Path.GetFileName(filename);
                if (name.Length > 8 && int.TryParse(name.Substring(4, 5), out userID) && userID >= NextSim)
                {
                    NextSim = userID + 1;
                }

                var file = new IffFile(filename);
                file.MarkThrowaway();

                var objects = file.List<OBJD>();
                if (objects != null)
                {
                    foreach (var obj in objects)
                    {
                        objs.Entries[obj.GUID] = new GameObjectReference(objs)
                        {
                            FileName = filename,
                            ID = obj.GUID,
                            Name = obj.ChunkLabel,
                            Source = GameObjectSource.User,
                            Group = (short)obj.MasterID,
                            SubIndex = obj.SubIndex
                        };
                    }
                }
            }
        }

        public void SaveNewNeighbour(GameObject obj)
        {
            var objs = (TS1ObjectProvider)ContentManager.WorldObjects;
            //save to a new user iff
            var path = Path.Combine(UserPath, "Characters/");
            var filename = Path.Combine(path, "User" + (NextSim++).ToString().PadLeft(5, '0') + ".iff");
            using (var stream = new FileStream(filename, FileMode.Create))
                obj.Resource.MainIff.Write(stream);

            objs.Entries[obj.OBJ.GUID] = new GameObjectReference(objs)
            {
                FileName = filename,
                ID = obj.GUID,
                Name = obj.OBJ.ChunkLabel,
                Source = GameObjectSource.User,
                Group = (short)obj.OBJ.MasterID,
                SubIndex = obj.OBJ.SubIndex
            };
        }

        public Neighbour GetNeighborByID(short ID)
        {
            Neighbour result = null;
            Neighbors.NeighbourByID.TryGetValue(ID, out result);
            return result;
        }

        public FAMI GetFamilyForHouse(short ID)
        {
            FAMI result = null;
            FamilyForHouse.TryGetValue(ID, out result);
            return result;
        }

        public int GetMagicoinsForNeighbor(short ID)
        {
            return GetInventoryByNID(ID)?.FirstOrDefault(x => x.GUID == 0x99E81BEC)?.Count ?? 0;
        }

        public int GetMagicoinsForFamily(FAMI family)
        {
            if (family == null) return 0;
            return family.FamilyGUIDs.Select(x => GetMagicoinsForNeighbor(GetNeighborIDForGUID(x) ?? -1)).Sum();
        }

        public void MoveOut(short houseID)
        {
            var old = GetFamilyForHouse(houseID);
            old.HouseNumber = 0;
            FamilyForHouse.Remove(houseID);
        }

        public void SetFamilyForHouse(short houseID, FAMI family, bool buy)
        {
            family.HouseNumber = houseID;
            if (buy)
            {
                family.Budget -= GetHouse(houseID)?.Get<SIMI>(1)?.PurchaseValue ?? 0;
            }
            FamilyForHouse[houseID] = family;
        }

        public FAMI GetFamily(ushort ID)
        {
            return MainResource.Get<FAMI>(ID);
        }

        public FAMs GetFamilyString(ushort ID)
        {
            return MainResource.Get<FAMs>(ID);
        }

        public short? GetNeighborIDForGUID(uint GUID)
        {
            short result = 0;
            if (Neighbors.DefaultNeighbourByGUID.TryGetValue(GUID, out result))
                return result;
            return null;
        }

        public List<InventoryItem> GetInventoryByNID(short ID)
        {
            List<InventoryItem> result = null;
            Neighborhood.InventoryByID.TryGetValue(ID, out result);
            return result;
        }

        public void SetInventoryForNID(short ID, List<InventoryItem> list)
        {
            Neighborhood.InventoryByID[ID] = list;
        }

        public short SetToNext(short current)
        {
            return (short)(Neighbors.Entries.FirstOrDefault(x => x.NeighbourID > current)?.NeighbourID ?? -1);
        }

        public short SetToNext(short current, uint guid)
        {
            return (short)(Neighbors.Entries.FirstOrDefault(x => x.NeighbourID > current && x.GUID == guid)?.NeighbourID ?? -1);
        }

        public bool SaveNeighbourhood(bool withSims)
        {
            //todo: save iffs for dirty avatars. 
            foreach (var ava in DirtyAvatars)
            {
                var obj = ContentManager.WorldObjects.Get(ava);
                using (var stream = new FileStream(obj.Resource.Name, FileMode.Create))
                    obj.Resource.MainIff.Write(stream);
            }
            DirtyAvatars.Clear();

            using (var stream = new FileStream(Path.Combine(UserPath, "Neighborhood.iff"), FileMode.Create, FileAccess.Write, FileShare.None))
                MainResource.Write(stream);

            return true;
        }

        public bool SaveHouse(int houseID, IffFile file)
        {
            using (var stream = new FileStream(GetHousePath(houseID), FileMode.Create, FileAccess.Write, FileShare.None))
                file.Write(stream);

            return true;
        }

        public IffFile GetHouse(int id)
        {
            return new IffFile(Path.Combine(UserPath, "Houses/House"+id.ToString().PadLeft(2, '0')+".iff"));
        }

        public string GetHousePath(int id)
        {
            return Path.Combine(UserPath, "Houses/House" + id.ToString().PadLeft(2, '0') + ".iff");
        }

        public BMP GetHouseThumb(int id)
        {
            return GetHouse(id)?.Get<BMP>(512); //roof on
        }

        public short GetTATT(uint guid, int index)
        {
            short[] dat = null;
            if (TypeAttributes.TypeAttributesByGUID.TryGetValue(guid, out dat))
            {
                if (index >= dat.Length) return 0;
                else return dat[index];
            }
            return 0;
        }

        public Tuple<string, string> GetHouseNameDesc(int houseID)
        {
            STR res;
            if (houseID < 80) res = NeighbourhoodDesc.Get<STR>((ushort)(houseID + 2000));
            else if (houseID < 90) res = STDesc.Get<STR>((ushort)(houseID + 2000));
            else res = MTDesc.Get<STR>((ushort)(houseID + 2000));

            if (res == null) return new Tuple<string, string>("", "");
            else return new Tuple<string, string>(res.GetString(0), res.GetString(1));
        }

        public void SetTATT(uint guid, int index, short value)
        {
            short[] dat = null;
            if (!TypeAttributes.TypeAttributesByGUID.TryGetValue(guid, out dat))
            {
                var obj = ContentManager.WorldObjects.Get(guid);
                if (obj == null) return;
                dat = new short[32];
                TypeAttributes.TypeAttributesByGUID[guid] = dat;
            }
            if (index >= dat.Length) return;
            else dat[index] = value;
        }
    }

    public class TS1GameState
    {
        public FAMI ActiveFamily;
        public uint DowntownSimGUID;
        public short LotTransitInfo;
    }
}
