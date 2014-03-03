using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSO.Common.rendering.framework.camera;
using TSO.Common.rendering.framework.model;

namespace TSO.Common.rendering.framework
{
    public abstract class _3DAbstract
    {
        public ICamera Camera;
        public string ID;
        public abstract List<_3DComponent> GetElements();
        public abstract void Add(_3DComponent item);
        public abstract void Update(UpdateState Time);
        public abstract void Draw(GraphicsDevice device);

        public virtual void PreDraw(GraphicsDevice device)
        {
        }

        public virtual void Initialize(_3DLayer layer)
        {
        }

        public _3DAbstract(GraphicsDevice Device)
        {
            m_Device = Device;
            m_Device.DeviceReset += new EventHandler(m_Device_DeviceReset);
        }

        private void m_Device_DeviceReset(object sender, EventArgs e)
        {
            DeviceReset(m_Device);
        }

        protected GraphicsDevice m_Device; 

        public abstract void DeviceReset(GraphicsDevice Device);
        public static bool IsInvalidated;
    }
}
