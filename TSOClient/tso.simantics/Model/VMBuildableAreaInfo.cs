using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model
{
    /// <summary>
    /// Manages how buildable area costs and scaling works. Final calculations are done serverside,
    /// so people modifying this will just result in rejected requests.
    /// </summary>
    public static class VMBuildableAreaInfo
    {
        /// <summary>
        /// Possible sizes in tiles of the buildable area.
        /// </summary>
        public static int[] BuildableSizes =
        {
            12,
            20,
            26,
            31,
            35,
            40, //original tso 39
            45, //43
            50, //46
            
            55, //new
            60,
            64
        };

        /// <summary>
        /// The price to increase size by 1 or add a floor. Having 64x64 5 floors is meant to be expensive!
        /// </summary>
        public static int[] UpgradePrices =
        {
            0,
            200,
            550,
            1200,
            2200,
            4000,
            7000,
            13000,

            //new: 3 size upgrades + 3 floor upgrades
            20000,
            28000,
            37000,
            47000,
            58000,
            70000
        };

        /// <summary>
        /// Note: levels above 8 do not require more than 8 roomies to have no penalty, as you can only have 8 roomies
        /// </summary>
        public static int[] LackRoomies =
        {
            0,
            200,
            650,
            1400,
            2900,
            5100,
            9100,
            16100
        };

        public static int CalculateBaseCost(int initialLevel, int targetLevel)
        {
            return UpgradePrices[targetLevel] - UpgradePrices[initialLevel];
        }

        public static int CalculateRoomieCost(int roomies, int initialLevel, int targetLevel)
        {
            roomies = Math.Min(8, Math.Max(roomies, 1)) - 1; //can't have 0 roomies, or more than 8. Start at 1.
            var lackAtCurrent = LackRoomies[Math.Max(0, Math.Min(7, initialLevel) - roomies)];
            var lackAtTarget = LackRoomies[Math.Max(0, Math.Min(7, targetLevel) - roomies)];
            return lackAtTarget - lackAtCurrent;
        }
    }
}
