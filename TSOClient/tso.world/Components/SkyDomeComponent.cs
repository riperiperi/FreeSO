using FSO.Common.Rendering.Framework.Camera;
using FSO.LotView.Model;
using FSO.LotView.Utils;
using FSO.LotView.Utils.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

namespace FSO.LotView.Components
{
    public class SkyDomeComponent : AbstractSkyDome
    {
        public Blueprint BP;

        public SkyDomeComponent(GraphicsDevice GD, Blueprint bp) : base(GD, (float)bp.OutsideTime)
        {
            BP = bp;
        }


        public void Draw(GraphicsDevice gd, WorldState state)
        {
            ICamera active3D = (state.Cameras.ActiveCamera as CameraController3D)?.Camera ?? state.Camera3D;
            ICamera allowSwitch = state.Cameras.TransitionWeights.Any(x => x.Camera is WorldCamera || x.IsLinear) ? active3D : state.Camera;
            Draw(gd, state.OutsideColor,
                state.Camera.View,
                allowSwitch.Projection, //((state.Camera as WorldCamera3D)?.BaseProjection() ?? state.Camera.Projection), 
                (float)BP.OutsideTime, 
                BP.Weather, 
                state.Light?.SunVector ?? 
                new Vector3(0, 1, 0),
                1f+((state.Camera as WorldCamera3D)?.FromIntensity ?? 0f) * 76);
        }
    }
}
