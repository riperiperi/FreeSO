using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSOFacadeWorker
{
    public class GraphicsDeviceServiceMock : IGraphicsDeviceService
    {
        GraphicsDevice _GraphicsDevice;
        Form HiddenForm;

        public GraphicsDeviceServiceMock()
        {
            HiddenForm = new Form()
            {
                Visible = true,
                ShowInTaskbar = false
            };

            var Parameters = new PresentationParameters()
            {
                BackBufferWidth = 1280,
                BackBufferHeight = 720,
                DeviceWindowHandle = HiddenForm.Handle,
                PresentationInterval = PresentInterval.Immediate,
                IsFullScreen = false
            };

            _GraphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.Reach, Parameters);
            _GraphicsDevice.Present();
        }

        public GraphicsDevice GraphicsDevice
        {
            get { return _GraphicsDevice; }
        }

        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;

        public void Release()
        {
            _GraphicsDevice.Dispose();
            _GraphicsDevice = null;

            HiddenForm.Close();
            HiddenForm.Dispose();
            HiddenForm = null;
        }
    }
}
