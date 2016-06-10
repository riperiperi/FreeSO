using System;
using System.Collections.Generic;
using System.Drawing;

namespace SimplePaletteQuantizer.ColorCaches.Octree
{
    public class OctreeCacheNode
    {
        private static readonly Byte[] Mask = new Byte[] { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };

        private readonly OctreeCacheNode[] nodes;
        private readonly Dictionary<Int32, Color> entries;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctreeCacheNode"/> class.
        /// </summary>
        public OctreeCacheNode()
        {
            nodes = new OctreeCacheNode[8];
            entries = new Dictionary<Int32, Color>();
        }
        
        /// <summary>
        /// Adds the color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="paletteIndex">Index of the palette.</param>
        /// <param name="level">The level.</param>
        public void AddColor(Color color, Int32 paletteIndex, Int32 level)
        {
            // if this node is a leaf, then increase a color amount, and pixel presence
            entries.Add(paletteIndex, color);
            
            if (level < 8) // otherwise goes one level deeper
            {
                // calculates an index for the next sub-branch
                Int32 index = GetColorIndexAtLevel(color, level);

                // if that branch doesn't exist, grows it
                if (nodes[index] == null)
                {
                    nodes[index] = new OctreeCacheNode();
                }

                // adds a color to that branch
                nodes[index].AddColor(color, paletteIndex, level + 1);
            }
        }

        /// <summary>
        /// Gets the index of the palette.
        /// </summary>
        public Dictionary<Int32, Color> GetPaletteIndex(Color color, Int32 level)
        {
            Dictionary<Int32, Color> result = entries;
            
            if (level < 8)
            {
                Int32 index = GetColorIndexAtLevel(color, level);

                if (nodes[index] != null)
                {
                    result = nodes[index].GetPaletteIndex(color, level + 1);
                }
            }

            return result;
        }

        private static Int32 GetColorIndexAtLevel(Color color, Int32 level)
        {
            return ((color.R & Mask[level]) == Mask[level] ? 4 : 0) |
                   ((color.G & Mask[level]) == Mask[level] ? 2 : 0) |
                   ((color.B & Mask[level]) == Mask[level] ? 1 : 0);
        }
    }
}
