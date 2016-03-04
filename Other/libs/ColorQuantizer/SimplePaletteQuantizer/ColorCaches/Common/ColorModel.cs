namespace SimplePaletteQuantizer.ColorCaches.Common
{
    public enum ColorModel
    {
        /// <summary>
        /// The RGB color model is an additive color model in which red, green, and blue light is added together 
        /// in various ways to reproduce a broad array of colors. The name of the model comes from the initials 
        /// of the three additive primary colors, red, green, and blue.
        /// </summary>
        RedGreenBlue = 0,

        /// <summary>
        /// HSL is a common cylindrical-coordinate representations of points in an RGB color model, which rearrange 
        /// the geometry of RGB in an attempt to be more perceptually relevant than the cartesian representation.
        /// </summary>
        HueSaturationBrightness = 1,
        HueSaturationLuminance = 1,

        /// <summary>
        /// A Lab color space is a color-opponent space with dimension L for lightness and a and b for the 
        /// color-opponent dimensions, based on nonlinearly compressed CIE XYZ color space coordinates.
        /// </summary>
        LabColorSpace = 2,

        /// <summary>
        /// XYZ color space
        /// </summary>
        XYZ = 3,
    }
}
