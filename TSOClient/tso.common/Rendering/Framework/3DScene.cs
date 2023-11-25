using System;
using System.Collections.Generic;
using FSO.Common.Rendering.Framework.Camera;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Common.Rendering.Framework
{
    /// <summary>
    /// A scene capable of rendering 3D elements.
    /// </summary>
    public class _3DScene : _3DAbstract
    {
        private List<_3DComponent> m_Elements = new List<_3DComponent>();

        public new _3DLayer Parent;

        /// <summary>
        /// Creates a new _3DScene instance.
        /// </summary>
        /// <param name="Device">A GraphicsDevice instance used for rendering.</param>
        /// <param name="camera">A camera inheriting from ICamera used for rendering.</param>
        public _3DScene(GraphicsDevice Device, ICamera camera) : base(Device)
        {
            this.Camera = camera;
        }

        /// <summary>
        /// Creates a new _3DScene instance.
        /// </summary>
        /// <param name="Device">A GraphicsDevice instance used for rendering.</param>
        public _3DScene(GraphicsDevice Device) : base(Device)
        {
        }

        /// <summary>
        /// Graphics device was reset (happens when scene is updated or minimized.)
        /// </summary>
        void m_Device_DeviceReset(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the _3DComponents that make up this scene.
        /// </summary>
        /// <returns>A List of _3DComponent instances.</returns>
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

        /// <summary>
        /// Removes a 3D element from this 3DScene.
        /// </summary>
        /// <param name="item">The _3DComponent instance to remove.</param>
        public void Remove(_3DComponent item)
        {
            m_Elements.Remove(item);
        }

        /// <summary>
        /// Adds a 3D element to this 3DScene.
        /// </summary>
        /// <param name="item">The _3DComponent instance to add.</param>
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
