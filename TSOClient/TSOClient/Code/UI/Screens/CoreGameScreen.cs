using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Panels;
using TSOClient.ThreeD;
using TSOClient.Code.Rendering.City;
using Microsoft.Xna.Framework;
using TSOClient.Code.Utils;

namespace TSOClient.Code.UI.Screens
{
    public class CoreGameScreen : GameScreen
    {
        private UIUCP ucp;
        private UIGizmo gizmo;

        public CoreGameScreen()
        {
            ucp = new UIUCP();
            ucp.Y = ScreenHeight - 210;
            this.Add(ucp);

            gizmo = new UIGizmo();
            gizmo.X = ScreenWidth - 500;
            gizmo.Y = ScreenHeight - 300;
            this.Add(gizmo);


            ///** City Scene **/
            //var scene = new ThreeDScene();
            //scene.Camera.Position = new Vector3(0, -14.1759f, 10f);
            //scene.Camera.Target = Vector3.Zero;
            //scene.Camera.Up = Vector3.Up;

            ////, new Vector3(0, 0, 0), Vector3.Up
            //var city = new CitySceneElement();
            //city.RotationX = (float)MathUtils.DegreeToRadian(-13);
            //city.Scale = new Vector3(1.24f);

            //city.Initialize();
            //scene.Add(city);

            //GameFacade.Scenes.AddScene(scene);
        }
    }
}
