﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.LotView.Utils
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
