using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using TSOClient.Code.Rendering.Lot.Model;

namespace TSOClient.Code.Rendering.Lot.Components
{
    public abstract class House2DComponent
    {
        /// <summary>
        /// Position of fixed tile objects on the tile space
        /// </summary>
        public Point Position;

        /// <summary>
        /// Height of this component, used to calculate damage region
        /// </summary>
        public abstract int Height { get; }
        
        public virtual void OnZoomChanged(HouseRenderState state)
        {
        }

        public virtual void OnRotationChanged(HouseRenderState state)
        {
        }

        public virtual void OnScrollChange(HouseRenderState state)
        {
        }

        public abstract void Draw(HouseRenderState state, HouseBatch batch);
    }
}
