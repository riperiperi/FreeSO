using System;
using System.Collections.Generic;
using System.Drawing;
using SimplePaletteQuantizer.Helpers;

namespace SimplePaletteQuantizer.Quantizers.Uniform
{
    /// <summary>
    /// In uniform quantization each axis of the color space is treated independently. 
    /// Each axis is then divided into equal sized segments. The planes perpendicular to 
    /// the axis' that pass through the division points then define regions in the color 
    /// space. The number of these regions is dependent on the scheme used for dividing the 
    /// color space. One possible scheme is to divide the red and green axis into 8 segments 
    /// each and the blue axis into 4 resulting in 256 regions. Another possibility is dividing 
    /// the red and blue into 6 and the green into 7 segments resulting in 252 regions [3]. Each 
    /// one of these regions will produce a color for the color map.
    /// 
    /// Once the color space has been divided each of the original colors is then mapped to the 
    /// region which it falls in. The representative colors for each region is then the average 
    /// of all the colors mapped to that region. Because each of the regions represents an entry
    /// in the color map, the same process for mapping the original colors to a region can be 
    /// repeated for mapping the original colors to colors in the color map. While this algorithm 
    /// is quick and easy to implement it does not yield very good results. Often region in the 
    /// color space will not have any colors mapped to them resulting in color map entries to be
    /// wasted.
    ///
    /// This algorithm can also be applied in a non-uniform manner if the axis are broken on a 
    /// logarithmic scale instead of linear. This will produce slightly better results because 
    /// the human eye cannot distinguish dark colors as well as bright ones.
    /// </summary>
    public class UniformQuantizer : BaseColorQuantizer
    {
        #region | Fields |

        private UniformColorSlot[] redSlots;
        private UniformColorSlot[] greenSlots;
        private UniformColorSlot[] blueSlots;

        #endregion

        #region << BaseColorQuantizer >>

        /// <summary>
        /// See <see cref="BaseColorQuantizer.OnPrepare"/> for more details.
        /// </summary>
        protected override void OnPrepare(ImageBuffer image)
        {
            redSlots = new UniformColorSlot[8];
            greenSlots = new UniformColorSlot[8];
            blueSlots = new UniformColorSlot[4];
        }

        /// <summary>
        /// See <see cref="BaseColorQuantizer.AddColor"/> for more details.
        /// </summary>
        protected override void OnAddColor(Color color, Int32 key, Int32 x, Int32 y)
        {
            base.OnAddColor(color, key, x, y);

            Int32 redIndex = color.R >> 5;
            Int32 greenIndex = color.G >> 5;
            Int32 blueIndex = color.B >> 6;

            redSlots[redIndex].AddValue(color.R);
            greenSlots[greenIndex].AddValue(color.G);
            blueSlots[blueIndex].AddValue(color.B);
        }

        /// <summary>
        /// See <see cref="BaseColorQuantizer.OnGetPalette"/> for more details.
        /// </summary>
        protected override List<Color> OnGetPalette(Int32 colorCount)
        {
            // use optimized palette, if any
            List<Color> optimizedPalette = base.OnGetPalette(colorCount);
            if (optimizedPalette != null) return optimizedPalette;

            // otherwise synthetize one
            List<Color> result = new List<Color>();

            // NOTE: I was considering either Lambda, or For loop (which should be the fastest), 
            // NOTE: but I used the ForEach loop for the sake of readability. Feel free to convert it.
            foreach (UniformColorSlot redSlot in redSlots)
            foreach (UniformColorSlot greenSlot in greenSlots)
            foreach (UniformColorSlot blueSlot in blueSlots)
            {
                Int32 red = redSlot.GetAverage();
                Int32 green = greenSlot.GetAverage();
                Int32 blue = blueSlot.GetAverage();

                Color color = Color.FromArgb(255, red, green, blue);
                result.Add(color);
            }

            // returns our new palette
            return result;
        }

        /// <summary>
        /// See <see cref="BaseColorQuantizer.OnGetPaletteIndex"/> for more details.
        /// </summary>
        protected override void OnGetPaletteIndex(Color color, Int32 key, Int32 x, Int32 y, out Int32 paletteIndex)
        {
            Int32 redIndex = color.R >> 5;
            Int32 greenIndex = color.G >> 5;
            Int32 blueIndex = color.B >> 6;
            paletteIndex = (redIndex << 5) + (greenIndex << 2) + blueIndex;
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
