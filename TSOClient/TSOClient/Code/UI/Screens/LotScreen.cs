using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.Rendering.Lot.Model;
using TSOClient.Code.Rendering.Lot;
using TSOClient.ThreeD;
using SimsLib.FAR1;
using SimsLib.FAR3;
using TSOClient.Code.Data;
using TSOClient.Code.UI.Panels;
using Microsoft.Xna.Framework;

namespace TSOClient.Code.UI.Screens
{
    public class LotScreen : GameScreen
    {
        //private HouseRenderer Renderer;
        private HouseScene Scene;
        private UIUCP ucp;

        public LotScreen()
        {
            ArchitectureCatalog.Init();

            var lotInfo = HouseData.Parse(GameFacade.GameFilePath("housedata/blueprints/restaurant00_00.xml"));

            Scene = new HouseScene();
            Scene.LoadHouse(lotInfo);
            GameFacade.Scenes.AddScene(Scene);


            //Renderer = new HouseRenderer();
            //Renderer.SetModel(lotInfo);
            ////Renderer.Position = new Microsoft.Xna.Framework.Vector3(-32.0f, -40.0f, 0.0f);

            //var scene = new ThreeDScene();
            //var focusPoint = Vector3.Zero;

            //var yValue = (float)Math.Cos(MathHelper.ToRadians(30.0f)) * 96.0f;
            //var cameraOffset = new Vector3(-96.0f, yValue, 96.0f);
            //var rotatedOffset = Vector3.Transform(cameraOffset, Microsoft.Xna.Framework.Matrix.CreateRotationY(MathHelper.PiOver2 * 0.5f));

            ////rotatedOffset = Vector3.Transform(rotatedOffset, Microsoft.Xna.Framework.Matrix.CreateScale(3f));
            ////Renderer.Position = new Vector3(-96.0f, 0.0f, -96.0f);

            //scene.Camera.Position = cameraOffset;// new Microsoft.Xna.Framework.Vector3(0, 0, 80);
            //scene.Add(Renderer);
            //Renderer.Scale = new Vector3(0.005f);

            //GameFacade.Scenes.AddScene(scene);


            ucp = new UIUCP();
            ucp.Y = ScreenHeight - 210;
            ucp.OnZoomChanged += new UCPZoomChangeEvent(ucp_OnZoomChanged);
            ucp.OnRotateChanged += new UCPRotateChangeEvent(ucp_OnRotateChanged);
            this.Add(ucp);
        }

        void ucp_OnRotateChanged(UCPRotateDirection direction)
        {
            var newDirection = (HouseRotation)(
                (((int)Scene.Rotation) + (direction == UCPRotateDirection.Clockwise ? -1 : 1)) % 4
            );

            Scene.Rotation = newDirection;
        }

        void ucp_OnZoomChanged(HouseZoom zoom)
        {
            Scene.Zoom = zoom;
        }
    }
}
