/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Content;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FSO.Client.Rendering;
using FSO.Client.Utils;
using ProtocolAbstractionLibraryD;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework;
using FSO.Vitaboy;
using FSO.Common.Rendering.Framework.Camera;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// Renders a sim in the UI, this class just helps translate the UI world
    /// into the 3D world for sim rendering
    /// </summary>
    public class UISim : UIElement
    {
        private _3DScene Scene;
        private BasicCamera Camera;
        public AdultVitaboyModel Avatar { get; internal set; }

        /** 45 degrees in either direction **/
        public float RotationRange = 45;
        public float RotationStartAngle = 180;
        public float RotationSpeed = new TimeSpan(0, 0, 10).Ticks;
        public bool AutoRotate = true;

        public float SimScale = 0.45f;
        public float ViewScale = 17.0f;
        
        protected string m_Timestamp;
        public float HeadXPos = 0.0f, HeadYPos = 0.0f;
        

        /// <summary>
        /// When was this character last cached by the client?
        /// </summary>
        public string Timestamp
        {
            get { return m_Timestamp; }
            set { m_Timestamp = value; }
        }
        
        private void UISimInit()
        {
            Camera = new BasicCamera(GameFacade.GraphicsDevice, new Vector3(0.0f, 7.0f, -17.0f), Vector3.Zero, Vector3.Up);
            Scene = new _3DScene(GameFacade.Game.GraphicsDevice, Camera);
            Scene.ID = "UISim";

            GameFacade.Game.GraphicsDevice.DeviceReset += new EventHandler<EventArgs>(GraphicsDevice_DeviceReset);

            Avatar = new AdultVitaboyModel();
            Avatar.Scene = Scene;
            Avatar.Scale = new Vector3(0.45f);
            Scene.Add(Avatar);

        }

        public UISim() : this(true)
        {
        }

        public UISim(bool AddScene)
        {
            UISimInit();
            if (AddScene)
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
            if (AutoRotate)
            {
                var startAngle = RotationStartAngle;
                var time = state.Time.TotalGameTime.Ticks;
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
