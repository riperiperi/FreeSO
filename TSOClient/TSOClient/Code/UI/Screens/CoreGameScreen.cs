/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

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
            ///** City Scene **/
            var scene = new ThreeDScene();
            //scene.Camera.Position = new Vector3(0, -14.1759f, 10f);
            //scene.Camera.Position = new Vector3(0, 0, 17.0f);
            //scene.Camera.Target = Vector3.Zero;
            scene.Camera.Up = Vector3.Forward;

            var city = new CitySceneElement();

            if (PlayerAccount.CurrentlyActiveSim != null)
                city.Initialize(PlayerAccount.CurrentlyActiveSim.ResidingCity.Name);
            else //Debug purposes...
                city.Initialize("Blazing Falls");

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

            ucp = new UIUCP();
            ucp.Y = ScreenHeight - 210;
            this.Add(ucp);

            gizmo = new UIGizmo();
            gizmo.X = ScreenWidth - 500;
            gizmo.Y = ScreenHeight - 300;
            this.Add(gizmo);

            GameFacade.Scenes.AddScene(scene);
        }
    }
}
