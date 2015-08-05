/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.LotView.Model;
using FSO.LotView.Components;

namespace FSO.SimAntics.Utils
{
    public class VMWorldExporter
    {

        public XmlHouseData housedata;

        public int GetDirection(ObjectComponent obj)
        {
            switch (obj.Direction)
            {
                case FSO.LotView.Model.Direction.WEST:
                    return 6;
                case FSO.LotView.Model.Direction.SOUTH:
                    return 4;
                case FSO.LotView.Model.Direction.EAST:
                    return 2;
                case FSO.LotView.Model.Direction.NORTH:
                    return 0;
                default:
                    return 0;
            }

        }

        public void SaveHouse(VM vm, string path)
        {

            if (vm.Context.Blueprint != null)
            {
                housedata = new XmlHouseData();
                housedata.World = new XmlHouseDataWorld();
                housedata.World.Floors = new List<XmlHouseDataFloor>();
                housedata.World.Walls = new List<XmlHouseDataWall>();
                housedata.Objects = new List<XmlHouseDataObject>();

            }

            var HouseWidth = vm.Context.Blueprint.Width;
            var HouseHeight = vm.Context.Blueprint.Height;
            var Levels = vm.Context.Blueprint.Stories;
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
