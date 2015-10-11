using System;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;

namespace MGWinForms
{
    public class GraphicsDeviceService : IGraphicsDeviceService
    {
        private static readonly GraphicsDeviceService _instance = new GraphicsDeviceService();
        private static int _refCount;

        private GraphicsDevice _device;

        public GraphicsDevice GraphicsDevice
        {
            get { return _device; }
        }

        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset = (s, e) => { };
        public event EventHandler<EventArgs> DeviceResetting = (s, e) => { };

        protected GraphicsDeviceService ()
        { }

        public static GraphicsDeviceService AddRef (IntPtr windowHandle, int width, int height)
        {
            if (Interlocked.Increment(ref _refCount) == 1)
                _instance.CreateDevice(windowHandle, width, height);

            return _instance;
        }

        public void Release ()
        {
            Release(true);
        }

        protected void Release (bool disposing)
        {
            if (Interlocked.Decrement(ref _refCount) == 0) {
                if (disposing) {
                    if (DeviceDisposing != null)
                        DeviceDisposing(this, EventArgs.Empty);

                    _device.Dispose();
                }

                _device = null;
            }
        }

        protected void CreateDevice (IntPtr windowHandle, int width, int height)
        {
            GraphicsAdapter adapter = GraphicsAdapter.DefaultAdapter;
            GraphicsProfile profile = GraphicsProfile.Reach;
            PresentationParameters pp = new PresentationParameters() {
                DeviceWindowHandle = windowHandle,
                BackBufferWidth = Math.Max(width, 1),
                BackBufferHeight = Math.Max(height, 1),
                MultiSampleCount = 8
            };
            
            _device = new GraphicsDevice(adapter, profile, pp);

            if (DeviceCreated != null)
                DeviceCreated(this, EventArgs.Empty);
        }

        public void ResetDevice (int width, int height)
        { }
    }
}