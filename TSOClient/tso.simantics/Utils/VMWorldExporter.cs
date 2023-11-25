using System;
using System.Collections.Generic;
using FSO.LotView.Model;

namespace FSO.SimAntics.Utils
{
    public class VMWorldExporter
    {

        public XmlHouseData housedata;

        public void SaveHouse(VM vm, string path)
        {

            if (vm.Context.Architecture != null)
            {
                housedata = new XmlHouseData();
                housedata.World = new XmlHouseDataWorld();
                housedata.World.Floors = new List<XmlHouseDataFloor>();
                housedata.World.Walls = new List<XmlHouseDataWall>();
                housedata.Objects = new List<XmlHouseDataObject>();

            }

            var HouseWidth = vm.Context.Architecture.Width;
            var HouseHeight = vm.Context.Architecture.Height;
            var Levels = vm.Context.Architecture.Stories;
            housedata.Size = HouseWidth;

            for (short x = 0; x < HouseWidth; x++)
            {
                for (short y = 0; y < HouseHeight; y++)
                {
                    for (sbyte z = 1; z <= Levels; z++)
                        if (vm.Context.Architecture.GetFloor(x, y, z).Pattern != 0)
                        {
                            var Floor = vm.Context.Architecture.GetFloor(x, y, z);
                            housedata.World.Floors.Add(new XmlHouseDataFloor()
                            {
                                X = x,
                                Y = y,
                                Value = Floor.Pattern,
                                Level = z - 1
                            });
                        }

                    for (sbyte z = 1; z <= Levels; z++)
                    {
                        if (vm.Context.Architecture.GetWall(x, y, z).Segments != 0)
                        {
                            var Wall = vm.Context.Architecture.GetWall(x, y, z);
                            housedata.World.Walls.Add(new XmlHouseDataWall()
                            {
                                X = x,
                                Y = y,
                                Segments = Wall.Segments,
                                Placement = 0,
                                TopLeftPattern = Wall.TopLeftPattern,
                                TopRightPattern = Wall.TopRightPattern,
                                LeftStyle = Wall.TopLeftStyle,
                                RightStyle = Wall.TopRightStyle,
                                BottomLeftPattern = Wall.BottomLeftPattern,
                                BottomRightPattern = Wall.BottomRightPattern,
                                Level = z - 1

                            });
                        }
                    }
                }
            }

            foreach (var entity in vm.Entities)
            {
                if (entity != entity.MultitileGroup.BaseObject || entity is VMAvatar) continue;

                uint GUID = (entity.MultitileGroup.MultiTile)?entity.MasterDefinition.GUID:entity.Object.OBJ.GUID;

                housedata.Objects.Add(new XmlHouseDataObject()
                {
                    GUID = "0x" + GUID.ToString("X"),
                    X = entity.Position.TileX,
                    Y = entity.Position.TileY,
                    Level = (entity.Position == LotTilePos.OUT_OF_WORLD) ? 0 : entity.Position.Level,
                    Dir = (int)Math.Round(Math.Log((double)entity.Direction, 2))
                });
            }


            XmlHouseData.Save(path, housedata);

        }
    }
}
