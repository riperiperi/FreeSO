using FSO.Client.Controllers;
using FSO.Client.Rendering.City;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UITop10Pedestal : UIContainer
    {
        public bool AltColor;
        public bool CastLeft;
        public bool CastRight;

        private Texture2D Tile;
        private Texture2D TileFill;
        private Texture2D TileTop;
        private Texture2D TileBtm;

        public UISim Sim { get; set; }

        public Binding<Avatar> User { get; internal set; }
        public Binding<Lot> Property { get; internal set; }

        public string AvatarTooltip { get; set; } = "";
        public string LotTooltip { get; set; } = "";

        private LotThumbEntry ThumbLock;
        private bool ShowTooltip;

        //Mixing concerns here but binding avatar id is much nicer than lots of plumbing each time
        private IClientDataService DataService;

        private uint _AvatarId = 0;
        public uint AvatarId
        {
            get { return _AvatarId; }
            set
            {
                _AvatarId = value;
                if (value == uint.MaxValue || value == 0)
                {
                    GameThread.NextUpdate((x) =>
                    {
                        User.Value = null;
                    });
                    Sim.Visible = false;
                }
                else
                {
                    DataService.Get<Avatar>(_AvatarId).ContinueWith(x =>
                    {
                        if (x.Result == null) { return; }
                        User.Value = x.Result;
                    });
                    DataService.Request(Server.DataService.Model.MaskedStruct.SimPage_Main, _AvatarId);
                    Sim.Visible = true;
                    LotId = 0;
                }
            }
        }


        private uint _LotId = 0;
        public uint LotId
        {
            get { return _LotId; }
            set
            {
                var last = _LotId;
                _LotId = value;
                if (value == uint.MaxValue || value == 0)
                {
                    GameThread.NextUpdate((x) =>
                    {
                        Property.Value = null;
                    });
                    if (ThumbLock != null)
                    {
                        ThumbLock.Held--;
                        ThumbLock = null;
                    }
                }
                else
                {
                    if (value == last) return;
                    DataService.Get<Lot>(_LotId).ContinueWith(x =>
                    {
                        if (x.Result == null) { return; }
                        Property.Value = x.Result;
                    });
                    DataService.Request(Server.DataService.Model.MaskedStruct.PropertyPage_LotInfo, _LotId);
                    AvatarId = 0;
                    
                    if (ThumbLock != null) ThumbLock.Held--;
                    ThumbLock = FindController<CoreGameScreenController>().Terrain.LockLotThumb(value);
                }
            }
        }

        public float Height { get; set; }
        public bool Hovered;
        private bool LastMouseDown;
        private bool Clicked;

        public UITop10Pedestal()
        {
            DataService = FSOFacade.Kernel.Get<IClientDataService>();
            var ui = Content.Content.Get().CustomUI;

            TileFill = ui.Get("neighp_tilefill.png").Get(GameFacade.GraphicsDevice);
            TileTop = ui.Get("neighp_tiletop.png").Get(GameFacade.GraphicsDevice);
            TileBtm = ui.Get("neighp_tilebtm.png").Get(GameFacade.GraphicsDevice);

            SetGraphics(false);

            Sim = new UISim();
            Sim.Avatar.BodyOutfitId = 2611340115981;
            Sim.Avatar.HeadOutfitId = 5076651343885;
            Sim.Size = new Microsoft.Xna.Framework.Vector2(64, 80);
            Sim.AutoRotate = true;
            Sim.Position = new Vector2(-32, (-80) + 16);
            this.Add(Sim);

            User = new Binding<Avatar>()
                .WithBinding(this, "AvatarTooltip", "Avatar_Name")
                .WithBinding(this, "Sim.Avatar.HeadOutfitId", "Avatar_Appearance.AvatarAppearance_HeadOutfitID")
                .WithBinding(this, "Sim.Avatar.BodyOutfitId", "Avatar_Appearance.AvatarAppearance_BodyOutfitID")
                .WithBinding(this, "Sim.Avatar.Appearance", "Avatar_Appearance.AvatarAppearance_SkinTone", (x) => {
                    if (x == null) return Vitaboy.AppearanceType.Light;
                    return (Vitaboy.AppearanceType)((byte)(x));
                    });

            Property = new Binding<Lot>()
                .WithBinding(this, "LotTooltip", "Lot_Name");
        }

        public override void Removed()
        {
            User.Dispose();
            Property.Dispose();
            base.Removed();
        }


        public override void Update(UpdateState state)
        {
            Sim.Position = new Vector2(-32, (-80) + 14 - Height);
            Sim.TimeOffset = X*10000000;
            base.Update(state);

            if (!WillDraw()) return;

            if (Hovered)
            {
                if (!ShowTooltip && (AvatarId > 0 || LotId > 0))
                {
                    state.UIState.TooltipProperties.Show = true;
                    state.UIState.TooltipProperties.Color = Color.Black;
                    state.UIState.TooltipProperties.Opacity = 0;
                    state.UIState.TooltipProperties.Position = state.MouseState.Position.ToVector2();
                    state.UIState.Tooltip = (AvatarId > 0)?AvatarTooltip:LotTooltip;
                    ShowTooltip = true;
                }
                if (ShowTooltip)
                {
                    if (state.UIState.TooltipProperties.Opacity < 1) state.UIState.TooltipProperties.Opacity += 0.1f;
                    state.UIState.TooltipProperties.UpdateDead = false;
                }
                if ((state.MouseState.LeftButton == ButtonState.Pressed) && !LastMouseDown)
                {
                    if (LotId != 0)
                    {
                        FindController<CoreGameScreenController>().ShowLotPage(LotId);
                    }
                    else if (AvatarId != 0)
                    {
                        FindController<CoreGameScreenController>().ShowPersonPage(AvatarId);
                    }
                    Clicked = true;
                    FSO.HIT.HITVM.Get().PlaySoundEvent(Model.UISounds.Click);
                }
            } else if (ShowTooltip)
            {
                ShowTooltip = false;
            }

            if (state.MouseState.LeftButton != ButtonState.Pressed) Clicked = false;

            Hovered = false;

            //horiz check
            var mouse = Parent.GlobalPoint(state.MouseState.Position.ToVector2());
            if (Visible && mouse.X > X-32 && mouse.X < X+32)
            {
                if (mouse.Y > Y && mouse.Y < Y + 16)
                {
                    //bottom region. check diagonal
                    var diff = (mouse.Y - Y)*2;
                    if (mouse.X > (X - 32) + diff && mouse.X < (X + 32) - diff)
                        Hovered = true;
                }
                else if (mouse.Y < Y - Height && mouse.Y > Y - (Height + 16))
                {
                    //top region. check diagonal
                    var diff = ((Y-Height) - mouse.Y) * 2;
                    if (mouse.X > (X - 32) + diff && mouse.X < (X + 32) - diff)
                        Hovered = true;
                }
                else if (mouse.Y <= Y && mouse.Y >= Y-Height)
                {
                    //box
                    Hovered = true;
                }
            }

            if (Hovered) {
                foreach (var child in Parent.GetChildren())
                {
                    var ped = child as UITop10Pedestal;
                    if (ped != null && ped != this && ped.Hovered)
                    {
                        Hovered = false;
                    }
                }
            }

            LastMouseDown = state.MouseState.LeftButton == ButtonState.Pressed;
        }

        public void SetGraphics(bool big)
        {
            var ui = Content.Content.Get().CustomUI;
            Tile = ui.Get("neighp_tile.png").Get(GameFacade.GraphicsDevice);
        }

        public void SetPlace(int place)
        {
            var targH = 0;
            if (place > 0 && place < 4)
            {
                targH = (4 - place) * 10;
            }
            GameFacade.Screens.Tween.To(this, 0.5f, new Dictionary<string, float> { { "Height", targH } }, TweenQuad.EaseOut);
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            //draw the pedestal
            var tintCol = (AltColor) ? new Color(217, 245, 255) : Color.White;

            var halfW = Tile.Width / 2;
            var halfH = Tile.Height / 2;
            var halfV = new Vector2(-halfW, -halfH);
            var scale = new Vector2(Tile.Width / TileFill.Width);
            //shadow
            DrawLocalTexture(batch, TileFill, null, new Vector2(1, 1) + halfV, scale, Color.Black);

            //fill
            
            var fillCol = new Color(new Color(164, 188, 210).ToVector4() * tintCol.ToVector4());
            var shad = fillCol * 0.88f;
            shad.A = 255;
            DrawLocalTexture(batch, TileFill, null, halfV, scale, fillCol);
            DrawLocalTexture(batch, TileFill, new Rectangle(TileFill.Width/2, 0, TileFill.Width/2, TileFill.Height), new Vector2(0, -halfH), scale, shad);

            var pxWhite = TextureGenerator.GetPxWhite(batch.GraphicsDevice);
            DrawLocalTexture(batch, pxWhite, null, new Vector2(-halfW, -Height), new Vector2(halfW, Height + 0.5f), fillCol);
            DrawLocalTexture(batch, pxWhite, null, new Vector2(0, -Height), new Vector2(halfW, Height + 0.5f), shad);

            //tile
            DrawLocalTexture(batch, Tile, null, new Vector2(0, -Height) + halfV, Vector2.One, tintCol);

            //bottom and top
            var intensity = Math.Min(1f, Height / 10f);
            DrawLocalTexture(batch, TileBtm, null, new Vector2(0, 6) + halfV, scale, tintCol*intensity);
            DrawLocalTexture(batch, TileTop, null, new Vector2(0, 16-Height) + halfV, scale, tintCol * intensity);

            if (Hovered)
            {
                var hoverCol = (Clicked)?(Color.Black*0.2f):(Color.White * 0.3f);
                DrawLocalTexture(batch, TileFill, new Rectangle(0, 0, TileFill.Width, halfH), new Vector2(-halfW, -(halfH+Height)), scale, hoverCol);
                DrawLocalTexture(batch, TileFill, new Rectangle(0, halfH, TileFill.Width, halfH), new Vector2(-halfW, 0), scale, hoverCol);
                
                DrawLocalTexture(batch, pxWhite, null, new Vector2(-halfW, -Height), new Vector2(halfW*2, Height + 0.5f), hoverCol);
            }

            if (ThumbLock != null && ThumbLock.Loaded && ThumbLock.LotTexture != null)
            {
                var tex = ThumbLock.LotTexture;
                DrawLocalTexture(batch, tex, null, new Vector2(-halfW, -(halfW+Height)), new Vector2((halfW * 2f)/tex.Width));
            }

            base.Draw(batch);
        }
    }
}
