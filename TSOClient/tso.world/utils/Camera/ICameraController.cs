using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView.Utils.Camera
{
    public interface ICameraController
    {
        ICamera BaseCamera { get; }
        bool UseZoomHold { get; }
        bool UseRotateHold { get; }

        void ZoomPress(float intensity);

        void ZoomHold(float intensity);

        void RotatePress(float intensity);

        void RotateHold(float intensity);

        void Update(UpdateState state, World world);

        void SetActive(ICameraController previous, World world);

        void InvalidateCamera(WorldState state);

        void SetDimensions(Vector2 dim);
    }
}
