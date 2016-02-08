using FSO.Common.Rendering.Framework.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Rendering.Framework
{
    public class _3DTargetScene : _3DScene
    {
        public RenderTarget2D Target;
        private GraphicsDevice Device;
        public _3DTargetScene(GraphicsDevice device, ICamera camera, Point size) : this(device, size) { Camera = camera; }
        public _3DTargetScene(GraphicsDevice device, Point size) : base(device)
        {
            Device = device;
            SetSize(size);
        }

        public void SetSize(Point size)
        {
            if (Target != null) Target.Dispose();
            Target = new RenderTarget2D(Device, size.X, size.Y, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 4, RenderTargetUsage.PreserveContents);
        }

        public override void Draw(GraphicsDevice device)
        {
            var oldTargets = device.GetRenderTargets();
            device.SetRenderTarget(Target);
            device.Clear(Color.Transparent);
            device.DepthStencilState = DepthStencilState.Default;
            Camera.ProjectionDirty();
            base.Draw(device);
            device.SetRenderTargets(oldTargets);
        }
    }
}
