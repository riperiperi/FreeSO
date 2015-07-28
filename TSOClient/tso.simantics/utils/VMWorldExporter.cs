using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world.model;
using tso.world.components;

namespace TSO.Simantics.utils
{
    public class VMWorldExporter
    {

        public XmlHouseData housedata;

        public int GetDirection(ObjectComponent obj)
        {
            switch (obj.Direction)
            {
                case tso.world.model.Direction.WEST:
                    return 6;
                case tso.world.model.Direction.SOUTH:
                    return 4;
                case tso.world.model.Direction.EAST:
                    return 2;
                case tso.world.model.Direction.NORTH:
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

            for (short x = 0; x <= HouseWidth - 1; x++)

                for (short y = 0; y <= HouseHeight - 1; y++)      
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

                    if (vm.Context.Blueprint.GetObjects(x, y) != null)
                    {
                        int Count = vm.Context.Blueprint.GetObjects(x, y).Objects.Count;
                            for (int c = 0; c <= Count - 1; c++) 
                           {
                                uint GUID = 0;
                                
                                 var Object = vm.Context.Blueprint.GetObjects(x, y).Objects[c];
                                if (Object.Obj.OBJ.IsMultiTile && Object.Obj.OBJ.SubIndex == -1)
                                   GUID = Object.Obj.OBJ.GUID;
                                else
                                   GUID = Object.Obj.OBJ.GUID;

                                if (GUID != 0 && GUID != 1922844738)
                                  housedata.Objects.Add(new XmlHouseDataObject()
                                      {

                                       GUID = "0x" + GUID.ToString("X"),
                                       X = (int)Object.TileX,
                                       Y = (int)Object.TileY,
                                       Level = (int)Object.Level,
                                       Dir = GetDirection(Object)
                                       });

                                
                            }
                        }
                }


            XmlHouseData.Save(path, housedata);

            }
    }
}
