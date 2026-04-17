using FSO.Common.Domain.RealestateDomain;
using FSO.Common.Domain.Shards;
using FSO.Content.Model;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;

namespace FSO.Common.Domain.Realestate
{
    public class RealestateDomain : IRealestateDomain
    {
        // No need to check redundant regex conditions until you have to throw various errors
        // (vide tso.client/UI/Panels/UILotPurchaseDialog.cs)
        // I tried to combine conditions to reduce redundancy
        private Regex VALIDATE_SPECIAL_CHARS = new Regex(@"[^\p{L} '-]"); // Numbers are special chars in this case
        private Regex VALIDATE_APOSTROPHES = new Regex("^[^']*'?[^']*$");
        private Regex VALIDATE_DASHES = new Regex("^[^-]*-?[^-]*$");
        private Regex VALIDATE_SPACES = new Regex("^[^ ]+(?: [^ ]+)*$");

        private Dictionary<int, ShardRealestateDomain> _ByShard;
        private IShardsDomain _Shards;
        private FSO.Content.Content _Content;

        public RealestateDomain(IShardsDomain shards, FSO.Content.Content content)
        {
            _Shards = shards;
            _Content = content;
            _ByShard = new Dictionary<int, ShardRealestateDomain>();

            foreach (var item in shards.All)
            {
                GetByShard(item.Id);
            }
        }

        public IShardRealestateDomain GetByShard(int shardId)
        {
            lock (_ByShard)
            {
                if (_ByShard.ContainsKey(shardId))
                {
                    return _ByShard[shardId];
                }

                var shard = _Shards.GetById(shardId);
                var map = _Shards.GetMapForId(shardId);
                var item = new ShardRealestateDomain(shard, map);
                _ByShard.Add(shardId, item);
                return item;
            }
        }

        public bool ValidateLotName(string name)
        {
            if (string.IsNullOrEmpty(name) ||
                name.Length < 3 ||
                name.Length > 24 ||
                VALIDATE_SPECIAL_CHARS.IsMatch(name) ||
                !VALIDATE_APOSTROPHES.IsMatch(name) ||
                !VALIDATE_DASHES.IsMatch(name) ||
                !VALIDATE_SPACES.IsMatch(name))
            {
                return false;
            }
            return true;
        }
    }

    public class ShardRealestateDomain : IShardRealestateDomain
    {
        private LotPricingStrategy _Pricing;
        private CityMap _Map;

        public bool Dynamic => true;
        private CityMap _BaseMap;
        private CityMap _PreTempMap;
        private Rectangle? _TempChangeBounds;

        private List<CityEditBase> _Commands = [];
        private CityEditBase _MyTempCommand;
        private List<CityEditBase> _TempCommands = [];

        public event Action<Rectangle> OnMapChange;

        public ShardRealestateDomain(ShardStatusItem shard, CityMap map)
        {
            _Map = map;
            //TODO: Hardcore
            _Pricing = new BasicLotPricingStrategy();
        }

        public int GetPurchasePrice(ushort x, ushort y)
        {
            return _Pricing.GetPrice(_Map, x, y);
        }

        public bool IsOpenable(ushort x, ushort y)
        {
            // Can't open lots out of bounds.
            if (!MapCoordinates.InBounds(x, y, 1))
            {
                //Out of bounds!
                return false;
            }

            // All-water lots have nowhere for players to stand.
            var terrain = _Map.GetTerrain(x, y);
            if (terrain == TerrainType.WATER) {
                // Only openable if any side of the terrain has a road.
                // TODO: When the terrain restore supports putting the mailbox on corners, allow those too.

                var road = _Map.GetRoad(x, y);
                return (road & 0xF) != 0;
            }

            return true;
        }

        public bool IsPurchasable(ushort x, ushort y)
        {
            //Cant buy lots on the very edge
            if (!MapCoordinates.InBounds(x, y, 1))
            {
                //Out of bounds!
                return false;
            }

            //Cant build on water
            var terrain = _Map.GetTerrain(x, y);
            if (terrain == TerrainType.WATER) { return false; }

            var slope = GetSlope(x, y);

            //10 is threshold for now
            return (slope < 10);
        }

        public int GetSlope(ushort x, ushort y)
        {
            x += 1;
            //Check elevation is ok, get all 4 corners and then decide
            var tl = _Map.GetElevation(x, y);
            var trPoint = MapCoordinates.Offset(x, y, 1, 0);
            var tr = _Map.GetElevation(trPoint.X, trPoint.Y);
            var blPoint = MapCoordinates.Offset(x, y, 0, 1);
            var bl = _Map.GetElevation(blPoint.X, blPoint.Y);
            var brPoint = MapCoordinates.Offset(x, y, 1, 1);
            var br = _Map.GetElevation(brPoint.X, brPoint.Y);

            int max = Math.Max(tl, Math.Max(tr, Math.Max(bl, br)));
            int min = Math.Min(tl, Math.Min(tr, Math.Min(bl, br)));

            return (max - min);
        }

        public CityMap GetMap()
        {
            return _Map;
        }

        public int AppendCommand(CityEditBase command)
        {
            if (_TempChangeBounds != null)
            {
                // Undo any temp changes so we can apply the command for real
                _Map.Set(_PreTempMap);
            }

            // When a command appears for real, remove it from the temp command set.
            _TempCommands.RemoveAll(x => x.AvatarId == command.AvatarId && x.UserModId == command.UserModId);

            if (!CityMapUtils.ValidateCommand(_Map, command))
            {
                return -1;
            }

            int index = _Commands.Count;

            _Commands.Add(command);

            CityMapUtils.ApplyCommand(_Map, command);

            if (OnMapChange != null)
            {
                var bound = CityMapUtils.GetBounds(_Map, command);

                if (bound != null)
                {
                    _PreTempMap?.Set(_Map);
                    ApplyTempCommands(bound);
                }
            }

            return index;
        }

        /// <summary>
        /// Set the temp command for this client (modifications the client is performing).
        /// </summary>
        /// <param name="command"></param>
        public void SetMyTempCommand(CityEditBase command)
        {
            bool redraw = true;
            if (command == null)
            {
                redraw = _TempCommands.Remove(_MyTempCommand);
            }
            else
            {
                var matching = _TempCommands.FindIndex(x => x.AvatarId == command.AvatarId && x.UserModId == command.UserModId);

                if (matching != -1)
                {
                    _TempCommands[matching] = command;
                }
                else
                {
                    _TempCommands.Add(command);
                }
            }

            _MyTempCommand = command;

            if (redraw)
            {
                ApplyTempCommands();
            }
        }

        private Rectangle? Union(Rectangle? first, Rectangle? second)
        {
            if (!first.HasValue)
            {
                return second;
            }
            else if (!second.HasValue)
            {
                return first;
            }
            else
            {
                return Rectangle.Union(first.Value, second.Value);
            }
        }

        public void ApplyTempCommands(Rectangle? bounds = null)
        {
            // If we don't have a pre-temp copy, make it now.
            if (_PreTempMap == null && _TempCommands.Count > 0)
            {
                _PreTempMap = new(_Map);
            }

            // If there were previous temp changes, roll them back so we can apply the new ones
            if (_TempChangeBounds != null)
            {
                bounds = Union(bounds, _TempChangeBounds);
                _Map.Set(_PreTempMap);
            }

            Rectangle? tempBounds = null;
            foreach (var temp in _TempCommands)
            {
                if (CityMapUtils.ValidateCommand(_Map, temp) && CityMapUtils.ApplyCommand(_Map, temp))
                {
                    var modBounds = CityMapUtils.GetBounds(_Map, temp);

                    tempBounds = Union(tempBounds, modBounds);
                }
            }

            bounds = Union(tempBounds, bounds);
            if (bounds != null)
            {
                OnMapChange?.Invoke(bounds.Value);
            }

            _TempChangeBounds = tempBounds;
        }
    }
}