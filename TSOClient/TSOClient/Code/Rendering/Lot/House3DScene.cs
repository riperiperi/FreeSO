using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.Rendering.Lot.Model;
using TSOClient.Code.Rendering.Lot.Components;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using tso.common.rendering.framework.camera;

namespace TSOClient.Code.Rendering.Lot
{
    /// <summary>
    /// 3D layer of house rendering, this includes terrain and sims
    /// </summary>
    public class House3DScene : IWorldObject
    {
        private HouseRenderState RenderState;
        private List<House3DComponent> Components;
        private OrthographicCamera Camera;

        public House3DScene(HouseRenderState state)
        {
            this.RenderState = state;
            this.Camera = (OrthographicCamera)state.Camera;
            this.Components = new List<House3DComponent>();
        }


        /// <summary>
        /// Provides the 3d layer with information about the lot
        /// </summary>
        /// <param name="model"></param>
        public void LoadHouse(HouseModel model)
        {
            /** Add terrain **/
            Components.Add(new TerrainComponent(RenderState));

            var cube1 = new CubeComponent(Color.Red, new Vector3(3.0f, 3.0f, 3.0f));
            cube1.Position = new Vector3(32.0f * 3, 0.0f, 32.0f * 3);
            Components.Add(cube1);
        }

        /// <summary>
        /// Render the 3D objects to the screen
        /// </summary>
        /// <param name="device"></param>
        /// <param name="state"></param>
        public void Draw(GraphicsDevice device, HouseRenderState state)
        {
            Components.ForEach(x => x.Draw(device, state));
        }


        /// <summary>
        /// A view component has changed (rotation, zoom, scroll).
        /// We need to adjust the camera
        /// </summary>
        private void InvalidateCamera()
        {
            //Camera.Translation = new Vector3(-radius, 0.0f, -radius);

            //Camera translation for scroll position
            var offsetX = RenderState.TileToWorld(RenderState.FocusTile.X);
            var offsetY = RenderState.TileToWorld(RenderState.FocusTile.Y);

            var centerX = Camera.Target.X;
            var centerY = Camera.Target.Z;

            offsetX -= centerX;
            offsetY -= centerY;// *1.03f;
            
            switch (RenderState.Zoom)
            {
                case HouseZoom.FarZoom:
                    Camera.Zoom = 152;
                    break;

                case HouseZoom.MediumZoom:
                    Camera.Zoom = 76;
                    break;

                case HouseZoom.CloseZoom:
                    Camera.Zoom = 38;
                    break;
            }
            Camera.Translation = new Vector3(offsetX, 0.0f, offsetY);

            //Camera.Target = new Vector3(offsetX, 0.0f, offsetY);
            //Camera.Position = new Vector3(offsetX + 96.0f, Camera.Position.Y, offsetY + 96.0f);
        }



        #region IWorldObject Members

        public void OnZoomChange(TSOClient.Code.Rendering.Lot.Model.HouseRenderState state)
        {
            InvalidateCamera();
            Components.ForEach(x => x.OnZoomChange(state));
        }

        public void OnRotationChange(TSOClient.Code.Rendering.Lot.Model.HouseRenderState state)
        {
            InvalidateCamera();
            Components.ForEach(x => x.OnRotationChange(state));
        }

        public void OnScrollChange(TSOClient.Code.Rendering.Lot.Model.HouseRenderState state)
        {
            InvalidateCamera();
            Components.ForEach(x => x.OnScrollChange(state));
        }

        #endregion
    }
}
