using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.Rendering.Lot.Components;
using TSOClient.Code.Rendering.Lot.Model;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.Rendering.Lot
{
    public class House2DLayer : IWorldObject
    {
        protected List<House2DComponent> Components = new List<House2DComponent>();
        protected bool m_Dirty;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="comp"></param>
        public void AddComponent(House2DComponent comp)
        {
            this.Components.Add(comp);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        /// <param name="batch"></param>
        /// <param name="state"></param>
        public void Draw(GraphicsDevice device, HouseBatch batch, HouseRenderState state)
        {
            Components.ForEach(x => x.Draw(state, batch));
        }

        #region IWorldObject Members

        public void OnZoomChange(HouseRenderState state)
        {
            Components.ForEach(x => x.OnZoomChanged(state));
            m_Dirty = true;
        }

        public void OnRotationChange(HouseRenderState state)
        {
            Components.ForEach(x => x.OnRotationChanged(state));
            m_Dirty = true;
        }

        public void OnScrollChange(HouseRenderState state)
        {
            Components.ForEach(x => x.OnScrollChange(state));
            m_Dirty = true;
        }

        #endregion
    }
}
