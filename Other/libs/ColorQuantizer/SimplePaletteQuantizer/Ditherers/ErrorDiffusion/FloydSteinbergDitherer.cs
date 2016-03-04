using System;

namespace SimplePaletteQuantizer.Ditherers.ErrorDiffusion
{
    public class FloydSteinbergDitherer : BaseErrorDistributionDitherer
    {
        /// <summary>
        /// See <see cref="BaseColorDitherer.CreateCoeficientMatrix"/> for more details.
        /// </summary>
        protected override Byte[,] CreateCoeficientMatrix()
        {
            return new Byte[,]
            {
                { 0, 0, 0 },
                { 0, 0, 7 },
                { 3, 5, 1 }
            };
        }

        /// <summary>
        /// See <see cref="BaseErrorDistributionDitherer.MatrixSideWidth"/> for more details.
        /// </summary>
        protected override Int32 MatrixSideWidth
        {
            get { return 1; }
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
