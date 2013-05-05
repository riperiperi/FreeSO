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
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace TSOClient.Code.Rendering.City
{
    public enum TerrainType
    {
        Grass = 0,
        Snow = 1,
        Sand = 2,
        Rock = 3,
        Water = 4
    }

    public enum NeighbourDir
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    public class CityData
    {
        private static Dictionary<uint, byte> ColorToTerrain = new Dictionary<uint, byte>()
        {
            {0xFF00FF00, (byte)TerrainType.Grass},
            {0xFFFFFFFF, (byte)TerrainType.Snow},
            {0xFFFFFF00, (byte)TerrainType.Sand},
            {0xFFFF0000, (byte)TerrainType.Rock},
            {0xFF0C00FF, (byte)TerrainType.Water}
        };

        private static Dictionary<string, byte> BlendTable = new Dictionary<string, byte>()
        {
            {"0000", 0},
            {"0100", 1},
            {"1000", 2},
            {"1100", 3},
            {"0001", 4},
            {"0101", 5},
            {"1001", 6},
            {"1101", 7},
            {"0010", 8},
            {"0110", 9},
            {"1010", 10},
            {"1110", 11},
            {"0011", 12},
            {"0111", 13},
            {"1011", 14},
            {"1111", 15}
        };

        public int Width { get; set; }
        public int Height { get; set; }
        public float[] Elevation { get; set; }
        public Color[] VertexColor { get; set; }
        public byte[] Terrain { get; set; }
        public byte[] BackTerrain { get; set; }
        public byte[] BlendMap { get; set; }


        public float GetElevation(int x, int y)
        {
            return Elevation[(y * Width) + x];
        }

        public float GetElevation(int x, int y, float scale)
        {
            return Elevation[(y * Width) + x] * scale;
        }

        public float GetElevation(int x, int y, NeighbourDir dir, float defaultValue, float scale)
        {
            var offset = GetOffset(x, y, dir);
            if (offset == -1)
            {
                return defaultValue;
            }
            return Elevation[offset] * scale;
        }


        public byte GetTerrain(int x, int y)
        {
            return Terrain[y * Width + x];
        }

        public byte GetTerrain(int x, int y, NeighbourDir dir, byte defaultValue)
        {
            var offset = GetOffset(x, y, dir);
            if (offset == -1)
            {
                return defaultValue;
            }
            return Terrain[offset];
        }

        /// <summary>
        /// Gets the array offset for a given cell
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int GetOffset(int x, int y)
        {
            return y * Width + x;
        }

        /// <summary>
        /// Gets the array offset for a given cell's neighbour.
        /// Returns -1 if not valid e.g. north for row 0 etc.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public int GetOffset(int x, int y, NeighbourDir dir)
        {
            int yMod = 0;

            switch (dir)
            {
                case NeighbourDir.North:
                    if (y > 0)
                    {
                        return ((y - 1) * Width) + x;
                    }
                    return -1;

                case NeighbourDir.NorthEast:
                    if (y > 0 && x < Width - 1)
                    {
                        return ((y - 1) * Width) + x + 1;
                    }
                    return -1;

                case NeighbourDir.East:
                    if (x < Width - 1)
                    {
                        return (y * Width) + x + 1;
                    }
                    return -1;

                case NeighbourDir.SouthEast:
                    if (y < Height - 1 && x < Width - 1)
                    {
                        return ((y + 1) * Width) + x + 1;
                    }
                    return -1;

                case NeighbourDir.South:
                    if (y < Height - 1)
                    {
                        return ((y + 1) * Width) + x;
                    }
                    return -1;

                case NeighbourDir.SouthWest:
                    if (y < Height - 1 && x > 0)
                    {
                        return ((y + 1) * Width) + x - 1;
                    }
                    return -1;

                case NeighbourDir.West:
                    if (x > 0)
                    {
                        return (y * Width) + x - 1;
                    }
                    return -1;

                case NeighbourDir.NorthWest:
                    if (y > 0 && x > 0)
                    {
                        return ((y - 1) * Width) + x - 1;
                    }
                    return -1;
            }

            //{
            //    case NeighbourDir.North:
            //        yMod = (y % 2);
            //        if (y > 0 && x < Width - yMod)
            //        {
            //            return ((y - 1) * Width) + x + yMod;
            //        }
            //        return -1;

            //    case NeighbourDir.NorthEast:
            //        if (x < Width - 1)
            //        {
            //            return y * Width + x + 1;
            //        }
            //        return -1;


            //    case NeighbourDir.East:
            //        yMod = (y % 2);
            //        if(y < Height - 1 && x < Width - yMod){
            //            return ((y + 1) * Width) + x + yMod;
            //        }
            //        return -1;

            //    case NeighbourDir.South:
            //        yMod = (y % 2 == 0 ? 1 : 0);
            //        if (y < Height - 1 && x > yMod)
            //        {
            //            return ((y + 1) * Width) + x - yMod;
            //        }
            //        return -1;

            //    case NeighbourDir.SouthWest:
            //        if (x > 0)
            //        {
            //            return y * Width + x - 1;
            //        }
            //        return -1;

            //    case NeighbourDir.West:
            //        yMod = (y % 2 == 0 ? 1 : 0);
            //        if (y > 0 && x > yMod)
            //        {
            //            return ((y - 1) * Width) + x - yMod;
            //        }
            //        return -1;

            //    case NeighbourDir.SouthEast:
            //        if (y < Height - 2)
            //        {
            //            return ((y + 2) * Width) + x;
            //        }
            //        return -1;

            //    case NeighbourDir.NorthWest:
            //        if (y > 2)
            //        {
            //            return ((y - 2) * Width) + x;
            //        }
            //        return -1;
            //}
            return -1;
        }



        public static CityData Load(GraphicsDevice gd, string path)
        {
            //TODO: Load textures the correct way
            Texture2D elevationTexture = Texture2D.FromFile(gd, Path.Combine(path, "elevation.bmp"));
            Texture2D terrainTexture = Texture2D.FromFile(gd, Path.Combine(path, "terraintype.bmp"));
            Texture2D vertexTexture = Texture2D.FromFile(gd, Path.Combine(path, "vertexcolor.bmp"));

            var width = 205;
            var height = 606; //613
            var mapWidth = elevationTexture.Width;
            var mapHeight = elevationTexture.Height;

            /** Get data from textures **/
            var elevationRaw = new Color[mapWidth * mapHeight];
            elevationTexture.GetData(elevationRaw);
            elevationTexture.Dispose();

            var terrainRaw = new Color[mapWidth * mapHeight];
            terrainTexture.GetData(terrainRaw);
            terrainTexture.Dispose();

            var vertexRaw = new Color[mapWidth * mapHeight];
            vertexTexture.GetData(vertexRaw);
            vertexTexture.Dispose();


            /** Result objects **/
            float[] elevation = new float[width * height];
            byte[] terrain = new byte[width * height];
            Color[] vertex = new Color[width * height];
            byte[] backTerrains = new byte[width * height];
            byte[] blendMap = new byte[width * height];

            var rbmp = new System.Drawing.Bitmap(512, 512);
            //height = 300;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //x = (306 + x) - floor(y / 2)
                    //y = ceil(y/2) + x
                    var srcY = y;
                    var mapX = (x + 306) - (int)Math.Floor((double)srcY / 2);
                    var mapY = (int)Math.Ceiling((double)srcY / 2) + x;
                    var mapOffset = mapX + (mapY * mapWidth);
                    var resultOffset = (y * width) + x;

                    var dcolor = System.Drawing.Color.FromArgb((int)terrainRaw[mapOffset].PackedValue);
                    rbmp.SetPixel(mapX, mapY, dcolor);

                    elevation[resultOffset] = ((float)((float)elevationRaw[mapOffset].R / (float)255.0));
                    vertex[resultOffset] = vertexRaw[mapOffset];
                    terrain[resultOffset] = ColorToTerrain[terrainRaw[mapOffset].PackedValue];
                }
            }

            //rbmp.Save(@"C:\Users\Darren\Desktop\TSO\mapExport.bmp");
            elevationRaw = null;
            vertexRaw = null;
            terrainRaw = null;

            var result = new CityData
            {
                Width = width,
                Height = height,
                Elevation = elevation,
                VertexColor = vertex,
                Terrain = terrain,
                BackTerrain = backTerrains,
                BlendMap = blendMap
            };

            /**
             * Calculate blending info
             *  Loops at 4 cells around the current cell 
             *  and creates a pattern e.g. 1010 where 0 is same terrain type, 1 is dif
             *  
             *  That code is then mapped to a specific alpha map
             **/
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var myTerrain = terrain[(y * width) + x];
                    var myOffset = y * width + x;
                    var backTerrain = myTerrain;

                    var north = result.GetTerrain(x, y, NeighbourDir.North, myTerrain);
                    var east = result.GetTerrain(x, y, NeighbourDir.East, myTerrain);
                    var south = result.GetTerrain(x, y, NeighbourDir.South, myTerrain);
                    var west = result.GetTerrain(x, y, NeighbourDir.West, myTerrain);

                    /** No blend **/
                    var myBlend = (byte)15;


                    var key = (myTerrain == north ? 1 : 0).ToString() +
                                (myTerrain == east ? 1 : 0).ToString() +
                                (myTerrain == south ? 1 : 0).ToString() +
                                (myTerrain == west ? 1 : 0).ToString();

                    /*if (BlendTable.ContainsKey(key))
                    {
                        myBlend = BlendTable[key];
                    }

                    if (east == south && south != myTerrain)
                    {
                        myBlend = 18;
                    }*/

                    backTerrains[myOffset] = myTerrain;
                    blendMap[myOffset] = myBlend;
                    //BlendTable
                }
            }
            //44280000


            return result;
        }
    }
}
