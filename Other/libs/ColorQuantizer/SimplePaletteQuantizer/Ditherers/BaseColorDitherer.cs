using System;
using System.Collections.Generic;
using System.Drawing;
using SimplePaletteQuantizer.Helpers;
using SimplePaletteQuantizer.PathProviders;
using SimplePaletteQuantizer.Quantizers;

namespace SimplePaletteQuantizer.Ditherers
{
    public abstract class BaseColorDitherer : IColorDitherer
    {
        #region | Fields |

        private IPathProvider pathProvider;

        #endregion

        #region | Properties |

        /// <summary>
        /// Gets the color count.
        /// </summary>
        protected Int32 ColorCount { get; private set; }

        /// <summary>
        /// Gets the source buffer.
        /// </summary>
        protected ImageBuffer SourceBuffer { get; private set; }

        /// <summary>
        /// Gets the target buffer.
        /// </summary>
        protected ImageBuffer TargetBuffer { get; private set; }

        /// <summary>
        /// Gets the quantizer.
        /// </summary>
        protected IColorQuantizer Quantizer { get; private set; }

        /// <summary>
        /// Cache: Access to already created coeficient matrix.
        /// </summary>
        protected Byte[,] CachedMatrix { get; private set; }

        /// <summary>
        /// Cache: Access to already created coeficient matrix with division performed.
        /// </summary>
        protected Single[,] CachedSummedMatrix { get; private set; }

        #endregion

        #region | Methods |

        public void ChangePathProvider(IPathProvider pathProvider)
        {
            this.pathProvider = pathProvider;
        }

        #endregion

        #region | Helper methods |

        private Int32 GetMatrixFactor()
        {
            Int32 result = 0;

            for (Int32 y = 0; y < CachedMatrix.GetLength(0); y++)
            for (Int32 x = 0; x < CachedMatrix.GetLength(1); x++)
            {
                Int32 value = CachedMatrix[y, x];
                if (value > result) result = value;
            }

            return result;
        }

        private IPathProvider GetPathProvider()
        {
            // if there is no path provider, it attempts to create a default one; integrated in the quantizer
            IPathProvider result = pathProvider ?? (pathProvider = OnCreateDefaultPathProvider());

            // if the provider exists; or default one was created for these purposes.. use it
            if (result == null)
            {
                String message = string.Format("The path provider is not initialized! Please use SetPathProvider() method on quantizer.");
                throw new ArgumentNullException(message);
            }

            // provider was obtained somehow, use it
            return result;
        }

        protected Int32 GetClampedColorElementWithError(Int32 colorElement, Single factor, Int32 error)
        {
            Int32 result = Convert.ToInt32(colorElement + factor * error);
            return GetClampedColorElement(result);
        }

        protected Int32 GetClampedColorElement(Int32 colorElement)
        {
            Int32 result = colorElement;
            if (result < 0) result = 0;
            if (result > 255) result = 255;
            return result;
        }

        #endregion

        #region | Abstract/virtual methods |

        /// <summary>
        /// Called when a need to create default path provider arisen.
        /// </summary>
        /// <returns></returns>
        protected virtual IPathProvider OnCreateDefaultPathProvider()
        {
            return new StandardPathProvider();
        }

        /// <summary>
        /// Called when ditherer is about to be prepared.
        /// </summary>
        protected virtual void OnPrepare()
        {
            // creates coeficient matrix and determines the matrix factor/divisor/maximum
            CachedMatrix = CreateCoeficientMatrix();
            Single maximum = GetMatrixFactor();

            // prepares the cache arrays
            Int32 width = CachedMatrix.GetLength(1);
            Int32 height = CachedMatrix.GetLength(0);
            CachedSummedMatrix = new Single[height, width];

            // caches the matrix (and division by a sum)
            for (Int32 y = 0; y < height; y++)
            for (Int32 x = 0; x < width; x++)
            {
                CachedSummedMatrix[y, x] = CachedMatrix[y, x] / maximum;
            }
        }

        /// <summary>
        /// Creates the coeficient matrix.
        /// </summary>
        /// <returns></returns>
        protected abstract Byte[,] CreateCoeficientMatrix();

        /// <summary>
        /// Allows ditherer to process image per pixel, with ability to access the rest of the image.
        /// </summary>
        protected abstract Boolean OnProcessPixel(Pixel sourcePixel, Pixel targetPixel);

        /// <summary>
        /// Called when dithering is finished.
        /// </summary>
        protected virtual void OnFinish()
        {
            // do nothing here
        }

        #endregion

        #region << IPathProvider >>

        /// <summary>
        /// See <see cref="IPathProvider.GetPointPath"/> for more details.
        /// </summary>
        public IList<Point> GetPointPath(Int32 width, Int32 heigth)
        {
            return GetPathProvider().GetPointPath(width, heigth);
        }

        #endregion

        #region << IColorDitherer >>

        /// <summary>
        /// See <see cref="IColorDitherer.IsInplace"/> for more details.
        /// </summary>
        public abstract Boolean IsInplace { get; }

        /// <summary>
        /// See <see cref="IColorDitherer.Prepare"/> for more details.
        /// </summary>
        public void Prepare(IColorQuantizer quantizer, Int32 colorCount, ImageBuffer sourceBuffer, ImageBuffer targetBuffer)
        {
            SourceBuffer = sourceBuffer;
            TargetBuffer = targetBuffer;
            ColorCount = colorCount;
            Quantizer = quantizer;

            OnPrepare();
        }

        /// <summary>
        /// Retrieves the path in which to traverse 
        /// </summary>
        /// <returns></returns>
        public IList<Point> GetPointPath()
        {
            return null;
        }

        /// <summary>
        /// See <see cref="ProcessPixel"/> for more details.
        /// </summary>
        public Boolean ProcessPixel(Pixel sourcePixel, Pixel targetPixel)
        {
            return OnProcessPixel(sourcePixel, targetPixel);
        }

        /// <summary>
        /// See <see cref="IColorDitherer.Prepare"/> for more details.
        /// </summary>
        public void Finish()
        {
            OnFinish();
        }

        #endregion
    }
}
