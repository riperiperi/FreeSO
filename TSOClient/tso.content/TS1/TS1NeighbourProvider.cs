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
        public Dictionary<short, short> ZoningDictionary = new Dictionary<short, short>();
        public NBRS Neighbors;
        public NGBH Neighborhood;
        public Dictionary<short, FAMI> FamilyForHouse = new Dictionary<short, FAMI>();
        public Content ContentManager;

        public TS1NeighborhoodProvider(Content contentManager)
        {
            ContentManager = contentManager;
            MainResource = new IffFile(Path.Combine(contentManager.TS1BasePath, "UserData/Neighborhood.iff"));
            LotLocations = new IffFile(Path.Combine(contentManager.TS1BasePath, "UserData/LotLocations.iff"));
            var lotZoning = new IffFile(Path.Combine(contentManager.TS1BasePath, "UserData/LotZoning.iff"));

            var zones = lotZoning.Get<STR>(1);
            for (int i=0; i<zones.Length; i++)
            {
                var split = zones.GetString(i).Split(',');
                ZoningDictionary[short.Parse(split[0])] = (short)((split[1] == " community") ? 1 : 0);
            }
            Neighbors = MainResource.List<NBRS>().FirstOrDefault();
            Neighborhood = MainResource.List<NGBH>().FirstOrDefault();

            FamilyForHouse = new Dictionary<short, FAMI>();
            var families = MainResource.List<FAMI>();
            foreach (var fam in families)
            {
                FamilyForHouse[(short)fam.HouseNumber] = fam;
            }
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

        public IffFile GetHouse(int id)
        {
            return new IffFile(Path.Combine(ContentManager.TS1BasePath, "UserData/Houses/House"+id.ToString().PadLeft(2, '0')+".iff"));
        }

        public BMP GetHouseThumb(int id)
        {
            return GetHouse(id)?.Get<BMP>(512); //roof on
        }
    }
}
