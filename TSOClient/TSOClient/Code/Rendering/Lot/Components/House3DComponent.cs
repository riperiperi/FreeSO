using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.Rendering.Lot.Model;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.Rendering.Lot.Components
{
    public abstract class House3DComponent : IWorldObject
    {
        public Vector3 Position;
        public abstract void Draw(GraphicsDevice device, HouseRenderState state);


        #region IWorldObject Members

        public void OnZoomChange(TSOClient.Code.Rendering.Lot.Model.HouseRenderState state)
        {
        }

        public void OnRotationChange(TSOClient.Code.Rendering.Lot.Model.HouseRenderState state)
        {
        }

        public void OnScrollChange(TSOClient.Code.Rendering.Lot.Model.HouseRenderState state)
        {
        }

        #endregion
    }
}
