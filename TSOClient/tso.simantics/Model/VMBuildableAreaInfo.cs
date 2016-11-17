using FSO.SimAntics.Entities;
using FSO.SimAntics.Model.TSOPlatform;
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

        public static int[] ObjectLimitPerPerson = new int[]
        {
            75,
            92,
            109,
            126,
            143,
            160,
            177,
            194,

            211,
            228,
            245,
            262,
            279,
            300
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

        public static int GetObjectLimit(VM vm)
        {
            var lotInfo = vm.TSOState;
            var lotSize = lotInfo.Size & 255;
            var lotFloors = (lotInfo.Size >> 8) & 255;
            var sizeMode = ObjectLimitPerPerson[lotSize + lotFloors];
            return sizeMode * Math.Max(1, lotInfo.Roommates.Count);
        }

        public static void UpdateOverbudgetObjects(VM vm)
        {
            var limit = GetObjectLimit(vm);
            vm.TSOState.ObjectLimit = limit;

            var multInUse = new HashSet<VMMultitileGroup>();
            foreach (var ava in vm.Context.ObjectQueries.Avatars)
            {
                if (ava.Thread == null) continue;
                foreach (var frame in ava.Thread.Stack)
                {
                    if (frame.Callee != null && frame.Callee is VMGameObject) multInUse.Add(frame.Callee.MultitileGroup);
                }
            }

            int i = 0;
            var ordered = vm.Context.ObjectQueries.MultitileByPersist.Values.OrderBy(x => x.BaseObject.ObjectID);
            foreach (var obj in ordered)
            {
                var isPortal = (obj.Objects.Count > 0) && (obj.BaseObject is VMGameObject) && ((VMGameObject)obj.BaseObject).PartOfPortal();
                foreach (var o in obj.Objects)
                {
                    if (o is VMGameObject)
                    {
                        if (i >= limit)
                        {
                            ((VMGameObject)o).Disabled |= VMGameObjectDisableFlags.ObjectLimitExceeded;
                            ((VMGameObject)o).Disabled &= ~VMGameObjectDisableFlags.ObjectLimitThreadDisable;
                            if (!multInUse.Contains(obj) && !o.GetFlag(VMEntityFlags.Occupied) && !isPortal) ((VMGameObject)o).Disabled |= VMGameObjectDisableFlags.ObjectLimitThreadDisable;
                        }
                        else ((VMGameObject)o).Disabled &= ~(VMGameObjectDisableFlags.ObjectLimitExceeded | VMGameObjectDisableFlags.ObjectLimitThreadDisable);
                    }
                }
                i++;
            }

            vm.TSOState.LimitExceeded = vm.Context.ObjectQueries.NumUserObjects > limit;
        }
    }
}
