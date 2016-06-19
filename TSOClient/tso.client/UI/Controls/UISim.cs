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
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework;
using FSO.Vitaboy;
using FSO.Common.Rendering.Framework.Camera;
using FSO.LotView.Utils;
using FSO.LotView;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// Renders a sim in the UI, this class just helps translate the UI world
    /// into the 3D world for sim rendering
    /// </summary>
    public class UISim : UIElement
    {
        private _3DTargetScene Scene;
        private WorldCamera Camera;
        public AdultVitaboyModel Avatar { get; internal set; }

        /** 45 degrees in either direction **/
        public float RotationRange = 45;
        public float RotationStartAngle = 45;
        public float RotationSpeed = new TimeSpan(0, 0, 10).Ticks;
        public bool AutoRotate = true;
        
        protected string m_Timestamp;
        public float HeadXPos = 0.0f, HeadYPos = 0.0f;

        private WorldZoom Zoom = WorldZoom.Near;

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
            Camera = new WorldCamera(GameFacade.GraphicsDevice);
            Camera.Zoom = Zoom;
            Camera.CenterTile = new Vector2(-1, -1);
            Scene = new _3DTargetScene(GameFacade.Game.GraphicsDevice, Camera, new Point(140, 200), (GlobalSettings.Default.AntiAlias)?8:0);
            Scene.ID = "UISim";

            GameFacade.Game.GraphicsDevice.DeviceReset += new EventHandler<EventArgs>(GraphicsDevice_DeviceReset);

            Avatar = new AdultVitaboyModel();
            Avatar.Scene = Scene;
            
            Scene.Add(Avatar);
        }

        public void SetZoom(WorldZoom zoom)
        {
            Zoom = zoom;
            if (Camera != null) Camera.Zoom = zoom;
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

        public override void PreDraw(UISpriteBatch batch)
        {
            if (!UISpriteBatch.Invalidated)
            {
                if (!_3DScene.IsInvalidated)
                {
                    batch.Pause();
                    Scene.Draw(GameFacade.GraphicsDevice);
                    batch.Resume();
                    DrawLocalTexture(batch, Scene.Target, new Vector2());
                }
            }
            base.PreDraw(batch);
        }

        public override void Draw(UISpriteBatch batch)
        {
            DrawLocalTexture(batch, Scene.Target, new Vector2());
        }
    }
}
