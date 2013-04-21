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
            var scene = new ThreeDScene();
            //scene.Camera.Position = new Vector3(0, -14.1759f, 10f);
            scene.Camera.Position = new Vector3(0, 0, 17.0f);
            scene.Camera.Target = Vector3.Zero;
            scene.Camera.Up = Vector3.Up;


            ////, new Vector3(0, 0, 0), Vector3.Up
            var city = new CitySceneElement();
            city.Initialize();


            //city.RotationX = (float)MathUtils.DegreeToRadian(347);
            //city.Scale = new Vector3(1.24f);

            scene.Camera.Target = new Vector3(
                ((city.City.Width * city.Geom.CellWidth) / 2),
                -((city.City.Height * city.Geom.CellHeight) / 2),
                0.0f);

            scene.Camera.Position =

                Vector3.Transform(
                    new Vector3(
                        scene.Camera.Target.X,
                        scene.Camera.Target.Y,
                        city.City.Width / GameFacade.GraphicsDevice.Viewport.Width),
                    Microsoft.Xna.Framework.Matrix.CreateRotationY((float)MathUtils.DegreeToRadian(-200)));


            
            





            scene.Add(city);

            GameFacade.Scenes.AddScene(scene);
        }
    }
}
