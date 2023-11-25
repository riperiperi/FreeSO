using FSO.Common;
using FSO.Common.Rendering.Framework;
using FSO.Common.Utils;
using FSO.LotView;
using FSO.LotView.Utils;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.IDE.Common
{
    public class ExternalWorld : World
    {
        public ExternalWorld(GraphicsDevice Device)
            : base(Device)
        {
            LimitScroll = false;
        }

        public override void InitDefaultGraphicsMode()
        {
            SetGraphicsMode(LotView.Model.GlobalGraphicsMode.Full2D, true);
        }

        public override void Initialize(_3DLayer layer)
        {
            /**
             * Setup world state, this object acts as a facade
             * to world objects as well as providing various
             * state settings for the world and helper functions
             */
            State = new WorldState(layer.Device, layer.Device.Viewport.Width / FSOEnvironment.DPIScaleFactor, layer.Device.Viewport.Height / FSOEnvironment.DPIScaleFactor, this);
            State.DisableSmoothRotation = true;
            State.ForceImmediate = true;
            //State.SetCameraType(this, LotView.Utils.Camera.CameraControllerType._2D);
            State.AmbientLight = new Texture2D(layer.Device, 256, 256);
            State.OutsidePx = new Texture2D(layer.Device, 1, 1);
            State._2D = new FSO.LotView.Utils._2DWorldBatch(layer.Device, 2, new SurfaceFormat[] {
                _2DWorldBatch.BUFFER_SURFACE_FORMATS[0],
                _2DWorldBatch.BUFFER_SURFACE_FORMATS[_2DWorldBatch.BUFFER_THUMB_DEPTH] }, new bool[] { true, false }, _2DWorldBatch.SCROLL_BUFFER);
            State._2D.AdvLight = TextureGenerator.GetPxWhite(layer.Device);
            State.DrawOOB = true;
            UseBackbuffer = false;

            base.Camera = State.Camera;

            HasInitGPU = true;
            HasInit = HasInitGPU & HasInitBlueprint;
        }
    }
}
