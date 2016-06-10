using System;

namespace SimplePaletteQuantizer.Ditherers.ErrorDiffusion
{
    public class ShiauDitherer : BaseErrorDistributionDitherer
    {
        /// <summary>
        /// See <see cref="BaseColorDitherer.CreateCoeficientMatrix"/> for more details.
        /// </summary>
        protected override Byte[,] CreateCoeficientMatrix()
        {
            return new Byte[,]
            {
                { 0, 0, 0, 0, 0 },
                { 0, 0, 0, 4, 0 },
                { 1, 1, 2, 0, 0 }
            };
        }

        /// <summary>
        /// See <see cref="BaseErrorDistributionDitherer.MatrixSideWidth"/> for more details.
        /// </summary>
        protected override Int32 MatrixSideWidth
        {
            get { return 2; }
        }

        /// <summary>
        /// See <see cref="BaseErrorDistributionDitherer.MatrixSideHeight"/> for more details.
        /// </summary>
        protected override Int32 MatrixSideHeight
        {
            get { return 1; }
        }
    }
}
