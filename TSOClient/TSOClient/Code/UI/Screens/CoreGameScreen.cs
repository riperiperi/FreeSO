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
using TSOClient.Code.Rendering;
using Matrix = Microsoft.Xna.Framework.Matrix;
using tso.common.rendering.framework;

namespace TSOClient.Code.UI.Screens
{
    public class CoreGameScreen : TSOClient.Code.UI.Framework.GameScreen
    {
        private UIUCP ucp;
        private UIGizmo gizmo;

        public CoreGameScreen()
        {
            ucp = new UIUCP();
            ucp.Y = ScreenHeight - 210;
            ucp.SetMode(UIUCP.UCPMode.CityMode);
            this.Add(ucp);

            gizmo = new UIGizmo();
            gizmo.X = ScreenWidth - 500;
            gizmo.Y = ScreenHeight - 300;
            this.Add(gizmo);

            ///** City Scene **/
            var scene = new _3DScene();
            var city = new CitySceneElement();
            city.Initialize();


            var camera = new CityCamera();
            camera.ScreenWidth = (float)this.ScreenWidth;
            camera.ScreenHeight = (float)this.ScreenHeight;
            camera.AspectRatioMultiplier = 0.97f;
            camera.FarZoomScale = 5.0f;
            camera.RotationY = 29.5f;
            camera.TranslationX = -362.0f;


            scene.Camera = camera;
            scene.Add(city);

            GameFacade.Scenes.Add(scene);
        }
    }
}
