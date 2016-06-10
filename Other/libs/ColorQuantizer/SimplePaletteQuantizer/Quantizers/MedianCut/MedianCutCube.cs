using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SimplePaletteQuantizer.ColorCaches.Common;
using SimplePaletteQuantizer.Helpers;

namespace SimplePaletteQuantizer.Quantizers.MedianCut
{
    internal class MedianCutCube
    {
        #region | Fields |

        // red bounds
        private Int32 redLowBound;
        private Int32 redHighBound;

        // green bounds
        private Int32 greenLowBound;
        private Int32 greenHighBound;

        // blue bounds
        private Int32 blueLowBound;
        private Int32 blueHighBound;

        private readonly ICollection<Int32> colorList;

        #endregion

        #region | Properties |

        /// <summary>
        /// Gets the color model.
        /// </summary>
        public ColorModel ColorModel { get; private set; }

        /// <summary>
        /// Gets or sets the index of the palette.
        /// </summary>
        /// <value>The index of the palette.</value>
        public Int32 PaletteIndex { get; private set; }

        #endregion

        #region | Constructors |

        /// <summary>
        /// Initializes a new instance of the <see cref="MedianCutCube"/> class.
        /// </summary>
        /// <param name="colors">The colors.</param>
        public MedianCutCube(ICollection<Int32> colors)
        {
            ColorModel = ColorModel.RedGreenBlue;
            colorList = colors;
            Shrink();
        }

        #endregion

        #region | Calculated properties |

        /// <summary>
        /// Gets the size of the red side of this cube.
        /// </summary>
        /// <value>The size of the red side of this cube.</value>
        public Int32 RedSize
        {
            get { return redHighBound - redLowBound; }
        }

        /// <summary>
        /// Gets the size of the green side of this cube.
        /// </summary>
        /// <value>The size of the green side of this cube.</value>
        public Int32 GreenSize
        {
            get { return greenHighBound - greenLowBound; }
        }

        /// <summary>
        /// Gets the size of the blue side of this cube.
        /// </summary>
        /// <value>The size of the blue side of this cube.</value>
        public Int32 BlueSize
        {
            get { return blueHighBound - blueLowBound; }
        }

        /// <summary>
        /// Gets the average color from the colors contained in this cube.
        /// </summary>
        /// <value>The average color.</value>
        public Color Color
        {
            get
            {
                Int32 red = 0, green = 0, blue = 0;

                foreach (Int32 argb in colorList)
                {
                    Color color = Color.FromArgb(argb);
                    red += ColorModelHelper.GetComponentA(ColorModel, color);
                    green += ColorModelHelper.GetComponentB(ColorModel, color);
                    blue += ColorModelHelper.GetComponentC(ColorModel, color);
                }

                red = colorList.Count == 0 ? 0 : red / colorList.Count;
                green = colorList.Count == 0 ? 0 : green / colorList.Count;
                blue = colorList.Count == 0 ? 0 : blue / colorList.Count;

                // ColorModelHelper.HSBtoRGB(Convert.ToInt32(red/ColorModelHelper.HueFactor), green / 255.0f, blue / 255.0f);

                Color result = Color.FromArgb(255, red, green, blue);
                return result;
            }
        }

        #endregion

        #region | Methods |

        /// <summary>
        /// Shrinks this cube to the least dimensions that covers all the colors in the RGB space.
        /// </summary>
        private void Shrink()
        {
            redLowBound = greenLowBound = blueLowBound = 255;
            redHighBound = greenHighBound = blueHighBound = 0;

            foreach (Int32 argb in colorList)
            {
                Color color = Color.FromArgb(argb);

                Int32 red = ColorModelHelper.GetComponentA(ColorModel, color);
                Int32 green = ColorModelHelper.GetComponentB(ColorModel, color);
                Int32 blue = ColorModelHelper.GetComponentC(ColorModel, color);

                if (red < redLowBound) redLowBound = red;
                if (red > redHighBound) redHighBound = red;
                if (green < greenLowBound) greenLowBound = green;
                if (green > greenHighBound) greenHighBound = green;
                if (blue < blueLowBound) blueLowBound = blue;
                if (blue > blueHighBound) blueHighBound = blue;
            }
        }

        /// <summary>
        /// Splits this cube's color list at median index, and returns two newly created cubes.
        /// </summary>
        /// <param name="componentIndex">Index of the component (red = 0, green = 1, blue = 2).</param>
        /// <param name="firstMedianCutCube">The first created cube.</param>
        /// <param name="secondMedianCutCube">The second created cube.</param>
        public void SplitAtMedian(Byte componentIndex, out MedianCutCube firstMedianCutCube, out MedianCutCube secondMedianCutCube)
        {
            List<Int32> colors;

            switch (componentIndex)
            {
                // red colors
                case 0:
                    colors = colorList.OrderBy(argb => ColorModelHelper.GetComponentA(ColorModel, Color.FromArgb(argb))).ToList();
                    break;

                // green colors
                case 1:
                    colors = colorList.OrderBy(argb => ColorModelHelper.GetComponentB(ColorModel, Color.FromArgb(argb))).ToList();
                    break;

                // blue colors
                case 2:
                    colors = colorList.OrderBy(argb => ColorModelHelper.GetComponentC(ColorModel, Color.FromArgb(argb))).ToList();
                    break;

                default:
                    throw new NotSupportedException("Only three color components are supported (R, G and B).");

            }

            // retrieves the median index (a half point)
            Int32 medianIndex = colorList.Count >> 1;

            // creates the two half-cubes
            firstMedianCutCube = new MedianCutCube(colors.GetRange(0, medianIndex));
            secondMedianCutCube = new MedianCutCube(colors.GetRange(medianIndex, colors.Count - medianIndex));
        }

        /// <summary>
        /// Assigns a palette index to this cube, to be later found by a GetPaletteIndex method.
        /// </summary>
        /// <param name="newPaletteIndex">The palette index to be assigned to this cube.</param>
        public void SetPaletteIndex(Int32 newPaletteIndex)
        {
            PaletteIndex = newPaletteIndex;
        }

        /// <summary>
        /// Determines whether the color is in the space of this cube.
        /// </summary>
        /// <param name="color">The color to be checked, if it's contained in this cube.</param>
        /// <returns>if true a color is in the space of this cube, otherwise returns false.</returns>
        public Boolean IsColorIn(Color color)
        {
            Int32 red = ColorModelHelper.GetComponentA(ColorModel, color);
            Int32 green = ColorModelHelper.GetComponentB(ColorModel, color);
            Int32 blue = ColorModelHelper.GetComponentC(ColorModel, color);

            return (red >= redLowBound && red <= redHighBound) &&
                   (green >= greenLowBound && green <= greenHighBound) &&
                   (blue >= blueLowBound && blue <= blueHighBound);
        }

        #endregion
    }
}
