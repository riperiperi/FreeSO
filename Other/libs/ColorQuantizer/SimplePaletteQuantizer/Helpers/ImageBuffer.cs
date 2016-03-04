using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SimplePaletteQuantizer.ColorCaches.Common;
using SimplePaletteQuantizer.Ditherers;
using SimplePaletteQuantizer.Extensions;
using SimplePaletteQuantizer.PathProviders;
using SimplePaletteQuantizer.Quantizers;

namespace SimplePaletteQuantizer.Helpers
{
    public class ImageBuffer : IDisposable
    {
        #region | Fields |

        private Int32[] fastBitX;
        private Int32[] fastByteX;
        private Int32[] fastY;

        private readonly Bitmap bitmap;
        private readonly BitmapData bitmapData;
        private readonly ImageLockMode lockMode;

        private List<Color> cachedPalette;

        #endregion

        #region | Delegates |

        public delegate Boolean ProcessPixelFunction(Pixel pixel);
        public delegate Boolean ProcessPixelAdvancedFunction(Pixel pixel, ImageBuffer buffer);
        public delegate Boolean TransformPixelFunction(Pixel sourcePixel, Pixel targetPixel);
        public delegate Boolean TransformPixelAdvancedFunction(Pixel sourcePixel, Pixel targetPixel, ImageBuffer sourceBuffer, ImageBuffer targetBuffer);

        #endregion

        #region | Properties |

        public Int32 Width { get; private set; }
        public Int32 Height { get; private set; }

        public Int32 Size { get; private set; }
        public Int32 Stride { get; private set; }
        public Int32 BitDepth { get; private set; }
        public Int32 BytesPerPixel { get; private set; }

        public Boolean IsIndexed { get; private set; }
        public PixelFormat PixelFormat { get; private set; }

        #endregion

        #region | Calculated properties |

        /// <summary>
        /// Gets a value indicating whether this buffer can be read.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can read; otherwise, <c>false</c>.
        /// </value>
        public Boolean CanRead
        {
            get { return lockMode == ImageLockMode.ReadOnly || lockMode == ImageLockMode.ReadWrite; }
        }

        /// <summary>
        /// Gets a value indicating whether this buffer can written to.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can write; otherwise, <c>false</c>.
        /// </value>
        public Boolean CanWrite
        {
            get { return lockMode == ImageLockMode.WriteOnly || lockMode == ImageLockMode.ReadWrite; }
        }

        /// <summary>
        /// Gets or sets the palette.
        /// </summary>
        public List<Color> Palette
        {
            get { return UpdatePalette(); }
            set
            {
                bitmap.SetPalette(value);
                cachedPalette = value;
            }
        }

        #endregion

        #region | Constructors |

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBuffer"/> class.
        /// </summary>
        public ImageBuffer(Image bitmap, ImageLockMode lockMode) : this((Bitmap) bitmap, lockMode) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageBuffer"/> class.
        /// </summary>
        public ImageBuffer(Bitmap bitmap, ImageLockMode lockMode)
        {
            // locks the image data
            this.bitmap = bitmap;
            this.lockMode = lockMode;

            // gathers the informations
            Width = bitmap.Width;
            Height = bitmap.Height;
            PixelFormat = bitmap.PixelFormat;
            IsIndexed = PixelFormat.IsIndexed();
            BitDepth = PixelFormat.GetBitDepth();
            BytesPerPixel = Math.Max(1, BitDepth >> 3);

            // determines the bounds of an image, and locks the data in a specified mode
            Rectangle bounds = Rectangle.FromLTRB(0, 0, Width, Height);

            // locks the bitmap data
            lock (bitmap) bitmapData = bitmap.LockBits(bounds, lockMode, PixelFormat);

            // creates internal buffer
            Stride = bitmapData.Stride < 0 ? -bitmapData.Stride : bitmapData.Stride;
            Size = Stride*Height;

            // precalculates the offsets
            Precalculate();
        }

        #endregion

        #region | Maintenance methods |

        private void Precalculate()
        {
            fastBitX = new Int32[Width];
            fastByteX = new Int32[Width];
            fastY = new Int32[Height];

            // precalculates the x-coordinates
            for (Int32 x = 0; x < Width; x++)
            {
                fastBitX[x] = x*BitDepth;
                fastByteX[x] = fastBitX[x] >> 3;
                fastBitX[x] = fastBitX[x] % 8;
            }

            // precalculates the y-coordinates
            for (Int32 y = 0; y < Height; y++)
            {
                fastY[y] = y * bitmapData.Stride;
            }
        }

        public Int32 GetBitOffset(Int32 x)
        {
            return fastBitX[x];
        }

        public Byte[] Copy()
        {
            // transfers whole image to a working memory
            Byte[] result = new Byte[Size]; 
            Marshal.Copy(bitmapData.Scan0, result, 0, Size);

            // returns the backup
            return result;
        }

        public void Paste(Byte[] buffer)
        {
            // commits the data to a bitmap
            Marshal.Copy(buffer, 0, bitmapData.Scan0, Size);
        }

        #endregion

        #region | Pixel read methods |

        public void ReadPixel(Pixel pixel, Byte[] buffer = null)
        {
            // determines pixel offset at [x, y]
            Int32 offset = fastByteX[pixel.X] + fastY[pixel.Y];

            // reads the pixel from a bitmap
            if (buffer == null)
            {
                pixel.ReadRawData(bitmapData.Scan0 + offset);
            }
            else // reads the pixel from a buffer
            {
                pixel.ReadData(buffer, offset);
            }
        }

        public Int32 GetIndexFromPixel(Pixel pixel)
        {
            Int32 result;

            // determines whether the format is indexed
            if (IsIndexed)
            {
                result = pixel.Index;
            }
            else // not possible to get index from a non-indexed format
            {
                String message = string.Format("Cannot retrieve index for a non-indexed format. Please use Color (or Value) property instead.");
                throw new NotSupportedException(message);
            }

            return result;
        }

        public Color GetColorFromPixel(Pixel pixel)
        {
            Color result;

            // determines whether the format is indexed
            if (pixel.IsIndexed)
            {
                Int32 index = pixel.Index;
                result = pixel.Parent.GetPaletteColor(index);
            }
            else // gets color from a non-indexed format
            {
                result = pixel.Color;
            }

            // returns the found color
            return result;    
        }

        public Int32 ReadIndexUsingPixel(Pixel pixel, Byte[] buffer = null)
        {
            // reads the pixel from bitmap/buffer
            ReadPixel(pixel, buffer);

            // returns the found color
            return GetIndexFromPixel(pixel);
        }

        public Color ReadColorUsingPixel(Pixel pixel, Byte[] buffer = null)
        {
            // reads the pixel from bitmap/buffer
            ReadPixel(pixel, buffer);

            // returns the found color
            return GetColorFromPixel(pixel);
        }

        public Int32 ReadIndexUsingPixelFrom(Pixel pixel, Int32 x, Int32 y, Byte[] buffer = null)
        {
            // redirects pixel -> [x, y]
            pixel.Update(x, y);

            // reads index from a bitmap/buffer using pixel, and stores it in the pixel
            return ReadIndexUsingPixel(pixel, buffer);
        }

        public Color ReadColorUsingPixelFrom(Pixel pixel, Int32 x, Int32 y, Byte[] buffer = null)
        {
            // redirects pixel -> [x, y]
            pixel.Update(x, y);

            // reads color from a bitmap/buffer using pixel, and stores it in the pixel
            return ReadColorUsingPixel(pixel, buffer);
        }

        #endregion

        #region | Pixel write methods |

        private void WritePixel(Pixel pixel, Byte[] buffer = null)
        {
            // determines pixel offset at [x, y]
            Int32 offset = fastByteX[pixel.X] + fastY[pixel.Y];

            // writes the pixel to a bitmap
            if (buffer == null)
            {
                pixel.WriteRawData(bitmapData.Scan0 + offset);
            }
            else // writes the pixel to a buffer
            {
                pixel.WriteData(buffer, offset);
            }
        }

        public void SetIndexToPixel(Pixel pixel, Int32 index, Byte[] buffer = null)
        {
            // determines whether the format is indexed
            if (IsIndexed)
            {
                pixel.Index = (Byte) index;
            }
            else // cannot write color to an indexed format
            {
                String message = string.Format("Cannot set index for a non-indexed format. Please use Color (or Value) property instead.");
                throw new NotSupportedException(message);
            }
        }

        public void SetColorToPixel(Pixel pixel, Color color, IColorQuantizer quantizer)
        {
            // determines whether the format is indexed
            if (pixel.IsIndexed)
            {
                // last chance if quantizer is provided, use it
                if (quantizer != null)
                {
                    Byte index = (Byte)quantizer.GetPaletteIndex(color, pixel.X, pixel.Y);
                    pixel.Index = index;
                }
                else // cannot write color to an index format
                {
                    String message = string.Format("Cannot retrieve color for an indexed format. Use GetPixelIndex() instead.");
                    throw new NotSupportedException(message);
                }
            }
            else // sets color to a non-indexed format
            {
                pixel.Color = color;
            }
        }

        public void WriteIndexUsingPixel(Pixel pixel, Int32 index, Byte[] buffer = null)
        {
            // sets index to pixel (pixel's index is updated)
            SetIndexToPixel(pixel, index, buffer);

            // writes pixel to a bitmap/buffer
            WritePixel(pixel, buffer);
        }

        public void WriteColorUsingPixel(Pixel pixel, Color color, IColorQuantizer quantizer, Byte[] buffer = null)
        {
            // sets color to pixel (pixel is updated with color)
            SetColorToPixel(pixel, color, quantizer);

            // writes pixel to a bitmap/buffer
            WritePixel(pixel, buffer);
        }

        public void WriteIndexUsingPixelAt(Pixel pixel, Int32 x, Int32 y, Int32 index, Byte[] buffer = null)
        {
            // redirects pixel -> [x, y]
            pixel.Update(x, y);

            // writes color to bitmap/buffer using pixel
            WriteIndexUsingPixel(pixel, index, buffer);
        }

        public void WriteColorUsingPixelAt(Pixel pixel, Int32 x, Int32 y, Color color, IColorQuantizer quantizer, Byte[] buffer = null)
        {
            // redirects pixel -> [x, y]
            pixel.Update(x, y);

            // writes color to bitmap/buffer using pixel
            WriteColorUsingPixel(pixel, color, quantizer, buffer);
        }

        #endregion

        #region | Generic methods |

        private void ProcessInParallel(ICollection<Point> path, Action<LineTask> process, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(process, "process");

            // updates the palette
            UpdatePalette();

            // prepares parallel processing
            Double pointsPerTask = (1.0*path.Count)/parallelTaskCount;
            LineTask[] lineTasks = new LineTask[parallelTaskCount];
            Double pointOffset = 0.0;

            // creates task for each batch of rows
            for (Int32 index = 0; index < parallelTaskCount; index++)
            {
                lineTasks[index] = new LineTask((Int32) pointOffset, (Int32) (pointOffset + pointsPerTask));
                pointOffset += pointsPerTask;
            }

            // process the image in a parallel manner
            Parallel.ForEach(lineTasks, process);
        }

        #endregion

        #region | Processing methods |

        private void ProcessPerPixelBase(IList<Point> path, Delegate processingAction, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(path, "path");
            Guard.CheckNull(processingAction, "processPixelFunction");
            
            // determines mode
            Boolean isAdvanced = processingAction is ProcessPixelAdvancedFunction;
            
            // prepares the per pixel task
            Action<LineTask> processPerPixel = lineTask =>
            {
                // initializes variables per task
                Pixel pixel = new Pixel(this);

                for (Int32 pathOffset = lineTask.StartOffset; pathOffset < lineTask.EndOffset; pathOffset++)
                {
                    Point point = path[pathOffset];
                    Boolean allowWrite;

                    // enumerates the pixel, and returns the control to the outside
                    pixel.Update(point.X, point.Y);

                    // when read is allowed, retrieves current value (in bytes)
                    if (CanRead) ReadPixel(pixel);

                    // process the pixel by custom user operation
                    if (isAdvanced)
                    {
                        ProcessPixelAdvancedFunction processAdvancedFunction = (ProcessPixelAdvancedFunction) processingAction;
                        allowWrite = processAdvancedFunction(pixel, this);
                    }
                    else // use simplified version with pixel parameter only
                    {
                        ProcessPixelFunction processFunction = (ProcessPixelFunction) processingAction;
                        allowWrite = processFunction(pixel);
                    }

                    // when write is allowed, copies the value back to the row buffer
                    if (CanWrite && allowWrite) WritePixel(pixel);
                }
            };

            // processes image per pixel
            ProcessInParallel(path, processPerPixel, parallelTaskCount);
        }

        public void ProcessPerPixel(IList<Point> path, ProcessPixelFunction processPixelFunction, Int32 parallelTaskCount = 4)
        {
            ProcessPerPixelBase(path, processPixelFunction, parallelTaskCount);
        }

        public void ProcessPerPixelAdvanced(IList<Point> path, ProcessPixelAdvancedFunction processPixelAdvancedFunction, Int32 parallelTaskCount = 4)
        {
            ProcessPerPixelBase(path, processPixelAdvancedFunction, parallelTaskCount);
        }

        #endregion

        #region | Transformation functions |

        private void TransformPerPixelBase(ImageBuffer target, IList<Point> path, Delegate transformAction, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(path, "path");
            Guard.CheckNull(target, "target");
            Guard.CheckNull(transformAction, "transformAction");

            // updates the palette
            UpdatePalette();
            target.UpdatePalette();

            // checks the dimensions
            if (Width != target.Width || Height != target.Height)
            {
                const String message = "Both images have to have the same dimensions.";
                throw new ArgumentOutOfRangeException(message);
            }

            // determines mode
            Boolean isAdvanced = transformAction is TransformPixelAdvancedFunction;

            // process the image in a parallel manner
            Action<LineTask> transformPerPixel = lineTask =>
            {
                // creates individual pixel structures per task
                Pixel sourcePixel = new Pixel(this);
                Pixel targetPixel = new Pixel(target);

                // enumerates the pixels row by row
                for (Int32 pathOffset = lineTask.StartOffset; pathOffset < lineTask.EndOffset; pathOffset++)
                {
                    Point point = path[pathOffset];
                    Boolean allowWrite;

                    // enumerates the pixel, and returns the control to the outside
                    sourcePixel.Update(point.X, point.Y);
                    targetPixel.Update(point.X, point.Y);

                    // when read is allowed, retrieves current value (in bytes)
                    if (CanRead) ReadPixel(sourcePixel);
                    if (target.CanRead) target.ReadPixel(targetPixel);

                    // process the pixel by custom user operation
                    if (isAdvanced)
                    {
                        TransformPixelAdvancedFunction transformAdvancedFunction = (TransformPixelAdvancedFunction) transformAction;
                        allowWrite = transformAdvancedFunction(sourcePixel, targetPixel, this, target);
                    }
                    else // use simplified version with pixel parameters only
                    {
                        TransformPixelFunction transformFunction = (TransformPixelFunction) transformAction;
                        allowWrite = transformFunction(sourcePixel, targetPixel);
                    }

                    // when write is allowed, copies the value back to the row buffer
                    if (target.CanWrite && allowWrite) target.WritePixel(targetPixel);
                }
            };

            // transforms image per pixel
            ProcessInParallel(path, transformPerPixel, parallelTaskCount);
        }

        public void TransformPerPixel(ImageBuffer target, IList<Point> path, TransformPixelFunction transformPixelFunction, Int32 parallelTaskCount = 4)
        {
            TransformPerPixelBase(target, path, transformPixelFunction, parallelTaskCount);
        }

        public void TransformPerPixelAdvanced(ImageBuffer target, IList<Point> path, TransformPixelAdvancedFunction transformPixelAdvancedFunction, Int32 parallelTaskCount = 4)
        {
            TransformPerPixelBase(target, path, transformPixelAdvancedFunction, parallelTaskCount);
        }
        
        #endregion

        #region | Scan colors methods |

        public void ScanColors(IColorQuantizer quantizer, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(quantizer, "quantizer");

            // determines which method of color retrieval to use
            IList<Point> path = quantizer.GetPointPath(Width, Height);

            // use different scanning method depending whether the image format is indexed
            ProcessPixelFunction scanColors = pixel =>
            {
                quantizer.AddColor(GetColorFromPixel(pixel), pixel.X, pixel.Y);
                return false;
            };

            // performs the image scan, using a chosen method
            ProcessPerPixel(path, scanColors, parallelTaskCount);
        }

        public static void ScanImageColors(Image sourceImage, IColorQuantizer quantizer, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(sourceImage, "sourceImage");

            // wraps source image to a buffer
            using (ImageBuffer source = new ImageBuffer(sourceImage, ImageLockMode.ReadOnly))
            {
                source.ScanColors(quantizer, parallelTaskCount);
            }
        }

        #endregion

        #region | Synthetize palette methods |

        public List<Color> SynthetizePalette(IColorQuantizer quantizer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(quantizer, "quantizer");

            // Step 1 - prepares quantizer for another round
            quantizer.Prepare(this);

            // Step 2 - scans the source image for the colors
            ScanColors(quantizer, parallelTaskCount);

            // Step 3 - synthetises the palette, and returns the result
            return quantizer.GetPalette(colorCount);
        }

        public static List<Color> SynthetizeImagePalette(Image sourceImage, IColorQuantizer quantizer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(sourceImage, "sourceImage");

            // wraps source image to a buffer
            using (ImageBuffer source = new ImageBuffer(sourceImage, ImageLockMode.ReadOnly))
            {
                return source.SynthetizePalette(quantizer, colorCount, parallelTaskCount);
            }
        }

        #endregion

        #region | Quantize methods |

        public void Quantize(ImageBuffer target, IColorQuantizer quantizer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // performs the pure quantization wihout dithering
            Quantize(target, quantizer, null, colorCount, parallelTaskCount);
        }

        public void Quantize(ImageBuffer target, IColorQuantizer quantizer, IColorDitherer ditherer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(target, "target");
            Guard.CheckNull(quantizer, "quantizer");

            // initializes quantization parameters
            Boolean isTargetIndexed = target.PixelFormat.IsIndexed();

            // step 1 - prepares the palettes
            List<Color> targetPalette = isTargetIndexed ? SynthetizePalette(quantizer, colorCount, parallelTaskCount) : null;

            // step 2 - updates the bitmap palette
            target.bitmap.SetPalette(targetPalette);
            target.UpdatePalette(true);

            // step 3 - prepares ditherer (optional)
            if (ditherer != null) ditherer.Prepare(quantizer, colorCount, this, target);

            // step 4 - prepares the quantization function
            TransformPixelFunction quantize = (sourcePixel, targetPixel) =>
            {
                // reads the pixel color
                Color color = GetColorFromPixel(sourcePixel);

                // converts alpha to solid color
                color = QuantizationHelper.ConvertAlpha(color);

                // quantizes the pixel
                SetColorToPixel(targetPixel, color, quantizer);

                // marks pixel as processed by default
                Boolean result = true;

                // preforms inplace dithering (optional)
                if (ditherer != null && ditherer.IsInplace)
                {
                    result = ditherer.ProcessPixel(sourcePixel, targetPixel);
                }

                // returns the result
                return result;
            };

            // step 5 - generates the target image
            IList<Point> path = quantizer.GetPointPath(Width, Height);
            TransformPerPixel(target, path, quantize, parallelTaskCount);

            // step 6 - preforms non-inplace dithering (optional)
            if (ditherer != null && !ditherer.IsInplace)
            {
                Dither(target, ditherer, quantizer, colorCount, 1);
            }

            // step 7 - finishes the dithering (optional)
            if (ditherer != null) ditherer.Finish();

            // step 8 - clean-up
            quantizer.Finish();
        }

        public static Image QuantizeImage(ImageBuffer source, IColorQuantizer quantizer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // performs the pure quantization wihout dithering
            return QuantizeImage(source, quantizer, null, colorCount, parallelTaskCount);
        }

        public static Image QuantizeImage(ImageBuffer source, IColorQuantizer quantizer, IColorDitherer ditherer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(source, "source");

            // creates a target bitmap in an appropriate format
            PixelFormat targetPixelFormat = Extend.GetFormatByColorCount(colorCount);
            Image result = new Bitmap(source.Width, source.Height, targetPixelFormat);

            // lock mode
            ImageLockMode lockMode = ditherer == null ? ImageLockMode.WriteOnly : ImageLockMode.ReadWrite;

            // wraps target image to a buffer
            using (ImageBuffer target = new ImageBuffer(result, lockMode))
            {
                source.Quantize(target, quantizer, ditherer, colorCount, parallelTaskCount);
                return result;
            }
        }

        public static Image QuantizeImage(Image sourceImage, IColorQuantizer quantizer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // performs the pure quantization wihout dithering
            return QuantizeImage(sourceImage, quantizer, null, colorCount, parallelTaskCount);
        }

        public static Image QuantizeImage(Image sourceImage, IColorQuantizer quantizer, IColorDitherer ditherer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(sourceImage, "sourceImage");

            // lock mode
            ImageLockMode lockMode = ditherer == null ? ImageLockMode.ReadOnly : ImageLockMode.ReadWrite;

            // wraps source image to a buffer
            using (ImageBuffer source = new ImageBuffer(sourceImage, lockMode))
            {
                return QuantizeImage(source, quantizer, ditherer, colorCount, parallelTaskCount);
            }
        }

        #endregion

        #region | Calculate mean error methods |

        public Double CalculateMeanError(ImageBuffer target, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(target, "target");

            // initializes the error
            Int64 totalError = 0;

            // prepares the function
            TransformPixelFunction calculateMeanError = (sourcePixel, targetPixel) =>
            {
                Color sourceColor = GetColorFromPixel(sourcePixel);
                Color targetColor = GetColorFromPixel(targetPixel);
                totalError += ColorModelHelper.GetColorEuclideanDistance(ColorModel.RedGreenBlue, sourceColor, targetColor);
                return false;
            };

            // performs the image scan, using a chosen method
            IList<Point> standardPath = new StandardPathProvider().GetPointPath(Width, Height);
            TransformPerPixel(target, standardPath, calculateMeanError, parallelTaskCount);

            // returns the calculates RMSD
            return Math.Sqrt(totalError/(3.0*Width*Height));
        }

        public static Double CalculateImageMeanError(ImageBuffer source, ImageBuffer target, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(source, "source");

            // use other override to calculate error
            return source.CalculateMeanError(target, parallelTaskCount);
        }

        public static Double CalculateImageMeanError(ImageBuffer source, Image targetImage, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(source, "source");
            Guard.CheckNull(targetImage, "targetImage");

            // wraps source image to a buffer
            using (ImageBuffer target = new ImageBuffer(targetImage, ImageLockMode.ReadOnly))
            {
                // use other override to calculate error
                return source.CalculateMeanError(target, parallelTaskCount);
            }
        }

        public static Double CalculateImageMeanError(Image sourceImage, ImageBuffer target, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(sourceImage, "sourceImage");

            // wraps source image to a buffer
            using (ImageBuffer source = new ImageBuffer(sourceImage, ImageLockMode.ReadOnly))
            {
                // use other override to calculate error
                return source.CalculateMeanError(target, parallelTaskCount);
            }
        }

        public static Double CalculateImageMeanError(Image sourceImage, Image targetImage, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(sourceImage, "sourceImage");
            Guard.CheckNull(targetImage, "targetImage");

            // wraps source image to a buffer
            using (ImageBuffer source = new ImageBuffer(sourceImage, ImageLockMode.ReadOnly))
            using (ImageBuffer target = new ImageBuffer(targetImage, ImageLockMode.ReadOnly))
            {
                // use other override to calculate error
                return source.CalculateMeanError(target, parallelTaskCount);
            }
        }

        #endregion

        #region | Calculate normalized mean error methods |

        public Double CalculateNormalizedMeanError(ImageBuffer target, Int32 parallelTaskCount = 4)
        {
            return CalculateMeanError(target, parallelTaskCount) / 255.0;
        }

        public static Double CalculateImageNormalizedMeanError(ImageBuffer source, Image targetImage, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(source, "source");
            Guard.CheckNull(targetImage, "targetImage");

            // wraps source image to a buffer
            using (ImageBuffer target = new ImageBuffer(targetImage, ImageLockMode.ReadOnly))
            {
                // use other override to calculate error
                return source.CalculateNormalizedMeanError(target, parallelTaskCount);
            }
        }

        public static Double CalculateImageNormalizedMeanError(Image sourceImage, ImageBuffer target, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(sourceImage, "sourceImage");

            // wraps source image to a buffer
            using (ImageBuffer source = new ImageBuffer(sourceImage, ImageLockMode.ReadOnly))
            {
                // use other override to calculate error
                return source.CalculateNormalizedMeanError(target, parallelTaskCount);
            }
        }

        public static Double CalculateImageNormalizedMeanError(ImageBuffer source, ImageBuffer target, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(source, "source");

            // use other override to calculate error
            return source.CalculateNormalizedMeanError(target, parallelTaskCount);
        }

        public static Double CalculateImageNormalizedMeanError(Image sourceImage, Image targetImage, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(sourceImage, "sourceImage");
            Guard.CheckNull(targetImage, "targetImage");

            // wraps source image to a buffer
            using (ImageBuffer source = new ImageBuffer(sourceImage, ImageLockMode.ReadOnly))
            using (ImageBuffer target = new ImageBuffer(targetImage, ImageLockMode.ReadOnly))
            {
                // use other override to calculate error
                return source.CalculateNormalizedMeanError(target, parallelTaskCount);
            }
        }

        #endregion

        #region | Change pixel format methods |

        public void ChangeFormat(ImageBuffer target, IColorQuantizer quantizer, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(target, "target");
            Guard.CheckNull(quantizer, "quantizer");

            // gathers some information about the target format
            Boolean hasSourceAlpha = PixelFormat.HasAlpha();
            Boolean hasTargetAlpha = target.PixelFormat.HasAlpha();
            Boolean isTargetIndexed = target.PixelFormat.IsIndexed();
            Boolean isSourceDeepColor = PixelFormat.IsDeepColor();
            Boolean isTargetDeepColor = target.PixelFormat.IsDeepColor();

            // step 1 to 3 - prepares the palettes
            if (isTargetIndexed) SynthetizePalette(quantizer, target.PixelFormat.GetColorCount(), parallelTaskCount);

            // prepares the quantization function
            TransformPixelFunction changeFormat = (sourcePixel, targetPixel) =>
            {
                // if both source and target formats are deep color formats, copies a value directly
                if (isSourceDeepColor && isTargetDeepColor)
                {
                    //UInt64 value = sourcePixel.Value;
                    //targetPixel.SetValue(value);
                }
                else
                {
                    // retrieves a source image color
                    Color color = GetColorFromPixel(sourcePixel);

                    // if alpha is not present in the source image, but is present in the target, make one up
                    if (!hasSourceAlpha && hasTargetAlpha)
                    {
                        Int32 argb = 255 << 24 | color.R << 16 | color.G << 8 | color.B;
                        color = Color.FromArgb(argb);
                    }

                    // sets the color to a target pixel
                    SetColorToPixel(targetPixel, color, quantizer);
                }

                // allows to write (obviously) the transformed pixel
                return true;
            };

            // step 5 - generates the target image
            IList<Point> standardPath = new StandardPathProvider().GetPointPath(Width, Height);
            TransformPerPixel(target, standardPath, changeFormat, parallelTaskCount);
        }

        public static void ChangeFormat(ImageBuffer source, PixelFormat targetFormat, IColorQuantizer quantizer, out Image targetImage, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(source, "source");

            // creates a target bitmap in an appropriate format
            targetImage = new Bitmap(source.Width, source.Height, targetFormat);

            // wraps target image to a buffer
            using (ImageBuffer target = new ImageBuffer(targetImage, ImageLockMode.WriteOnly))
            {
                source.ChangeFormat(target, quantizer, parallelTaskCount);
            }
        }

        public static void ChangeFormat(Image sourceImage, PixelFormat targetFormat, IColorQuantizer quantizer, out Image targetImage, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(sourceImage, "sourceImage");

            // wraps source image to a buffer
            using (ImageBuffer source = new ImageBuffer(sourceImage, ImageLockMode.ReadOnly))
            {
                ChangeFormat(source, targetFormat, quantizer, out targetImage, parallelTaskCount);
            }
        }

        #endregion

        #region | Dithering methods |

        public void Dither(ImageBuffer target, IColorDitherer ditherer, IColorQuantizer quantizer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(target, "target");
            Guard.CheckNull(ditherer, "ditherer");
            Guard.CheckNull(quantizer, "quantizer");

            // prepares ditherer for another round
            ditherer.Prepare(quantizer, colorCount, this, target);
            
            // processes the image via the ditherer
            IList<Point> path = ditherer.GetPointPath(Width, Height);
            TransformPerPixel(target, path, ditherer.ProcessPixel, parallelTaskCount);
        }

        public static void DitherImage(ImageBuffer source, ImageBuffer target, IColorDitherer ditherer, IColorQuantizer quantizer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(source, "source");

            // use other override to calculate error
            source.Dither(target, ditherer, quantizer, colorCount, parallelTaskCount);
        }

        public static void DitherImage(ImageBuffer source, Image targetImage, IColorDitherer ditherer, IColorQuantizer quantizer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(source, "source");
            Guard.CheckNull(targetImage, "targetImage");

            // wraps source image to a buffer
            using (ImageBuffer target = new ImageBuffer(targetImage, ImageLockMode.ReadOnly))
            {
                // use other override to calculate error
                source.Dither(target, ditherer, quantizer, colorCount, parallelTaskCount);
            }
        }

        public static void DitherImage(Image sourceImage, ImageBuffer target, IColorDitherer ditherer, IColorQuantizer quantizer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(sourceImage, "sourceImage");

            // wraps source image to a buffer
            using (ImageBuffer source = new ImageBuffer(sourceImage, ImageLockMode.ReadOnly))
            {
                // use other override to calculate error
                source.Dither(target, ditherer, quantizer, colorCount, parallelTaskCount);
            }
        }

        public static void DitherImage(Image sourceImage, Image targetImage, IColorDitherer ditherer, IColorQuantizer quantizer, Int32 colorCount, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(sourceImage, "sourceImage");
            Guard.CheckNull(targetImage, "targetImage");

            // wraps source image to a buffer
            using (ImageBuffer source = new ImageBuffer(sourceImage, ImageLockMode.ReadOnly))
            using (ImageBuffer target = new ImageBuffer(targetImage, ImageLockMode.ReadOnly))
            {
                // use other override to calculate error
                source.Dither(target, ditherer, quantizer, colorCount, parallelTaskCount);
            }
        }

        #endregion

        #region | Gamma correction |

        public void CorrectGamma(Single gamma, IColorQuantizer quantizer, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(quantizer, "quantizer");

            // determines which method of color retrieval to use
            IList<Point> path = quantizer.GetPointPath(Width, Height);

            // calculates gamma ramp
            Int32[] gammaRamp = new Int32[256];

            for (Int32 index = 0; index < 256; ++index)
            {
                gammaRamp[index] = Clamp((Int32) ((255.0f*Math.Pow(index/255.0f, 1.0f/gamma)) + 0.5f));
            }

            // use different scanning method depending whether the image format is indexed
            ProcessPixelFunction correctGamma = pixel =>
            {
                Color oldColor = GetColorFromPixel(pixel);
                Int32 red = gammaRamp[oldColor.R];
                Int32 green = gammaRamp[oldColor.G];
                Int32 blue = gammaRamp[oldColor.B];
                Color newColor = Color.FromArgb(red, green, blue);
                SetColorToPixel(pixel, newColor, quantizer);
                return true;
            };

            // performs the image scan, using a chosen method
            ProcessPerPixel(path, correctGamma, parallelTaskCount);
        }

        public static void CorrectImageGamma(Image sourceImage, Single gamma, IColorQuantizer quantizer, Int32 parallelTaskCount = 4)
        {
            // checks parameters
            Guard.CheckNull(sourceImage, "sourceImage");

            // wraps source image to a buffer
            using (ImageBuffer source = new ImageBuffer(sourceImage, ImageLockMode.ReadOnly))
            {
                source.CorrectGamma(gamma, quantizer, parallelTaskCount);
            }
        }

        #endregion

        #region | Palette methods |

        public static Int32 Clamp(Int32 value, Int32 minimum = 0, Int32 maximum = 255)
        {
            if (value < minimum) value = minimum;
            if (value > maximum) value = maximum;
            return value;
        }

        private List<Color> UpdatePalette(Boolean forceUpdate = false)
        {
            if (IsIndexed && (cachedPalette == null || forceUpdate))
            {
                cachedPalette = bitmap.GetPalette();
            }

            return cachedPalette;
        }

        public Color GetPaletteColor(Int32 paletteIndex)
        {
            return cachedPalette[paletteIndex];
        }

        #endregion

        #region << IDisposable >>

        public void Dispose()
        {
            // releases the image lock
            lock (bitmap) bitmap.UnlockBits(bitmapData);
        }

        #endregion

        #region | Sub-classes |

        private class LineTask
        {
            /// <summary>
            /// Gets or sets the start offset.
            /// </summary>
            public Int32 StartOffset { get; private set; }

            /// <summary>
            /// Gets or sets the end offset.
            /// </summary>
            public Int32 EndOffset { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="SimplePaletteQuantizer.Helpers.ImageBuffer.LineTask"/> class.
            /// </summary>
            public LineTask(Int32 startOffset, Int32 endOffset)
            {
                StartOffset = startOffset;
                EndOffset = endOffset;
            }
        }

        #endregion
    }
}