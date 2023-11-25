using FSO.Common.Rendering.Framework.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Common.Rendering.Framework
{
    public class _3DTargetScene : _3DScene
    {
        public RenderTarget2D Target;
        private GraphicsDevice Device;
        private int Multisample = 0;
        public Color ClearColor = Color.Transparent;
        public _3DTargetScene(GraphicsDevice device, ICamera camera, Point size, int multisample) : this(device, size, multisample) { Camera = camera; }
        public _3DTargetScene(GraphicsDevice device, Point size, int multisample) : base(device)
        {
            Device = device;
            Multisample = multisample;
            SetSize(size);
        }

        public void SetSize(Point size)
        {
            if (Target != null) Target.Dispose();
            Target = new RenderTarget2D(Device, size.X, size.Y, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, Multisample, RenderTargetUsage.PreserveContents);
        }

        public override void Draw(GraphicsDevice device)
        {
            var oldTargets = device.GetRenderTargets();
            device.SetRenderTarget(Target);
            device.Clear(ClearColor);
            device.DepthStencilState = DepthStencilState.Default;
            Camera.ProjectionDirty();
            base.Draw(device);
            device.SetRenderTargets(oldTargets);
        }

        public override void Dispose()
        {
            base.Dispose();
            Target.Dispose();
        }
    }
}
