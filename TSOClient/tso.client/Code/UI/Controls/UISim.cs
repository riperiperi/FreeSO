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
using TSO.Content;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSOClient.Code.Rendering;
using TSOClient.Code.Utils;
using ProtocolAbstractionLibraryD;
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
        private _3DScene Scene;
        private BasicCamera Camera;
        public AdultVitaboyModel Avatar;

        /** 45 degrees in either direction **/
        public float RotationRange = 45;
        public float RotationStartAngle = 180;
        public float RotationSpeed = new TimeSpan(0, 0, 10).Ticks;
        public bool AutoRotate = true;

        public float SimScale = 0.45f;
        public float ViewScale = 17.0f;

        private int m_CharacterID;

        protected Guid m_GUID;
        protected string m_Timestamp;
        protected string m_Name;
        protected string m_Sex;
        protected string m_Description;
        protected ulong m_HeadOutfitID;
        protected ulong m_BodyOutfitID;

        public Outfit Head
        {
            get
            {
                if (Avatar.Body == null)
                    return Content.Get().AvatarOutfits.Get(m_HeadOutfitID);

                return Avatar.Head;
            }
            set { Avatar.Head = value; }
        }

        public Outfit Body
        {
            get
            {
                if (Avatar.Body == null)
                    return Content.Get().AvatarOutfits.Get(m_BodyOutfitID);

                return Avatar.Body;
            }

            set { Avatar.Body = value; }
        }

        public Outfit Handgroup
        {
            get { return Avatar.Handgroup; }
            set { Avatar.Handgroup = value; }
        }

        /// <summary>
        /// The ID of the head's outfit. Used by the network protocol.
        /// </summary>
        public ulong HeadOutfitID
        {
            get { return m_HeadOutfitID; }
            set { m_HeadOutfitID = value; }
        }

        /// <summary>
        /// The ID of the body's Outfit. Used by the network protocol.
        /// </summary>
        public ulong BodyOutfitID
        {
            get { return m_BodyOutfitID; }
            set { m_BodyOutfitID = value; }
        }

        protected CityInfo m_City;

        protected bool m_CreatedThisSession = false;

        public float HeadXPos = 0.0f, HeadYPos = 0.0f;

        /// <summary>
        /// This Sim's skeleton.
        /// </summary>
        public Skeleton SimSkeleton
        {
            get
            {
                return Avatar.Skeleton;
            }
        }

        /// <summary>
        /// Received a server-generated GUID.
        /// </summary>
        /// <param name="GUID">The GUID to assign to this sim.</param>
        public void AssignGUID(string GUID)
        {
            m_GUID = new Guid(GUID);
        }

        /// <summary>
        /// A Sim's GUID, created by the client and stored in the DB.
        /// </summary>
        public Guid GUID
        {
            get { return m_GUID; }
        }

        /// <summary>
        /// The character's ID, as it exists in the DB.
        /// </summary>
        public int CharacterID
        {
            get { return m_CharacterID; }
            set { m_CharacterID = value; }
        }

        /// <summary>
        /// When was this character last cached by the client?
        /// </summary>
        public string Timestamp
        {
            get { return m_Timestamp; }
            set { m_Timestamp = value; }
        }

        /// <summary>
        /// The character's name, as it exists in the DB.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public string Sex
        {
            get { return m_Sex; }
            set { m_Sex = value; }
        }

        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        public CityInfo ResidingCity
        {
            get { return m_City; }
            set { m_City = value; }
        }

        /// <summary>
        /// Set to true when a CharacterCreate packet was
        /// received. If this is false, the character in
        /// the DB will NOT be updated with the city that
        /// the character resides in when receiving a 
        /// KeyRequest packet from a CityServer, saving 
        /// an expensive DB call.
        /// </summary>
        public bool CreatedThisSession
        {
            get { return m_CreatedThisSession; }
            set { m_CreatedThisSession = value; }
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

        public UISim(string GUID)
        {
            if (GUID != "")
                this.m_GUID = new Guid(GUID);
            UISimInit();
            GameFacade.Scenes.AddExternal(Scene);
        }

        public UISim(string GUID, bool AddScene)
        {
            if ((GUID != "") && (GUID != "\0"))
                this.m_GUID = new Guid(GUID);
            UISimInit();
            if (AddScene)
                GameFacade.Scenes.AddExternal(Scene);
        }

        public UISim(Guid GUID)
        {
            this.m_GUID = GUID;
            UISimInit();
            GameFacade.Scenes.AddExternal(Scene);
        }

        public UISim(Guid GUID, bool AddScene)
        {
            this.m_GUID = GUID;
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
            if (AutoRotate){
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
