using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;

namespace FSO.LotView.Utils.Camera
{
    public class CameraController2D : ICameraController
    {
        private WorldCamera Camera;
        public ICamera BaseCamera => Camera;
        public bool UseZoomHold => throw new NotImplementedException();

        public bool UseRotateHold => throw new NotImplementedException();

        public void InvalidateCamera(WorldState state)
        {
            var ctr = state.WorldSpace.GetScreenFromTile(state.CenterTile);
            ctr.X = (float)Math.Round(ctr.X);
            ctr.Y = (float)Math.Round(ctr.Y);
            var test = new Vector2(-0.5f, 0);
            test *= 1 << (3 - (int)state.Zoom);
            var back = state.WorldSpace.GetTileFromScreen(ctr + test);
            Camera.CenterTile = new Vector3(back, 0);
            Camera.Zoom = state.Zoom;
            Camera.Rotation = state.Rotation;
            Camera.PreciseZoom = state.PreciseZoom;
        }

        public void SetDimensions(Vector2 dim)
        {
            Camera.ViewDimensions = dim;
        }

        public void RotateHold(float intensity)
        {
            return;
        }

        public void RotatePress(float intensity)
        {
            throw new NotImplementedException();
        }

        public void SetActive(ICameraController previous, World world)
        {

        }

        public void Update(UpdateState state, World world)
        {

        }

        public void ZoomHold(float intensity)
        {
            return;
        }

        public void ZoomPress(float intensity)
        {
            throw new NotImplementedException();
        }
    }
}
