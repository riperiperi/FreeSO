using FSO.Content.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Domain.Realestate
{
    public interface LotPricingStrategy
    {
        int GetPrice(CityMap map, ushort x, ushort y);
    }

    public class BasicLotPricingStrategy : LotPricingStrategy
    {
        public int GetPrice(CityMap map, ushort x, ushort y)
        {
            /*if (x < 1 || y < 1){
                //Cant purchase
                return int.MaxValue;
            }

            if (x > 203 || y > 304) {
                //Cant purchase
                return int.MaxValue;
            }*/

            //TODO: Work on this scheme
            var terrain = map.GetTerrain(x, y);
            System.Diagnostics.Debug.WriteLine(x + ":" + y);
            System.Diagnostics.Debug.WriteLine(terrain);
            var basePrice = 3000;

            switch (terrain)
            {
                case TerrainType.GRASS:
                    basePrice += 1000;
                    break;
                case TerrainType.SAND:
                case TerrainType.SNOW:
                    basePrice += 2000;
                    break;
            }

            var price = basePrice;

            //Altitude increase price
            var elevation = map.GetElevation(x, y);

            //$19 for each elevation increment
            price += (19 * elevation);

            //+2500 for every water edge
            var leftLocation = MapCoordinates.Offset(new MapCoordinate(x, y), -1, 0);
            var leftTerrain = map.GetTerrain(leftLocation.X, leftLocation.Y);

            var rightLocation = MapCoordinates.Offset(new MapCoordinate(x, y), 1, 0);
            var rightTerrain = map.GetTerrain(rightLocation.X, rightLocation.Y);

            var topLocation = MapCoordinates.Offset(new MapCoordinate(x, y), 0, -1);
            var topTerrain = map.GetTerrain(topLocation.X, topLocation.Y);

            var bottomLocation = MapCoordinates.Offset(new MapCoordinate(x, y), 0, 1);
            var bottomTerrain = map.GetTerrain(bottomLocation.X, bottomLocation.Y);

            if (leftTerrain == TerrainType.WATER){
                price += 5000;
            }
            if (rightTerrain == TerrainType.WATER){
                price += 5000;
            }
            if (topTerrain == TerrainType.WATER){
                price += 5000;
            }
            if (bottomTerrain == TerrainType.WATER){
                price += 5000;
            }

            //Extra for an island
            if(bottomTerrain == TerrainType.WATER && topTerrain == TerrainType.WATER &&
                leftTerrain == TerrainType.WATER && rightTerrain == TerrainType.WATER){
                price += 10000;
            }

            return price;
        }
    }
}
