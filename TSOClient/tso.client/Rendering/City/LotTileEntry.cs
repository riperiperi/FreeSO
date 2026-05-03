using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using Microsoft.Xna.Framework;

namespace FSO.Client.Rendering.City
{
    public class CityLotTiles
    {
        private readonly Dictionary<uint, LotTileEntry> TileByID = [];
        private readonly HashSet<uint> UpdateOnlineSet = [];
        private readonly HashSet<uint> DeletedSet = [];

        public readonly Dictionary<Vector2, LotTileEntry> TileByVector = [];
        public readonly HashSet<int> OccupiedTilesBase = [];
        private readonly HashSet<uint> OnlineTiles = [];

        private HashSet<int> OccupiedTilesCopy;
        public HashSet<int> OccupiedTiles
        {
            get
            {
                if (OccupiedTilesCopy == null)
                {
                    OccupiedTilesCopy = [.. OccupiedTilesBase];
                }

                return OccupiedTilesCopy;
            }
        }

        public IEnumerable<LotTileEntry> List => TileByID.Values;

        private static Vector2 GetVectorForId(uint id)
        {
            return new Vector2((short)(id >> 16), (short)(id & 0xFFFF));
        }

        private static int GetOccupiedTileId(LotTileEntry tile)
        {
            return (int)tile.y * 512 + (int)tile.x;
        }

        public bool UpdateWithCity(Common.DataService.Model.City city, IClientDataService dataService)
        {
            var entries = TileByID;
            var deletedSet = DeletedSet;
            var updateOnlineSet = UpdateOnlineSet;

            var byVector = TileByVector;
            var occupied = OccupiedTilesBase;
            var online = OnlineTiles;

            deletedSet.Clear();
            deletedSet.UnionWith(entries.Keys);

            updateOnlineSet.Clear();

            int newCount = 0;
            int onlineChangeCount = 0;

            foreach (var property in city.City_ReservedLotInfo)
            {
                deletedSet.Remove(property.Key);

                if (entries.TryGetValue(property.Key, out var entry))
                {
                    var wasOnline = entry.flags.HasFlag(LotTileFlags.Online);

                    if (wasOnline != property.Value)
                    {
                        updateOnlineSet.Add(property.Key);
                        onlineChangeCount++;

                        if (property.Value)
                        {
                            online.Add(property.Key);
                        }
                        else
                        {
                            online.Remove(property.Key);
                        }
                    }

                    entry.flags = property.Value ? LotTileFlags.Online : 0;
                }
                else
                {
                    entry = new LotTileEntry((int)property.Key, (short)(property.Key >> 16), (short)(property.Key & 0xFFFF), property.Value ? LotTileFlags.Online : 0);
                    entries[property.Key] = entry;
                    byVector[GetVectorForId(property.Key)] = entry;
                    occupied.Add(GetOccupiedTileId(entry));

                    if (property.Value)
                    {
                        online.Add(property.Key);

                        // Lot_IsOnline starts as false, so we need to set it to true.
                        updateOnlineSet.Add(property.Key);
                    }

                    newCount++;
                }
            }

            foreach (var spot in city.City_SpotlightsVector)
            {
                if (entries.TryGetValue(spot, out var entry))
                {
                    entry.flags |= LotTileFlags.Spotlight;
                }
            }

            foreach (var delete in deletedSet)
            {
                occupied.Remove(GetOccupiedTileId(entries[delete]));

                entries.Remove(delete);
                byVector.Remove(GetVectorForId(delete));
            }

            if (updateOnlineSet.Count > 0)
            {
                dataService.GetMany<Lot>([.. updateOnlineSet.Select(x => (object)x)]).ContinueWith(x =>
                {
                    if (!x.IsCompleted)
                    {
                        return;
                    }

                    var entries = TileByID;

                    foreach (var lot in x.Result)
                    {
                        if (entries.TryGetValue(lot.Id, out var mapItem))
                        {
                            lot.Lot_IsOnline = (mapItem.flags & LotTileFlags.Online) == LotTileFlags.Online;
                        }
                    }
                });
            }

            if (newCount > 0 || deletedSet.Count > 0)
            {
                // Force the terrain to build a new one if it's required to generate foliage.
                OccupiedTilesCopy = null;
            }

            return newCount > 0 || deletedSet.Count > 0 || onlineChangeCount > 0;
        }

        public void AddLocationsTo(HashSet<uint> locations)
        {
            foreach (var pair in TileByID)
            {
                locations.Add(pair.Key);
            }
        }

        public void AddOpenLotSurroundingsTo(HashSet<uint> locations)
        {
            foreach (var tile in OnlineTiles)
            {
                locations.Add(tile);
                locations.Add(tile - 1);
                locations.Add(tile + 1);

                uint axis = 1u << 16;
                locations.Add(tile + axis);
                locations.Add(tile + axis - 1);
                locations.Add(tile + axis + 1);

                locations.Add(tile - axis);
                locations.Add((tile - axis) - 1);
                locations.Add((tile - axis) + 1);
            }
        }
    }

    public class LotTileEntry
    {
        public int lotid;
        public int packed_pos
        {
            get
            {
                return ((x << 16) | y);
            }
        }
        public short x;
        public short y;
        public LotTileFlags flags; //bit 0 = online, bit 1 = spotlight, bit 2 = locked, bit 3 = occupied, other bits free for whatever use

        public LotTileEntry(int Lotid, short X, short Y, LotTileFlags Flags)
        {
            this.lotid = Lotid;
            this.x = X;
            this.y = Y;
            this.flags = Flags;
        }
    }

    [Flags]
    public enum LotTileFlags
    {
        Online = 0x1,
        Spotlight = 0x2,
        Locked = 0x4,
        Occupied = 0x8
    }
}
