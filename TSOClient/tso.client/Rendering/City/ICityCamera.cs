using FSO.Client.Controllers;
using FSO.Common.Rendering.Framework.Camera;
using FSO.Common.Rendering.Framework.Model;
using FSO.LotView;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Rendering.City
{
    public interface ICityCamera : ICamera
    {
        TerrainZoomMode Zoomed { get; set; }
        float LotZoomProgress { get; set; }
        float ZoomProgress { get; set; }
        float LotSquish { get; }
        float FogMultiplier { get; }
        float DepthBiasScale { get; }
        float FarUIFade { get; }
        CityCameraCenter CenterCam { get; set; }
        bool HideUI { get; }

        void Update(UpdateState state, Terrain city);
        void MouseEvent(FSO.Common.Rendering.Framework.IO.UIMouseEventType type, UpdateState state);
        float GetIsoScale();
        Vector2 CalculateR();
        Vector2 CalculateRShadow();
        void InheritPosition(Terrain parent, World lotWorld, CoreGameScreenController controller, bool instant);

        void CenterCamera(CityCameraCenter center);
        void ClearCenter();
    }

    public class CityCameraCenter
    {
        public Vector2 Center;
        public float YAngle;
        public float Dist;

        public float RotAngle;
        public int ID;
    }
}
