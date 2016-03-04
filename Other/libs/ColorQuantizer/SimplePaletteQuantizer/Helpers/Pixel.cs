using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using SimplePaletteQuantizer.Helpers.Pixels;
using SimplePaletteQuantizer.Helpers.Pixels.Indexed;
using SimplePaletteQuantizer.Helpers.Pixels.NonIndexed;

namespace SimplePaletteQuantizer.Helpers
{
    /// <summary>
    /// This is a pixel format independent pixel.
    /// </summary>
    public class Pixel : IDisposable
    {
        #region | Constants |

        internal const Byte Zero = 0;
        internal const Byte One = 1;
        internal const Byte Two = 2;
        internal const Byte Four = 4;
        internal const Byte Eight = 8;

        internal const Byte NibbleMask = 0xF;
        internal const Byte ByteMask = 0xFF;

        internal const Int32 AlphaShift = 24;
        internal const Int32 RedShift = 16;
        internal const Int32 GreenShift = 8;
        internal const Int32 BlueShift = 0;

        internal const Int32 AlphaMask = ByteMask << AlphaShift;
        internal const Int32 RedGreenBlueMask = 0xFFFFFF;

        #endregion

        #region | Fields |

        private Type pixelType;
        private Int32 bitOffset;
        private Object pixelData;
        private IntPtr pixelDataPointer;

        #endregion

        #region | Properties |

        /// <summary>
        /// Gets the X.
        /// </summary>
        public Int32 X { get; private set; }

        /// <summary>
        /// Gets the Y.
        /// </summary>
        public Int32 Y { get; private set; }

        /// <summary>
        /// Gets the parent buffer.
        /// </summary>
        public ImageBuffer Parent { get; private set; }

        #endregion

        #region | Calculated properties |

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        public Byte Index
        {
            get { return ((IIndexedPixel) pixelData).GetIndex(bitOffset); }
            set { ((IIndexedPixel) pixelData).SetIndex(bitOffset, value); }
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        public Color Color
        {
            get { return ((INonIndexedPixel) pixelData).GetColor(); }
            set { ((INonIndexedPixel) pixelData).SetColor(value); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is indexed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is indexed; otherwise, <c>false</c>.
        /// </value>
        public Boolean IsIndexed
        {
            get { return Parent.IsIndexed; }    
        }

        #endregion

        #region | Constructors |

        /// <summary>
        /// Initializes a new instance of the <see cref="Pixel"/> struct.
        /// </summary>
        public Pixel(ImageBuffer parent)
        {
            Parent = parent;

            Initialize();
        }

        private void Initialize()
        {
            // creates pixel data
            pixelType = IsIndexed ? GetIndexedType(Parent.PixelFormat) : GetNonIndexedType(Parent.PixelFormat);
            NewExpression newType = Expression.New(pixelType);
            UnaryExpression convertNewType = Expression.Convert(newType, typeof (Object));
            Expression<Func<Object>> indexedExpression = Expression.Lambda<Func<Object>>(convertNewType);
            pixelData = indexedExpression.Compile().Invoke();
            pixelDataPointer = MarshalToPointer(pixelData);
        }

        #endregion

        #region | Methods |

        /// <summary>
        /// Gets the type of the indexed pixel format.
        /// </summary>
        internal Type GetIndexedType(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format1bppIndexed: return typeof(PixelData1Indexed);
                case PixelFormat.Format4bppIndexed: return typeof(PixelData4Indexed);
                case PixelFormat.Format8bppIndexed: return typeof(PixelData8Indexed);

                default:
                    String message = String.Format("This pixel format '{0}' is either non-indexed, or not supported.", pixelFormat);
                    throw new NotSupportedException(message);
            }
        }

        /// <summary>
        /// Gets the type of the non-indexed pixel format.
        /// </summary>
        internal Type GetNonIndexedType(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format16bppArgb1555: return typeof(PixelDataArgb1555);
                case PixelFormat.Format16bppGrayScale: return typeof(PixelDataGray16);
                case PixelFormat.Format16bppRgb555: return typeof(PixelDataRgb555);
                case PixelFormat.Format16bppRgb565: return typeof(PixelDataRgb565);
                case PixelFormat.Format24bppRgb: return typeof(PixelDataRgb888);
                case PixelFormat.Format32bppRgb: return typeof(PixelDataRgb8888);
                case PixelFormat.Format32bppArgb: return typeof(PixelDataArgb8888);
                case PixelFormat.Format48bppRgb: return typeof(PixelDataRgb48);
                case PixelFormat.Format64bppArgb: return typeof(PixelDataArgb64);

                default:
                    String message = String.Format("This pixel format '{0}' is either indexed, or not supported.", pixelFormat);
                    throw new NotSupportedException(message);
            }
        }

        private static IntPtr MarshalToPointer(Object data)
        {
            Int32 size = Marshal.SizeOf(data);
            IntPtr pointer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, pointer, false);
            return pointer;
        }

        #endregion

        #region | Update methods |

        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public void Update(Int32 x, Int32 y)
        {
            X = x; 
            Y = y; 
            bitOffset = Parent.GetBitOffset(x);
        }

        /// <summary>
        /// Reads the raw data.
        /// </summary>
        /// <param name="imagePointer">The image pointer.</param>
        public void ReadRawData(IntPtr imagePointer)
        {
            pixelData = Marshal.PtrToStructure(imagePointer, pixelType);
        }

        /// <summary>
        /// Reads the data.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        public void ReadData(Byte[] buffer, Int32 offset)
        {
            Marshal.Copy(buffer, offset, pixelDataPointer, Parent.BytesPerPixel);
            pixelData = Marshal.PtrToStructure(pixelDataPointer, pixelType);
        }

        /// <summary>
        /// Writes the raw data.
        /// </summary>
        /// <param name="imagePointer">The image pointer.</param>
        public void WriteRawData(IntPtr imagePointer)
        {
            Marshal.StructureToPtr(pixelData, imagePointer, false);
        }

        /// <summary>
        /// Writes the data.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        public void WriteData(Byte[] buffer, Int32 offset)
        {
            Marshal.Copy(pixelDataPointer, buffer, offset, Parent.BytesPerPixel);
        }

        #endregion

        #region << IDisposable >>

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Marshal.FreeHGlobal(pixelDataPointer);
        }

        #endregion
    }
}