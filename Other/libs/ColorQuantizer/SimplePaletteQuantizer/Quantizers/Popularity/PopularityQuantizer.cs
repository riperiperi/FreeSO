using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SimplePaletteQuantizer.ColorCaches;
using SimplePaletteQuantizer.ColorCaches.Octree;
using SimplePaletteQuantizer.Helpers;

namespace SimplePaletteQuantizer.Quantizers.Popularity
{
    /// <summary>
    /// Popularity algorithms are another form of uniform quantization. However, instead of 
    /// dividing the color space into 256 regions these algorithms break the color space into 
    /// much smaller, and consequently many more, regions. One possible implementation is to 
    /// divide the space into regions 4x4x4 in size (262,144 regions). The original colors are 
    /// again mapped to the region they fall in. The representative color for each region is the 
    /// average of the colors mapped to it. The color map is selected by taking the representative 
    /// colors of the 256 most popular regions (the regions that had the most colors mapped to them). 
    /// If a non-empty region is not selected for the color map its index into the color map (the 
    /// index that will be assigned to colors that map to that region) is then the entry in the color 
    /// map that is closest (Euclidean distance) to its representative color). 
    ///
    /// These algorithms are also easy to implement and yield better results than the uniform 
    /// quantization algorithm. They do, however, take slightly longer to execute and can have a 
    /// significantly larger storage requirement depending on the size of regions. Also depending 
    /// on the image characteristics this may not produce a good result. This can be said about all 
    /// uniform sub-division schemes, because the method for dividing the color space does utilize 
    /// any information about the image.
    /// </summary>
    public class PopularityQuantizer : BaseColorCacheQuantizer
    {
        #region | Fields |

        private List<Color> palette;
        private ConcurrentDictionary<Int32, PopularityColorSlot> colorMap;

        #endregion

        #region | Methods |

        private static Int32 GetColorIndex(Color color)
        {
            // determines the index by splitting the RGB cube to 4x4x4 (1 >> 2 = 4)
            Int32 redIndex = color.R >> 2;
            Int32 greenIndex = color.G >> 2;
            Int32 blueIndex = color.B >> 2;

            // calculates the whole unique index of the slot: Index = R*4096 + G*64 + B
            return (redIndex << 12) + (greenIndex << 6) + blueIndex;
        }

        #endregion

        #region << BaseColorCacheQuantizer >>

        /// <summary>
        /// See <see cref="BaseColorQuantizer.OnPrepare"/> for more details.
        /// </summary>
        protected override void OnPrepare(ImageBuffer image)
        {
            base.OnPrepare(image);

            palette = new List<Color>();
            colorMap = new ConcurrentDictionary<Int32, PopularityColorSlot>();
        }

        /// <summary>
        /// See <see cref="BaseColorQuantizer.OnAddColor"/> for more details.
        /// </summary>
        protected override void OnAddColor(Color color, Int32 key, Int32 x, Int32 y)
        {
            base.OnAddColor(color, key, x, y);
            Int32 index = GetColorIndex(color);
            colorMap.AddOrUpdate(index, colorKey => new PopularityColorSlot(color), (colorKey, slot) => slot.AddValue(color));
        }

        /// <summary>
        /// See <see cref="BaseColorCacheQuantizer.OnCreateDefaultCache"/> for more details.
        /// </summary>
        protected override IColorCache OnCreateDefaultCache()
        {
            // use OctreeColorCache best performance/quality
            return new OctreeColorCache();
        }

        /// <summary>
        /// See <see cref="BaseColorCacheQuantizer.OnGetPaletteToCache"/> for more details.
        /// </summary>
        protected override List<Color> OnGetPaletteToCache(Int32 colorCount)
        {
            // use fast random class
            FastRandom random = new FastRandom(0);

            // NOTE: I've added a little randomization here, as it was performing terribly otherwise.
            // sorts out the list by a pixel presence, takes top N slots, and calculates 
            // the average color from them, thus our new palette.
            IEnumerable<Color> colors = colorMap.
                 OrderBy(entry => random.Next(colorMap.Count)).
                 OrderByDescending(entry => entry.Value.PixelCount).
                 Take(colorCount).
                 Select(entry => entry.Value.GetAverage());

            palette.Clear();
            palette.AddRange(colors);
            return palette;
        }

        #endregion

        #region << IColorQuantizer >>

        /// <summary>
        /// See <see cref="IColorQuantizer.AllowParallel"/> for more details.
        /// </summary>
        public override Boolean AllowParallel
        {
            get { return true; }
        }

        #endregion
    }
}
