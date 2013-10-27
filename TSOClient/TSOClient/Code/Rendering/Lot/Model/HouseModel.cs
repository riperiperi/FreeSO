using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.Rendering.Lot.Components;
using TSOClient.Code.Data;

namespace TSOClient.Code.Rendering.Lot.Model
{
    public class HouseModel
    {
        private List<FloorComponent> _FloorList;
        private FloorComponent[,,] _FloorLookup;

        public FloorComponent GetFloor(int level, int x, int y)
        {
            return _FloorLookup[level, x, y];
        }

        public List<FloorComponent> GetFloors()
        {
            return _FloorList;
        }



        public void LoadHouse(HouseData data)
        {
            _FloorList = new List<FloorComponent>();
            _FloorLookup = new FloorComponent[2, data.Size, data.Size];
            foreach (var floor in data.World.Floors){

                var floorComponent = new FloorComponent()
                {
                    Position = new Microsoft.Xna.Framework.Point(floor.X, floor.Y),
                    Level = floor.Level,
                    FloorStyle = floor.Value
                };
                _FloorLookup[floor.Level, floor.X, floor.Y] = floorComponent;
                _FloorList.Add(floorComponent);
            }
        }
    }
}
