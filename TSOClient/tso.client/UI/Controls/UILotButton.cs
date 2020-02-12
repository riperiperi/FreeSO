using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.Model;
using FSO.Content.Model;
using FSO.Client.Controllers;
using FSO.Common.DataService;
using Ninject;
using System.IO;
using FSO.Files;
using FSO.Common.Serialization.Primitives;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Screens;
using FSO.Common.Rendering.Framework.IO;
using FSO.HIT;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client.Rendering.City;
using FSO.Common;

namespace FSO.Client.UI.Controls
{
    public class UILotButton : UIContainer
    {
        public Binding<Lot> Target { get; internal set; }
        private UITooltipHandler m_TooltipHandler;
        public Texture2D ThumbImg { get; set; }
        public Texture2D BgImg { get; set; }
        public Texture2D HoverImg { get; set; }
        public UILabel NameLabel { get; set; }

        //Mixing concerns here but binding avatar id is much nicer than lots of plumbing each time
        private IClientDataService DataService;
        private UIMouseEventRef ClickHandler;
        private LotThumbEntry Thumb;

        public UILotButton()
        {
            DataService = FSOFacade.Kernel.Get<IClientDataService>();

            NameLabel = new UILabel();
            NameLabel.Y = 52;
            NameLabel.X = 2;
            NameLabel.Alignment = TextAlignment.Center;
            NameLabel.Size = new Vector2(76, 40);
            NameLabel.CaptionStyle = NameLabel.CaptionStyle.Clone();
            NameLabel.CaptionStyle.Size = 9;
            NameLabel.Wrapped = true;
            Add(NameLabel);

            Target = new Binding<Lot>()
                .WithBinding(this, "NameLabel.Caption", "Lot_Name")
                .WithBinding(this, "BgImg", "Lot_IsOnline", x =>
                {
                    var online = (bool)x;
                    return GetTexture((ulong)((online) ? 0xC000000002 : 0xC200000002));
                })
                .WithBinding(this, "HoverImg", "Lot_IsOnline", x =>
                {
                    var online = (bool)x;
                    return GetTexture((ulong)((online) ? 0xC100000002 : 0xC300000002));
                });
            m_TooltipHandler = UIUtils.GiveTooltip(this);

            ClickHandler =
                ListenForMouse(new Rectangle(0, 0, 80, 50), new UIMouseEvent(OnMouseEvent));
        }

        private bool m_isOver;
        private bool m_isDown;

        public void Dispose()
        {
            if (ThumbImg != null) ThumbImg.Dispose();
        }

        private void OnMouseEvent(UIMouseEventType type, UpdateState state)
        {
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

        public override void Update(UpdateState state)
        {
            base.Update(state);
            var terrain = ((CoreGameScreen)GameFacade.Screens.CurrentUIScreen).CityRenderer;
            if (Is3D || terrain.Camera.CenterCam != null) UpdatePosition3D();
        }

        public void UpdatePosition3D()
        {
            var xp = (int)_LotId >> 16;
            var yp = (int)_LotId & 0xFFFF;

            var terrain = ((CoreGameScreen)GameFacade.Screens.CurrentUIScreen).CityRenderer;
            var pos = terrain.transformSpr4(new Vector3(xp+0.5f, terrain.InterpElevationAt(new Vector2(xp + 0.5f, yp + 0.5f))+2f, yp+0.5f));
        
            Position = new Vector2(pos.X-40, pos.Y-110) / FSOEnvironment.DPIScaleFactor;
            ZOrder = pos.Z;
            Visible = (pos.Z < 0);
            //AvoidOther();
        }

        public float ZOrder;

        private bool Is3D
        {
            get
            {
                return FSOEnvironment.Enable3D;
            }
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
                    Target.Value = x.Result;
                 });
                DataService.Request(Server.DataService.Model.MaskedStruct.PropertyPage_LotInfo, _LotId);

                if (!Is3D)
                {
                    var xp = (int)value >> 16;
                    var yp = (int)value & 0xFFFF;

                    var terrain = ((CoreGameScreen)GameFacade.Screens.CurrentUIScreen).CityRenderer;
                    var pos = (terrain.GetFar2DFromTile(xp, yp) + terrain.GetFar2DFromTile(xp + 1, yp + 1)) / 2;

                    Position = pos - new Vector2(40, 110);
                    AvoidOther();
                }
                Thumb = FindController<CoreGameScreenController>().Terrain.LockLotThumb(value);
            }
        }

        private void OnButtonClick()
        {
            FindController<CoreGameScreenController>().ShowLotPage(_LotId);
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, 80, 100);
        }

        public void AvoidOther()
        {
            var w = GameFacade.Screens.CurrentUIScreen.ScreenWidth / 80;
            var h = GameFacade.Screens.CurrentUIScreen.ScreenHeight / 100;
            var arry = new bool[w, h];
            var kids = Parent.GetChildren();
            foreach (var child in kids)
            {
                if (child != this && child is UILotButton)
                {
                    var xp2 = (int)child.Position.X / 80;
                    var yp2 = (int)child.Position.Y / 100;
                    if (xp2 < w && xp2 >= 0 && yp2 < h && yp2 >= 0) arry[xp2, yp2] = true;
                }
            }
            var xp = (int)Math.Round(Position.X / 80);
            var yp = (int)Math.Round(Position.Y / 100);
            if (xp < w && yp < h && xp >= 0 && yp >= 0)
            {
                if (!arry[xp, yp])
                {
                    //we're free
                    Position = new Vector2((int)Math.Round(Position.X / 80) * 80, (int)Math.Round(Position.Y / 100) * 100);
                    return;
                }

            }

            //attempt search
            var spread = new Queue<Point>();
            var start = new Point(Math.Min(Math.Max(0, xp), w-1), Math.Min(Math.Max(0, yp), h-1));
            var covered = new bool[w, h];
            covered[start.X, start.Y] = true;
            spread.Enqueue(start);

            while (spread.Count > 0)
            {
                var item = spread.Dequeue();
                if (item.X+1 < w && !covered[item.X+1, item.Y])
                {
                    covered[item.X + 1, item.Y] = true;
                    if (!arry[item.X + 1, item.Y])
                    {//we found it
                        Position = new Vector2((item.X + 1) * 80, item.Y * 100);
                        return;
                    }
                    spread.Enqueue(new Point(item.X + 1, item.Y));
                }
                if (item.Y - 1 >= 0 && !covered[item.X, item.Y - 1])
                {
                    covered[item.X, item.Y - 1] = true;
                    if (!arry[item.X, item.Y - 1])
                    {//we found it
                        Position = new Vector2((item.X) * 80, (item.Y - 1) * 100);
                        return;
                    }
                    spread.Enqueue(new Point(item.X, item.Y - 1));
                }

                if (item.X - 1 >= 0 && !covered[item.X - 1, item.Y])
                {
                    covered[item.X - 1, item.Y] = true;
                    if (!arry[item.X - 1, item.Y])
                    {//we found it
                        Position = new Vector2((item.X - 1) * 80, item.Y * 100);
                        return;
                    }

                    spread.Enqueue(new Point(item.X - 1, item.Y));
                }
                if (item.Y + 1 < h && !covered[item.X, item.Y + 1])
                {
                    covered[item.X, item.Y + 1] = true;
                    if (!arry[item.X, item.Y + 1])
                    {//we found it
                        Position = new Vector2((item.X) * 80, (item.Y + 1) * 100);
                        return;
                    }

                    spread.Enqueue(new Point(item.X, item.Y + 1));
                }
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible || Thumb == null) return;
            var ThumbImg = Thumb.LotTexture;
            if (ThumbImg != null && BgImg != null && HoverImg != null)
            {
                var terrain = ((CoreGameScreen)GameFacade.Screens.CurrentUIScreen).CityRenderer;
                var Size = new Vector2(80, 50);
                Vector2 startVec = new Vector2(40, 25) + Position, end = UITerrainHighlight.GetEndpointFromLotId(terrain, startVec, (int)LotId);
                Vector2 start = end;

                // position line around border
                float threshold = 5f;
                if (start.X > Position.X + Size.X)
                    start.X = Position.X + Size.X - threshold;
                else if (start.X < Position.X)
                    start.X = Position.X + threshold;
                if (start.Y > Position.Y + Size.Y)
                    start.Y = Position.Y + Size.Y - threshold;
                else if (start.Y < Position.Y)
                    start.Y = Position.Y + threshold;
                if (Math.Abs(start.X - end.X) < 30)
                    start.X = Size.X / 2 + Position.X;
                if (Math.Abs(start.Y - end.Y) < 30)
                    start.Y = Size.Y / 2 + Position.Y;

                UITerrainHighlight.DrawArrow(batch, terrain,
                    start * FSOEnvironment.DPIScaleFactor, (int)LotId, Target.Value.Lot_IsOnline ? default : new Color(80,80,80));
                DrawLocalTexture(batch, (m_isOver && !m_isDown) ? HoverImg : BgImg, new Vector2());

                var scale = new Vector2(0.25f, 0.25f);
                DrawLocalTexture(batch, ThumbImg, null, new Vector2(40, 25) - new Vector2(32, 32), scale);
                var px = TextureGenerator.GetPxWhite(batch.GraphicsDevice);
                DrawLocalTexture(batch, px, null, new Vector2(0, 50), new Vector2(80, 16 * NameLabel.NumLines + 7), Color.Black * 0.6f);

                base.Draw(batch);
            }
        }
    }
}
