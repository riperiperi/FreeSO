/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tso.world.utils
{
    /// <summary>
    /// Utility to help iterating through tiles in a depth sorted order
    /// </summary>
    public class IsometricTileIterator
    {
        public static IEnumerable<IsometricTile> Tiles(WorldRotation rotation, short startX, short startY, short width, short height)
        {
            return Tiles(rotation, startX, startY, width, height, 1, 1);
        }

        public static IEnumerable<IsometricTile> Tiles(WorldRotation rotation, short startX, short startY, short width, short height, short advanceX, short advanceY){
            List<IsometricTile> tiles = new List<IsometricTile>();

            var endX = startX + width;
            var endY = startY + height;

            for (var x = startX; x < endX; x += advanceX)
            {
                for (var y = startY; y < endY; y += advanceY)
                {
                    tiles.Add(new IsometricTile 
                    {
                        TileX = x,
                        TileY = y
                    });
                }
            }

            tiles.Sort(new IsometricTileSorter<IsometricTile>(rotation));

            foreach (var tile in tiles)
            {
                yield return tile;
            }
        }

    }

    public class IsometricTileSorter<T> : IComparer<T> where T : IIsometricTile
    {

        private WorldRotation Rotation;
        public IsometricTileSorter(WorldRotation rotation){
            this.Rotation = rotation;
        }

        #region IComparer<IIsometricTile> Members
        public int Compare(T x, T y)
        {
            switch (Rotation){
                case WorldRotation.TopLeft:
                    return (x.TileX + x.TileY).CompareTo((y.TileX + y.TileY));
                case WorldRotation.TopRight:
                    return (x.TileX - x.TileY).CompareTo((y.TileX - y.TileY));
            }
            return 0;
        }
        #endregion
    }

    public interface IIsometricTile {
        short TileX { get; }
        short TileY { get; }
    }

    public class IsometricTile : IIsometricTile
    {
        public short TileX { get; set; }
        public short TileY { get; set; }
    }
}
