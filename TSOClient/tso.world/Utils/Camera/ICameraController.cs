using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;

namespace FSO.LotView.Utils.Camera
{
    public interface ICameraController
    {
        ICamera BaseCamera { get; }
        bool UseZoomHold { get; }
        bool UseRotateHold { get; }
        WorldRotation CutRotation { get; }

        void ZoomPress(float intensity);

        void ZoomHold(float intensity);

        void RotatePress(float intensity);

        void RotateHold(float intensity);

        void Update(UpdateState state, World world);

        ICameraController BeforeActive(ICameraController previous, World world);

        void OnActive(ICameraController previous, World world);

        void InvalidateCamera(WorldState state);

        void SetDimensions(Vector2 dim);
    }
}
