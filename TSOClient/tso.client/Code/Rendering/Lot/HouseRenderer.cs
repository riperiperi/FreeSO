using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.ThreeD;
using TSOClient.Code.Rendering.Lot.Model;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Rendering.Lot.Components;
using TSOClient.Code.Utils;
using TSOClient.Code.Data;
using TSOClient.Code.UI.Model;
using tso.common.rendering.framework.model;
using tso.common.rendering.framework;

namespace TSOClient.Code.Rendering.Lot
{
    public class HouseRenderer : _3DComponent
    {
        private HouseData House;

        private List<House2DComponent> StaticLayer;
        private List<House2DComponent> DynamicLayer;

        private HouseRenderState RenderState;


        public HouseRenderer()
        {
        }


        public HouseRotation GetRotation()
        {
            return RenderState.Rotation;
        }

        public void SetRotation(HouseRotation rotation)
        {
            RenderState.Rotation = rotation;
        }

        public void SetZoom(HouseZoom zoom)
        {
            RenderState.Zoom = zoom;
        }

        public void SetModel(HouseData house)
        {
            this.House = house;

            StaticLayer = new List<House2DComponent>();

            //StaticLayer.Add(new TerrainComponent());

            foreach (var floor in house.World.Floors.Where(x => x.Level == 0))
            {
                StaticLayer.Add(new FloorComponent {
                    Position = new Microsoft.Xna.Framework.Point(floor.X, floor.Y)
                });
            }

            foreach (var wall in house.World.Walls.Where(x => x.Level == 0))
            {
                StaticLayer.Add(new WallComponent (wall){
                    Position = new Microsoft.Xna.Framework.Point(wall.X, wall.Y)
                });
            }


            RenderState = new HouseRenderState();
            RenderState.Rotation = HouseRotation.Angle360;
            RenderState.Size = 64;
            //RenderState.ScrollOffset = new Microsoft.Xna.Framework.Vector2(32, 32);
            RenderState.Zoom = HouseZoom.FarZoom;
            RenderState.Device = GameFacade.GraphicsDevice;


            
            /*this.Layers = new LotLevel[2];
            for (var i = 0; i < 2; i++)
            {
                var layer = new LotLevel(i);
                layer.Process(house);
                layer.ProcessGeometry();
                Layers[i] = layer;
            }*/
        }


        public override void Update(UpdateState GState)
        {
        }

        public override void Draw(GraphicsDevice device)
        {
            var batch = new HouseBatch(GameFacade.GraphicsDevice);
            //batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.SaveState);
            batch.Begin();
            foreach (var item in StaticLayer)
            {
                item.Draw(RenderState, batch);
            }
            batch.End();


            //var layer = Layers[0];
            //layer.DrawFloor(device, scene, this);
        }
    }
}
