using FSO.Content;
using FSO.LotView.Model;
using System;
using System.Linq;

namespace FSO.SimAntics.Utils
{
    public static class VMArchitectureStats
    {
        public static WorldFloorProvider Floors;
        public static WorldWallProvider Walls;

        public static int GetArchValue(VMArchitecture arch)
        {
            Floors = Content.Content.Get().WorldFloors;
            Walls = Content.Content.Get().WorldWalls;

            int value = 0;
            for (int level = 0; level < arch.Stories; level++)
            {
                var walls = arch.Walls[level];
                var floors = arch.Floors[level];
                int index = 0;
                for (int y=0; y<arch.Height; y++)
                {
                    for (int x=0; x<arch.Width; x++)
                    {
                        if (arch.FineBuildableArea[index])
                        {
                            var floor = floors[index];
                            var wall = walls[index];

                            if (floor.Pattern > 0)
                            {
                                value += GetFloorPrice(floor.Pattern);
                            }
                            if (wall.Segments > 0)
                            {
                                if ((wall.Segments & WallSegments.AnyDiag) > 0)
                                {
                                    value += GetWallPrice(wall.TopRightStyle);

                                    if (wall.TopLeftPattern != 0) value += GetFloorPrice(wall.TopLeftPattern)/2;
                                    if (wall.TopLeftStyle != 0) value += GetFloorPrice(wall.TopLeftStyle)/2;

                                    if (wall.BottomLeftPattern != 0) value += GetPatternPrice(wall.BottomLeftPattern);
                                    if (wall.BottomRightPattern != 0) value += GetPatternPrice(wall.BottomRightPattern);
                                }
                                else
                                {
                                    if ((wall.Segments & WallSegments.TopLeft) > 0)
                                    {
                                        value += GetWallPrice(wall.TopLeftStyle);
                                        value += GetPatternPrice(wall.TopLeftPattern);
                                        var wall2 = walls[index - 1];
                                        value += GetPatternPrice(wall2.BottomRightPattern);
                                    }
                                    if ((wall.Segments & WallSegments.TopRight) > 0)
                                    {
                                        value += GetWallPrice(wall.TopRightStyle);
                                        value += GetPatternPrice(wall.TopRightPattern);
                                        var wall2 = walls[index - arch.Width];
                                        value += GetPatternPrice(wall2.BottomLeftPattern);
                                    }
                                }
                            }
                        }
                        index++;
                    }
                }
            }
            return value;
        }

        public static Tuple<int, int> GetObjectValue(VM vm)
        {
            var multitiles = vm.Entities.GroupBy(x => x.MultitileGroup);
            var value = 0;
            var archValue = 0;
            var count = 0;
            foreach (var mt in multitiles)
            {
                var bObj = mt.Key.BaseObject;
                if (!vm.Context.IsUserOutOfBounds(bObj.Position))
                {
                    if ((bObj.MasterDefinition ?? bObj.Object.OBJ).BuildModeType > 0)
                        archValue += mt.Key.Price;
                    else 
                        value += mt.Key.Price;
                }
                count++;
            }
            return new Tuple<int, int>(value, archValue);
        }

        private static int GetWallPrice(ushort id)
        {
            return Walls.GetWallStyle(id)?.Price ?? 0;
        }

        private static int GetPatternPrice(ushort id)
        {
            var pref = GetPatternRef(id);
            return (pref == null) ? 0 : pref.Price;
        }

        private static int GetFloorPrice(ushort id)
        {
            if (id == 1) return 0;
            var fref = GetFloorRef(id);
            return (fref == null) ? 0 : fref.Price;
        }

        private static WallReference GetPatternRef(ushort id)
        {
            WallReference result = null;
            Walls.Entries.TryGetValue(id, out result);
            return result;
        }
        private static FloorReference GetFloorRef(ushort id)
        {
            FloorReference result = null;
            Floors.Entries.TryGetValue(id, out result);
            return result;
        }
    }
}
