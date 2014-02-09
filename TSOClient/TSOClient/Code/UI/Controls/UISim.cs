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
using SimsLib.ThreeD;
using TSOClient.Code.Rendering;
using TSOClient.Code.Rendering.Sim;
using TSOClient.Code.Utils;
using tso.common.rendering.framework.model;
using tso.common.rendering.framework;

namespace TSOClient.Code.UI.Controls
{
    /// <summary>
    /// Renders a sim in the UI, this class just helps translate the UI world
    /// into the 3D world for sim rendering
    /// </summary>
    public class UISim : UIElement
    {
        //private SimRenderer SimRender;
        private _3DScene SimScene;

        /** 45 degrees in either direction **/
        public float RotationRange = 45;
        public float RotationStartAngle = 180;
        public float RotationSpeed = new TimeSpan(0, 0, 10).Ticks;
        public bool AutoRotate = true;

        public float SimScale = 0.45f;
        public float ViewScale = 17.0f;

        public UISim()
        {
            //SimRender = new SimRenderer();
            //SimRender.ID = "SimRender";

            //SimScene = new ThreeDScene();
            //SimScene.ID = "SimScene";
            //SimScene.Camera = new Camera(new Vector3(0.0f, 7.0f, -17.0f), Vector3.Zero, Vector3.Up);
            //SimScene.Add(SimRender);
            //GameFacade.Scenes.AddScene(SimScene); //Why the %&(¤%( was this commented out? LET STAY!!

            ///** Default settings **/
            //SimRender.Scale = new Vector3(0.45f);
            ////SimRender.RotationX = (float)MathUtils.DegreeToRadian(5);
            ////SimRender.RotationX = (float)MathUtils.DegreeToRadian(RotationStartAngle);
            ////
            ////var scene = new TSOClient.ThreeD.ThreeDScene();
            ////scene.Add(a);
            //GameFacade.Scenes.AddExternalScene(SimScene);
        }

        private void CalculateView()
        {
            //SimRender.Scale = new Vector3(_Scale.X * SimScale, _Scale.Y * SimScale, 1.0f);

            var screen = GameFacade.Screens.CurrentUIScreen;
            if (screen == null) { return; }

            var globalLocation = screen.GlobalPoint(this.LocalPoint(Vector2.Zero));
            SimScene.Camera.ProjectionOrigin = globalLocation;
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            if (AutoRotate)
            {
                var startAngle = RotationStartAngle;
                var time = state.Time.TotalRealTime.Ticks;
                var phase = (time % RotationSpeed) / RotationSpeed;

                var multiplier = Math.Sin((Math.PI * 2) * phase);
                var newAngle = startAngle + (RotationRange * multiplier);

                //SimRender.RotationY = (float)MathUtils.DegreeToRadian(newAngle);
            }
        }

        private Sim m_Sim;
        public Sim Sim
        {
            get { return m_Sim; }
            set
            {
                m_Sim = value;
                //SimRender.Sim = value;
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
                /** if (!ThreeDScene.IsInvalidated)
                {
                    batch.Pause();
                    SimScene.Draw(GameFacade.GraphicsDevice);
                    batch.Resume();
                }**/
            }
        }
    }
}
