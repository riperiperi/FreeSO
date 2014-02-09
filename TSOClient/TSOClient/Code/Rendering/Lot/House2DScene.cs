using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.Rendering.Lot.Model;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Rendering.Lot.Components;
using Microsoft.Xna.Framework;
using TSOClient.Code.Utils;
using tso.common.utils;

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

            var walls = model.GetWalls().Where(x => x.Level == 0);
            walls.Reverse();
            foreach (var wall in walls) { Walls.AddComponent(wall); }


            //Walls.AddComponent(new DummyZSprite(@"E:\Temp\tso\tower_"));
            //Walls.AddComponent(new DummyZSprite(@"E:\Temp\tso\chair_"));
        }



        public void Draw(GraphicsDevice device, HouseRenderState state)
        {
            var batch = new HouseBatch(device);
            //batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            batch.Begin();
            Floor.Draw(device, batch, state);
            Walls.Draw(device, batch, state);

            /** Draw indicator in center of screen **/
            var rectSize = 5;
            var gw = GlobalSettings.Default.GraphicsWidth;
            var gh = GlobalSettings.Default.GraphicsHeight;

            batch.Draw(new TSOClient.Code.Rendering.Lot.Framework.HouseBatchSprite {
                DestRect = new Rectangle((gw-rectSize)/2, (gh-rectSize)/2, rectSize, rectSize),
                SrcRect = new Rectangle(0, 0, 1, 1),
                Pixel = TextureUtils.TextureFromColor(device, Color.Pink),
                RenderMode = TSOClient.Code.Rendering.Lot.Framework.HouseBatchRenderMode.NO_DEPTH
            });

            batch.End();
        }


        #region IWorldObject Members

        public void OnZoomChange(HouseRenderState state)
        {
            Floor.OnZoomChange(state);
            Walls.OnZoomChange(state);
        }

        public void OnRotationChange(HouseRenderState state)
        {
            Floor.OnRotationChange(state);
            Walls.OnRotationChange(state);
        }

        public void OnScrollChange(HouseRenderState state)
        {
            Floor.OnScrollChange(state);
            Walls.OnScrollChange(state);
        }

        #endregion
    }
}
