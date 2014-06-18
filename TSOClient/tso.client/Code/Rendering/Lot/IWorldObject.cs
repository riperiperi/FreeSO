using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.Rendering.Lot.Model;

namespace TSOClient.Code.Rendering.Lot
{
    public interface IWorldObject
    {
        void OnZoomChange(HouseRenderState state);
        void OnRotationChange(HouseRenderState state);
        void OnScrollChange(HouseRenderState state);
    }
}
