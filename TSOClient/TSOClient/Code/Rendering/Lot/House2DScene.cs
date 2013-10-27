using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.Rendering.Lot.Model;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.Rendering.Lot
{
    public class House2DScene : IWorldObject
    {
        private House2DLayer Floor;
        private House2DLayer Walls;



        public House2DScene()
        {
            Floor = new House2DLayer();
            Walls = new House2DLayer();
        }


        /// <summary>
        /// Setup the initial rendering objects for this house
        /// model
        /// </summary>
        /// <param name="model"></param>
        public void LoadHouse(HouseModel model)
        {
            /** Get all the first floor tiles **/
            var floors = model.GetFloors().Where(x => x.Level == 0);
            foreach (var floor in floors) { Floor.AddComponent(floor); }
        }



        public void Draw(GraphicsDevice device, HouseRenderState state)
        {
            var batch = new HouseBatch(device);
            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            Floor.Draw(device, batch, state);
            batch.End();
        }


        #region IWorldObject Members

        public void OnZoomChange(HouseRenderState state)
        {
            Floor.OnZoomChange(state);
        }

        public void OnRotationChange(HouseRenderState state)
        {
            Floor.OnRotationChange(state);
        }

        public void OnScrollChange(HouseRenderState state)
        {
            Floor.OnScrollChange(state);
        }

        #endregion
    }
}
