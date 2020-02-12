using FSO.Client.Controllers;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Screens;
using FSO.Common;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.HIT;
using FSO.Server.DataService.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FSO.Client.GameContent.FileIDs;

namespace FSO.Client.UI.Controls
{
     public class UIMapWaypoint : UIElement
    {
        private readonly int SizeW = 55, SizeH = 55;
        public enum UIMapWaypointStyle
        {
            YouAreHere,
            YourHouseHere
        }

        private bool attemptRedraw = false;
        private int oldZoomLevel = 5;

        private float completion = 0;
        private Vector2 animOldLoc, animNewLoc;

        private Binding<Avatar> myAvatar;
        private Binding<Lot> myLot;

        public float ZOrder;
        protected bool Is3D
        {
            get
            {
                return FSOEnvironment.Enable3D;
            }
        }

        public bool ForceHide
        {
            get;set;
        }

        private uint _LotId;
        public uint LotId
        {
            get { return _LotId; }
            set
            {
                _LotId = value;
                DataService.Get<Lot>(_LotId).ContinueWith(x =>
                {
                    if (x.Result == null) { return; }
                    myLot.Value = x.Result;
                });
                DataService.Request(Server.DataService.Model.MaskedStruct.PropertyPage_LotInfo, _LotId);
                reposition();
            }
        }

        private Texture2D BgImg { get; set; }
        public UIMapWaypointStyle Style;

        protected IClientDataService DataService;
        private readonly UIMouseEventRef ClickHandler;

        public UIMapWaypoint(UIMapWaypointStyle style)
        {
            DataService = FSOFacade.Kernel.Get<IClientDataService>();
            Style = style;
            var Network = FSOFacade.Kernel.Get<Network.Network>();
            myAvatar = new Binding<Avatar>();
            myLot = new Binding<Lot>();
            switch (style)
            {
                case UIMapWaypointStyle.YouAreHere: // pinkish backgound
                    BgImg = GetTexture((ulong)UIFileIDs.youarehereback);
                    break;
                case UIMapWaypointStyle.YourHouseHere:
                    BgImg = GetTexture((ulong)UIFileIDs.yourhousehereback);
                    DataService.Get<Avatar>(Network.MyCharacter).ContinueWith(x =>
                    {
                        myAvatar.Value = x.Result;
                        if (x.Result != null && x.Result.Avatar_LotGridXY != 0 && (myLot.Value == null || myLot.Value.Lot_Location_Packed != x.Result.Avatar_LotGridXY))
                        {
                            DataService.Request(MaskedStruct.AdmitInfo_Lot, x.Result.Avatar_LotGridXY).ContinueWith(y =>
                            {
                                myLot.Value = (Lot)y.Result;
                            });
                        }
                    });
                    break;
            }
            ClickHandler =
                ListenForMouse(new Rectangle(0, 0, SizeW, SizeH), new UIMouseEvent(OnMouseEvent));
        }

        private bool m_isOver;
        private bool m_isDown;

        public void Dispose()
        {
        }

        private void OnMouseEvent(UIMouseEventType type, UpdateState state)
        {
            if (!Visible) return;
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    m_isOver = true;
                    break;

                case UIMouseEventType.MouseOut:
                    m_isOver = false;
                    break;

                case UIMouseEventType.MouseDown:
                    m_isDown = true;
                    break;

                case UIMouseEventType.MouseUp:
                    if (m_isDown)
                    {
                        OnButtonClick();
                        HITVM.Get().PlaySoundEvent(UISounds.Click);
                    }
                    m_isDown = false;
                    break;
            }
        }

        private void Avoid()
        {
            //uses a priority system where YouAreHere takes priority over YourHouseHere
            var kids = Parent.GetChildren();
            UIMapWaypoint other = null;
            foreach (var child in kids)
            {
                if (child != this && (child is UIMapWaypoint))
                {
                    other = child as UIMapWaypoint;
                }
            }
            if (other == null || other.Visible == false)
                return;
            var dirVector = animNewLoc - animOldLoc; 
            if (dirVector.X < 30)
                dirVector.X = 30; // guaranteed 15px border to check if other map waypoints are around
            if (dirVector.Y < 30)
                dirVector.Y = 30;
            if (other.GetBounds().Intersects(new Rectangle((int)(Position.X - (dirVector.X/2)), (int)(Position.Y - (dirVector.Y/2)), (int)(SizeW + dirVector.X), (int)(SizeH + dirVector.Y))))
            {
                if (other.Style == UIMapWaypointStyle.YouAreHere)
                {                    
                    Position = new Vector2(other.Position.X - 10 - SizeW, animNewLoc.Y); // try left
                    if (Position.X < 10)
                        Position = new Vector2(other.Position.X + 10 + SizeW, animNewLoc.Y); // fallback on right
                    completion = 1; // make sure the waypoint cannot animate back to its desired position!
                }
            }
        }

        private void ensureOnScreen()
        {
            var gamescreen = ((CoreGameScreen)GameFacade.Screens.CurrentUIScreen);
            if (animNewLoc.X < 0) {
                animNewLoc.X = 10;
                completion = 0;
            }
            if (animNewLoc.X + SizeW > gamescreen.GetBounds().Width)
            {
                animNewLoc.X = gamescreen.GetBounds().Width - SizeW - 10;
                completion = 0;
            }
            if (animNewLoc.Y < 0)
            { 
                animNewLoc.Y = 10;
                completion = 0;
            }
            if (animNewLoc.Y + SizeH > gamescreen.GetBounds().Height)
            {
                animNewLoc.Y = gamescreen.GetBounds().Height - SizeH - 10;
                completion = 0;
            }
        }

        private Vector2 getDesiredLocation()
        {
            var xp = (int)_LotId >> 16;
            var yp = (int)_LotId & 0xFFFF;

            var terrain = ((CoreGameScreen)GameFacade.Screens.CurrentUIScreen).CityRenderer;
            return ((terrain.GetFar2DFromTile(xp, yp) + terrain.GetFar2DFromTile(xp + 1, yp + 1)) / 2) - new Vector2(SizeW / 2,150 + SizeH);
        }

        private void reposition()
        {
            if (!Is3D)
            {
                var pos = getDesiredLocation();
                if (double.IsNaN(pos.X) || double.IsNaN(pos.Y))
                {
                    attemptRedraw = true;
                    return;
                }
                attemptRedraw = false;
                Position = pos - new Vector2(SizeW/2, 150 - SizeH); // this is on purpose: makes the marker sprout up to alert the user
            }
        }       

        protected void OnButtonClick()
        {
            FindController<CoreGameScreenController>().ShowLotPage(_LotId);
        }       

        public override Rectangle GetBounds()
        {
            return new Rectangle((int)Position.X,(int)Position.Y, SizeW, SizeH);
        }  

        public void update3DZindex()
        {
            var xp = (int)_LotId >> 16;
            var yp = (int)_LotId & 0xFFFF;

            var terrain = ((CoreGameScreen)GameFacade.Screens.CurrentUIScreen).CityRenderer;
            var pos = terrain.transformSpr4(new Vector3(xp+0.5f, terrain.InterpElevationAt(new Vector2(xp + 0.5f, yp + 0.5f))+2f, yp+0.5f));

            ZOrder = pos.Z;
        }

        public override void Update(UpdateState state)
        {                        
            base.Update(state);
            uint lotId = 0;
            var gamescreen = ((CoreGameScreen)GameFacade.Screens.CurrentUIScreen);            
            ForceHide = gamescreen.ZoomLevel <= 3;
            if (ForceHide)
            {
                Visible = false;
                return;
            }
            switch (Style) {
                case UIMapWaypointStyle.YouAreHere:
                    lotId = FindController<CoreGameScreenController>().GetCurrentLotID();                    
                    break;
                case UIMapWaypointStyle.YourHouseHere:
                    lotId = myLot.Value?.Id ?? 0;
                    break;
            }
            Visible = lotId != 0; 
            if (!Visible)
                return;
            if (LotId != lotId)
                LotId = lotId;
            if (attemptRedraw) // forces the position to updated
                reposition();
            var desired = getDesiredLocation(); // refreshes the desired position of the marker
            if (desired != animNewLoc)
            { 
                animOldLoc = Position;
                animNewLoc = desired;
                completion = 0;
            }            
            ensureOnScreen();
            Avoid();
            if (completion < 1) // animate if needed
            {                
                completion = 1f;
                Position = animNewLoc; //interpolation: Vector2.Lerp(animOldLoc, animNewLoc, completion);
            }
            if (Is3D)
                update3DZindex();
            oldZoomLevel = gamescreen.ZoomLevel;
        }              

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            if (BgImg != null)
            {
                var terrain = ((CoreGameScreen)GameFacade.Screens.CurrentUIScreen).CityRenderer;
                Vector2 startVec = new Vector2(60 / 2, 60 / 2) + Position, end = UITerrainHighlight.GetEndpointFromLotId(terrain, startVec, (int)LotId);
                Vector2 start = end;

                //position line around border all pretty like
                float threshold = 5f;
                if (start.X > Position.X + SizeW)
                    start.X = Position.X + SizeW - threshold;
                else if (start.X < Position.X)
                    start.X = Position.X + threshold;
                if (start.Y > Position.Y + SizeH)
                    start.Y = Position.Y + SizeH - threshold;
                else if (start.Y < Position.Y)
                    start.Y = Position.Y + threshold;
                if (Math.Abs(start.X - end.X) < 30)
                    start.X = SizeW / 2 + Position.X;
                if (Math.Abs(start.Y - end.Y) < 30)
                    start.Y = SizeH / 2 + Position.Y;

                UITerrainHighlight.DrawArrow(batch, terrain, 
                    start * FSOEnvironment.DPIScaleFactor, (int)LotId, 
                    (Style == UIMapWaypointStyle.YourHouseHere) ? new Color(70, 185, 142) : new Color(129,103,152));
                DrawLocalTexture(batch, BgImg, null, new Vector2(), new Vector2(1), Color.White * (m_isOver ? .75f : 1f));
            }
        }
    }
}
