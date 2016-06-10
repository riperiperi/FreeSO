using System;

namespace SimplePaletteQuantizer.Ditherers.Ordered
{
    public class ClusteredDotDitherer : BaseOrderedDitherer
    {
        /// <summary>
        /// See <see cref="BaseColorDitherer.CreateCoeficientMatrix"/> for more details.
        /// </summary>
        protected override Byte[,] CreateCoeficientMatrix()
        {
            return new Byte[,] 
            {
        		{ 13,  5, 12, 16 },
			    {  6,  0,  4, 11 },
			    {  7,  2,  3, 10 },
			    { 14,  8,  9, 15 }
            };
        }

        /// <summary>
        /// See <see cref="BaseOrderedDitherer.MatrixWidth"/> for more details.
        /// </summary>
        protected override Byte MatrixWidth
        {
            get { return 4; }
        }

        /// <summary>
        /// See <see cref="BaseOrderedDitherer.MatrixHeight"/> for more details.
        /// </summary>
        protected override Byte MatrixHeight
        {
            get { return 4; }
        }
    }
}
