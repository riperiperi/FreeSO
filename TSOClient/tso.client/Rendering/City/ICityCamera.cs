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
        bool HideUI { get; }

        void Update(UpdateState state, Terrain city);
        void MouseOut();
        float GetIsoScale();
        Vector2 CalculateR();
        Vector2 CalculateRShadow();
        void InheritPosition(Terrain parent, World lotWorld, CoreGameScreenController controller);
    }
}
