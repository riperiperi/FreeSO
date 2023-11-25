using FSO.Common.Utils;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FSO.Common.Rendering
{
    public class CachableTexture2D : Texture2D, ITimedCachable
    {
        /// <summary>
        /// Creates a new texture of the given size
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public CachableTexture2D(GraphicsDevice graphicsDevice, int width, int height)
            : base(graphicsDevice, width, height)
        {
        }
        /// <summary>
        /// Creates a new texture of a given size with a surface format and optional mipmaps 
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="mipmap"></param>
        /// <param name="format"></param>
        public CachableTexture2D(GraphicsDevice graphicsDevice, int width, int height, bool mipmap, SurfaceFormat format)
            : base(graphicsDevice, width, height, mipmap, format)
        {
        }
        /// <summary>
        /// Creates a new texture array of a given size with a surface format and optional mipmaps.
        /// Throws ArgumentException if the current GraphicsDevice can't work with texture arrays
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="mipmap"></param>
        /// <param name="format"></param>
        /// <param name="arraySize"></param>
        public CachableTexture2D(GraphicsDevice graphicsDevice, int width, int height, bool mipmap, SurfaceFormat format, int arraySize)
            : base(graphicsDevice, width, height, mipmap, format, arraySize)
        {

        }

        private bool WasReferenced = true;
        public bool BeingDisposed = false;
        private bool Resurrect = true;
        public object Parent; //set if you want a parent object to be tied to this object (don't kill parent til we die)
        ~CachableTexture2D() {
            if (!IsDisposed && !BeingDisposed)
            {
                //if we are disposed, there's no need to do anything.
                if (WasReferenced)
                {
                    TimedReferenceController.KeepAlive(this, KeepAliveType.DEREFERENCED);
                    WasReferenced = false;
                    GC.ReRegisterForFinalize(this);
                    Resurrect = true;
                }
                else
                {
                    BeingDisposed = true;
                    GameThread.NextUpdate(x => this.Dispose());
                    GC.ReRegisterForFinalize(this);
                    Resurrect = true; //one more final
                }
            }
            else { Resurrect = false; }
        }

        public void Rereferenced(bool saved)
        {
            WasReferenced = saved;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) base.Dispose(disposing);
            else if (Resurrect)
            {
                //in finalizer
                GC.ReRegisterForFinalize(this);
            }
        }
    }
}
