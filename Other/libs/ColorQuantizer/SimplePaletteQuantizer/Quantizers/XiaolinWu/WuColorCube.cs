using System;

namespace SimplePaletteQuantizer.Quantizers.XiaolinWu
{
    internal class WuColorCube
    {
        /// <summary>
        /// Gets or sets the red minimum.
        /// </summary>
        /// <value>The red minimum.</value>
        public Int32 RedMinimum { get; set; }

        /// <summary>
        /// Gets or sets the red maximum.
        /// </summary>
        /// <value>The red maximum.</value>
        public Int32 RedMaximum { get; set; }

        /// <summary>
        /// Gets or sets the green minimum.
        /// </summary>
        /// <value>The green minimum.</value>
        public Int32 GreenMinimum { get; set; }

        /// <summary>
        /// Gets or sets the green maximum.
        /// </summary>
        /// <value>The green maximum.</value>
        public Int32 GreenMaximum { get; set; }

        /// <summary>
        /// Gets or sets the blue minimum.
        /// </summary>
        /// <value>The blue minimum.</value>
        public Int32 BlueMinimum { get; set; }

        /// <summary>
        /// Gets or sets the blue maximum.
        /// </summary>
        /// <value>The blue maximum.</value>
        public Int32 BlueMaximum { get; set; }

        /// <summary>
        /// Gets or sets the cube volume.
        /// </summary>
        /// <value>The volume.</value>
        public Int32 Volume { get; set; }
    }
}
