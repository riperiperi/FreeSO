using Microsoft.Xna.Framework;

namespace FSO.Common.Model
{
    public enum LotTransitionType
    {
        None,
        DirectControl,
        Routing
    }

    public class LotTransitionInfo
    {
        public uint BeforeLocation;
        public int RelativeChangeX;
        public int RelativeChangeY;

        public int AvatarLotTilePosX;
        public int AvatarLotTilePosY;
        public float AvatarDirection;

        public LotTransitionType Type;
        public uint RoutingTargetLocation;
        public int RoutingLotTilePosX;
        public int RoutingLotTilePosY;

        /// <summary>
        /// By default, relative change x/y are in lot space, for calculation of the new lot tile pos on the target lot.
        /// This function converts them to offsets usable for city map coordinates.
        /// </summary>
        /// <param name="relativeChange">Relative change in lot space</param>
        /// <returns>Relative change in city space</returns>
        public static Point RelativeChangeLotToCity(Point relativeChange)
        {
            return new Point(-relativeChange.Y, relativeChange.X);
        }

        /// <summary>
        /// This function converts relative city tile x/y into lot space.
        /// </summary>
        /// <param name="relativeChange">Relative change in city space</param>
        /// <returns>Relative change in lot space</returns>
        public static Point RelativeChangeCityToLot(Point relativeChange)
        {
            return new Point(relativeChange.Y, -relativeChange.X);
        }

        /// <summary>
        /// Gets a mask for the surrounding lots that need to be reloaded for this transition.
        /// In order (-1, -1), (0, -1), (1, -1), (-1, 0)...
        /// Bits that aren't set should be able to copy surrounding lots from the previous lot, rather than re-initialzing them.
        /// </summary>
        /// <returns></returns>
        public uint GetSurroundingLotMask()
        {
            uint updateMask = 0b111111111;
            var cityOffset = RelativeChangeLotToCity(new Point(RelativeChangeX, RelativeChangeY));

            int i = 0;
            for (int y = -1; y < 2; y++)
            {
                for (int x = -1; x < 2; x++)
                {
                    uint bit = 1u << i;

                    // If this lot was present as an old surround lot, we can inherit it.
                    var oldX = x + cityOffset.X;
                    var oldY = y + cityOffset.Y;

                    if (Math.Abs(oldX) < 2 && Math.Abs(oldY) < 2 && !(oldX == 0 && oldY == 0))
                    {
                        // Within bounds, not the source lot.
                        updateMask &= ~bit;
                    }

                    i++;
                }
            }

            return updateMask;
        }

        public int GetOldSubworldForIndex(int index)
        {
            var cityOffset = RelativeChangeLotToCity(new Point(RelativeChangeX, RelativeChangeY));

            index += cityOffset.X;
            index += cityOffset.Y * 3;

            return index;
        }
    }
}
