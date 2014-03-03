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
using TSOClient.VM;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSOClient.Code.Rendering;
using TSOClient.Code.Utils;
using TSO.Common.rendering.framework.model;
using TSO.Common.rendering.framework;
using TSO.Vitaboy;
using TSO.Common.rendering.framework.camera;

namespace TSOClient.Code.UI.Controls
{
    /// <summary>
    /// Renders a sim in the UI, this class just helps translate the UI world
    /// into the 3D world for sim rendering
    /// </summary>
    public class UISim : UIElement
    {
        //private SimRenderer SimRender;
        private _3DScene Scene;
        private BasicCamera Camera;
        public AdultSimAvatar Avatar;

        /** 45 degrees in either direction **/
        public float RotationRange = 45;
        public float RotationStartAngle = 180;
        public float RotationSpeed = new TimeSpan(0, 0, 10).Ticks;
        public bool AutoRotate = true;

        public float SimScale = 0.45f;
        public float ViewScale = 17.0f;

        public UISim()
        {
            Camera = new BasicCamera(GameFacade.GraphicsDevice, new Vector3(0.0f, 7.0f, -17.0f), Vector3.Zero, Vector3.Up);
            Scene = new _3DScene(GameFacade.Game.GraphicsDevice, Camera);
            Scene.ID = "UISim";

            GameFacade.Game.GraphicsDevice.DeviceReset += new EventHandler(GraphicsDevice_DeviceReset);

            Avatar = new AdultSimAvatar();
            Avatar.Scene = Scene;
            Avatar.Scale = new Vector3(0.45f);
            Scene.Add(Avatar);

            GameFacade.Scenes.AddExternal(Scene);
        }

        private void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            Scene.DeviceReset(GameFacade.Game.GraphicsDevice);
        }

        private void CalculateView()
        {
            var screen = GameFacade.Screens.CurrentUIScreen;
            if (screen == null) { return; }

            var globalLocation = screen.GlobalPoint(this.LocalPoint(Vector2.Zero));
            Camera.ProjectionOrigin = globalLocation;
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (AutoRotate){
                var startAngle = RotationStartAngle;
                var time = state.Time.TotalRealTime.Ticks;
                var phase = (time % RotationSpeed) / RotationSpeed;
                var multiplier = Math.Sin((Math.PI * 2) * phase);
                var newAngle = startAngle + (RotationRange * multiplier);
                Avatar.RotationY = (float)MathUtils.DegreeToRadian(newAngle);
            }
        }

        private Vector2 m_Size;
        public override Vector2 Size
        {
            get { return m_Size; }
            set
            {
                m_Size = value;
                CalculateView();
            }
        }

        protected override void CalculateMatrix()
        {
            base.CalculateMatrix();

            /** Re-calculate the 3D world **/
            CalculateView();
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!UISpriteBatch.Invalidated)
            {
                if (!_3DScene.IsInvalidated)
                {
                    batch.Pause();
                    Avatar.Draw(GameFacade.GraphicsDevice);
                    batch.Resume();
                }
            }
        }
    }
}
