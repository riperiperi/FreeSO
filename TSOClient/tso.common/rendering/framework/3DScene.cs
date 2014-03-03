using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Common.rendering.framework.camera;
using Microsoft.Xna.Framework.Graphics;
using TSO.Common.rendering.framework.model;

namespace TSO.Common.rendering.framework
{
    public class _3DScene : _3DAbstract
    {
        private List<_3DComponent> m_Elements = new List<_3DComponent>();

        public _3DLayer Parent;

        public _3DScene(GraphicsDevice Device, ICamera camera) : base(Device)
        {
            this.Camera = camera;
        }

        public _3DScene(GraphicsDevice Device) : base(Device)
        {
        }

        void m_Device_DeviceReset(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public override List<_3DComponent> GetElements()
        {
            return m_Elements;
        }

        public override void Initialize(_3DLayer layer)
        {
            this.Parent = layer;

            foreach (var element in m_Elements)
            {
                element.Initialize();
            }
        }

        public override void Update(UpdateState state)
        {
            for (int i = 0; i < m_Elements.Count; i++)
            {
                m_Elements[i].Update(state);
            }
        }

        public void Remove(_3DComponent item){
            m_Elements.Remove(item);
        }

        public override void Add(_3DComponent item)
        {
            m_Elements.Add(item);
            item.Scene = this;
            if (this.Parent != null)
            {
                item.Initialize();
            }
        }

        public override void PreDraw(GraphicsDevice device){
        }

        public override void Draw(GraphicsDevice device)
        {
            for (int i = 0; i < m_Elements.Count; i++)
            {
                m_Elements[i].Draw(device);
            }
        }

        public override string ToString()
        {
            if (ID != null)
            {
                return ID;
            }

            return base.ToString();
        }

        /// <summary>
        /// GraphicsDevice was reset.
        /// </summary>
        /// <param name="Device">The GraphicsDevice instance.</param>
        public override void DeviceReset(GraphicsDevice Device)
        {
            for (int i = 0; i < m_Elements.Count; i++)
                m_Elements[i].DeviceReset(Device);
        }
    }
}
