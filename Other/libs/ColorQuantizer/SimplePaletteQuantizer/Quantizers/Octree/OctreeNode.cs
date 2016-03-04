using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SimplePaletteQuantizer.Quantizers.Octree
{
    internal class OctreeNode
    {
        #region | Fields |

        private static readonly Byte[] Mask = new Byte[] { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };

        private Int32 red;
        private Int32 green;
        private Int32 blue;

        private Int32 pixelCount;
        private Int32 paletteIndex;

        private readonly OctreeNode[] nodes;

        #endregion

        #region | Constructors |

        /// <summary>
        /// Initializes a new instance of the <see cref="OctreeNode"/> class.
        /// </summary>
        public OctreeNode(Int32 level, OctreeQuantizer parent)
        {
            nodes = new OctreeNode[8];

            if (level < 7)
            {
                parent.AddLevelNode(level, this);
            }
        }

        #endregion

        #region | Calculated properties |

        /// <summary>
        /// Gets a value indicating whether this node is a leaf.
        /// </summary>
        /// <value><c>true</c> if this node is a leaf; otherwise, <c>false</c>.</value>
        public Boolean IsLeaf
        {
            get { return pixelCount > 0; }
        }

        /// <summary>
        /// Gets the averaged leaf color.
        /// </summary>
        /// <value>The leaf color.</value>
        public Color Color
        {
            get
            {
                Color result;

                // determines a color of the leaf
                if (IsLeaf)
                {
                    result = pixelCount == 1 ? 
                        Color.FromArgb(255, red, green, blue) : 
                        Color.FromArgb(255, red/pixelCount, green/pixelCount, blue/pixelCount);
                }
                else
                {
                    throw new InvalidOperationException("Cannot retrieve a color for other node than leaf.");
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the active nodes pixel count.
        /// </summary>
        /// <value>The active nodes pixel count.</value>
        public Int32 ActiveNodesPixelCount
        {
            get
            {
                Int32 result = pixelCount;

                // sums up all the pixel presence for all the active nodes
                for (Int32 index = 0; index < 8; index++)
                {
                    OctreeNode node = nodes[index];

                    if (node != null)
                    {
                        result += node.pixelCount;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Enumerates only the leaf nodes.
        /// </summary>
        /// <value>The enumerated leaf nodes.</value>
        public IEnumerable<OctreeNode> ActiveNodes
        {
            get 
            {
                List<OctreeNode> result = new List<OctreeNode>();

                // adds all the active sub-nodes to a list
                for (Int32 index = 0; index < 8; index++)
                {
                    OctreeNode node = nodes[index];

                    if (node != null)
                    {
                        if (node.IsLeaf)
                        {
                            result.Add(node);
                        }
                        else
                        {
                            result.AddRange(node.ActiveNodes);
                        }
                    }
                }

                return result;
            }
        }

        #endregion

        #region | Methods |

        /// <summary>
        /// Adds the color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="level">The level.</param>
        /// <param name="parent">The parent.</param>
        public void AddColor(Color color, Int32 level, OctreeQuantizer parent)
        {
            // if this node is a leaf, then increase a color amount, and pixel presence
            if (level == 8)
            {
                red += color.R;
                green += color.G;
                blue += color.B;
                pixelCount++;
            }
            else if (level < 8) // otherwise goes one level deeper
            {
                // calculates an index for the next sub-branch
                Int32 index = GetColorIndexAtLevel(color, level);

                // if that branch doesn't exist, grows it
                if (nodes[index] == null)
                {
                    nodes[index] = new OctreeNode(level, parent);
                }

                // adds a color to that branch
                nodes[index].AddColor(color, level + 1, parent);
            }
        }

        /// <summary>
        /// Gets the index of the palette.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="level">The level.</param>
        /// <returns></returns>
        public Int32 GetPaletteIndex(Color color, Int32 level)
        {
            Int32 result;

            // if a node is leaf, then we've found are best match already
            if (IsLeaf)
            {
                result = paletteIndex;
            }
            else // otherwise continue in to the lower depths
            {
                Int32 index = GetColorIndexAtLevel(color, level);

                result = nodes[index] != null ? nodes[index].GetPaletteIndex(color, level + 1) : nodes.
                    Where(node => node != null).
                    First().
                    GetPaletteIndex(color, level + 1);
            }

            return result;
        }

        /// <summary>
        /// Removes the leaves by summing all it's color components and pixel presence.
        /// </summary>
        /// <returns></returns>
        public Int32 RemoveLeaves(Int32 level, Int32 activeColorCount, Int32 targetColorCount, OctreeQuantizer parent)
        {
            Int32 result = 0;

            // scans thru all the active nodes
            for (Int32 index = 0; index < 8; index++)
            {
                OctreeNode node = nodes[index];

                if (node != null)
                {
                    // sums up their color components
                    red += node.red;
                    green += node.green;
                    blue += node.blue;

                    // and pixel presence
                    pixelCount += node.pixelCount;

                    // increases the count of reduced nodes
                    result++;
                }
            }

            // returns a number of reduced sub-nodes, minus one because this node becomes a leaf
            return result - 1;
        }

        #endregion

        #region | Helper methods |

        /// <summary>
        /// Calculates the color component bit (level) index.
        /// </summary>
        /// <param name="color">The color for which the index will be calculated.</param>
        /// <param name="level">The bit index to be used for index calculation.</param>
        /// <returns>The color index at a certain depth level.</returns>
        private static Int32 GetColorIndexAtLevel(Color color, Int32 level)
        {
            return ((color.R & Mask[level]) == Mask[level] ? 4 : 0) |
                   ((color.G & Mask[level]) == Mask[level] ? 2 : 0) |
                   ((color.B & Mask[level]) == Mask[level] ? 1 : 0);
        }

        /// <summary>
        /// Sets a palette index to this node.
        /// </summary>
        /// <param name="index">The palette index.</param>
        internal void SetPaletteIndex(Int32 index)
        {
            paletteIndex = index;
        }

        #endregion
    }
}