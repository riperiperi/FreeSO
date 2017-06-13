using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.IO;
using FSO.Content.Framework;
using FSO.Content.Model;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Panels
{
    public class UINeighborhoodSelectionPanel : UIContainer
    {

        public static NeighborhoodViewConfig[] Neighborhoods = new NeighborhoodViewConfig[]
        {
            new NeighborhoodViewConfig()
            {
                Graphic = "Nbhd\\NScreen.BMP",
                Scale = 1f,
                FullImageAnimations = new NeighborhoodImageAnim[] {new NeighborhoodImageAnim("Nbhd\\DiffN1-N2_8.bmp", "Nbhd\\DiffN1-N3_8.bmp", "Nbhd\\DiffN1-N4_8.bmp") }
            },
            new NeighborhoodViewConfig()
            {
                Graphic = "Downtown\\DScreen.bmp",
                Scale = 1f,
                FullImageAnimations = new NeighborhoodImageAnim[] {new NeighborhoodImageAnim("Downtown\\dscreen00.bmp", "Downtown\\dscreen01.bmp", "Downtown\\dscreen02.bmp", "Downtown\\dscreen03.bmp", "Downtown\\dscreen04.bmp") }
            },
            new NeighborhoodViewConfig()
            {
                Graphic = "VIsland\\visland.bmp",
                Scale = 1f,
                FullImageAnimations = new NeighborhoodImageAnim[] {
                    new NeighborhoodImageAnim("VIsland\\visland_waves001.bmp", "VIsland\\visland_waves002.bmp", "VIsland\\visland_waves003.bmp", "VIsland\\visland_waves004.bmp", "VIsland\\visland_waves005.bmp"),
                    new NeighborhoodImageAnim("VIsland\\visland_port.bmp"),
                    new NeighborhoodImageAnim("VIsland\\visland_trees.bmp"),
                } //TODO: nessie
            },
            new NeighborhoodViewConfig()
            {
                Graphic = "Community\\NScreen_unleashed.bmp",
                Scale = 2f,
                Pulsate = false,
                FullImageAnimations = new NeighborhoodImageAnim[] {new NeighborhoodImageAnim("Community\\NScreen_unleashed_waves001.bmp", "Community\\NScreen_unleashed_waves002.bmp", "Community\\NScreen_unleashed_waves003.bmp", "Community\\NScreen_unleashed_waves004.bmp", "Community\\NScreen_unleashed_waves005.bmp", "Community\\NScreen_unleashed_waves006.bmp") }
            },
            new NeighborhoodViewConfig()
            {
                Graphic = "Studiotown\\DScreen.bmp",
                Scale = 1f,
                FullImageAnimations = new NeighborhoodImageAnim[] {new NeighborhoodImageAnim("Studiotown\\DScreen_top_layer.bmp") } //TODO: cars
            },
            new NeighborhoodViewConfig(),
            new NeighborhoodViewConfig()
            {
                Graphic = "Magicland\\DScreen.bmp",
                Pulsate = false,
                Scale = 1f,
                FullImageAnimations = new NeighborhoodImageAnim[] {new NeighborhoodImageAnim(new Vector2(0, 62), "Magicland\\DScreen_waves1.bmp", "Magicland\\DScreen_waves2.bmp", "Magicland\\DScreen_waves3.bmp", "Magicland\\DScreen_waves4.bmp", "Magicland\\DScreen_waves5.bmp", "Magicland\\DScreen_waves6.bmp") } //todo: blimp
            },
        };

        public TS1Provider Provider;
        public event Action<int> OnHouseSelect;
        public UINeighborhoodSelectionPanel(ushort mode)
        {
            Provider = Content.Content.Get().TS1Global;
            PopulateScreen(mode);
            GameResized();
        }

        public override void GameResized()
        {
            base.GameResized();
            var scale = GlobalSettings.Default.GraphicsHeight / 600.0f;
            ScaleX = ScaleY = scale;

            X = (GlobalSettings.Default.GraphicsWidth - 800 * scale) / 2;
        }

        public void PopulateScreen(ushort mode)
        {
            var childClone = new List<UIElement>(Children);
            var config = Neighborhoods[mode - 1];
            foreach (var child in childClone) Remove(child);

            var bg = new UIImage(((ITextureRef)Provider.Get(config.Graphic)).Get(GameFacade.GraphicsDevice));
            Add(bg);
            bg.BlockInput();
            var locationIff = Content.Content.Get().Neighborhood.LotLocations;
            var locations = locationIff.Get<STR>(mode);
            if (locations == null) return;

            for (int i = 0; i < locations.Length; i++)
            {
                var loc = locations.GetString(i).Split(',');
                var button = new UINeighborhoodHouseButton(int.Parse(loc[0].TrimStart()), SelectHouse, config.Scale);
                button.Position = new Vector2(int.Parse(loc[1].TrimStart()), int.Parse(loc[2].TrimStart()));
                Add(button);
            }

            foreach (var layer in config.FullImageAnimations)
            {
                var lelem = new UINeighborhoodAnimationLayer(layer, config.Pulsate, config.FrameDuration);
                lelem.Position = layer.Position;
                Add(lelem);
            }
        }

        public void SelectHouse(int house)
        {
            OnHouseSelect?.Invoke(house);
        }
    }

    public class UINeighborhoodAnimationLayer : UIElement {

        public Texture2D[] Frames;
        public int FrameNum;
        public int SubFrame;
        public int FrameTime;
        private int TotalFrames;
        public NeighborhoodImageAnim Anim;

        public UINeighborhoodAnimationLayer(NeighborhoodImageAnim anim, bool pulsate, int frameTime)
        {
            var provider = Content.Content.Get().TS1Global;
            Frames = anim.Frames.Select(x=> ((ITextureRef)provider.Get(x)).Get(GameFacade.GraphicsDevice)).ToArray();
            SubFrame = frameTime;
            FrameTime = frameTime;
            TotalFrames = pulsate ? (Frames.Length * 2 - 2) : Frames.Length;
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (--SubFrame <= 0)
            {
                SubFrame = FrameTime;
                FrameNum++;
                if (TotalFrames != 0) FrameNum %= TotalFrames;
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            var realFrame = (FrameNum >= Frames.Length) ? ((Frames.Length - 2) - (FrameNum - Frames.Length)) : FrameNum;
            DrawLocalTexture(batch, Frames[Math.Max(0, realFrame)], Vector2.Zero);
        }
    }

    public class NeighborhoodViewConfig
    {
        public string Graphic;
        public float Scale;
        public NeighborhoodImageAnim[] FullImageAnimations = new NeighborhoodImageAnim[0];
        public int FrameDuration = 15;
        public bool Pulsate = true;
    }

    public class NeighborhoodImageAnim
    {
        public string[] Frames;
        public Vector2 Position;

        public NeighborhoodImageAnim(params string[] frames)
        {
            Frames = frames;
        }

        public NeighborhoodImageAnim(Vector2 position, params string[] frames) : this(frames)
        {
            Position = position;
        }
    }

    public class UINeighborhoodHouseButton : UIElement
    {
        private Texture2D HouseTex;
        private Texture2D HouseOpenTex;
        private float HouseScale;
        private bool Hovered;
        public float AlphaTime { get; set; }

        public UINeighborhoodHouseButton(int houseNumber, Action<int> selectionCallback, float scale)
        {
            var house = Content.Content.Get().Neighborhood.GetHouse(houseNumber);
            HouseTex = house.Get<BMP>(513).GetTexture(GameFacade.GraphicsDevice);
            ManualTextureMask(ref HouseTex, new Color(248, 0, 248, 255));
            HouseOpenTex = house.Get<BMP>(512).GetTexture(GameFacade.GraphicsDevice);
            ManualTextureMask(ref HouseOpenTex, new Color(248, 0, 248, 255));
            HouseScale = scale;

            var w = (int)(HouseTex.Width / HouseScale);
            var h = (int)(HouseTex.Height / HouseScale);
            var clickHandler =
                ListenForMouse(new Rectangle(w/-2, h/-2, w, h), (evt, state) =>
                {
                    switch (evt) {
                        case UIMouseEventType.MouseUp:
                            selectionCallback(houseNumber); break;
                        case UIMouseEventType.MouseOver:
                            GameFacade.Screens.Tween.To(this, 0.5f, new Dictionary<string, float>() { { "AlphaTime", 1f } });
                            HIT.HITVM.Get().PlaySoundEvent(Model.UISounds.Whoosh);
                            Hovered = true; break;
                        case UIMouseEventType.MouseOut:
                            GameFacade.Screens.Tween.To(this, 0.5f, new Dictionary<string, float>() { { "AlphaTime", 0f } });
                            Hovered = false; break;
                    }
                });
        }

        public override void Draw(UISpriteBatch batch)
        {
            DrawLocalTexture(batch, HouseTex, null, new Vector2(-HouseTex.Width, -HouseTex.Height)/(HouseScale*2), new Vector2(1f/HouseScale, 1f/HouseScale));
            if (AlphaTime > 0)
            {
                DrawLocalTexture(batch, HouseOpenTex, null, new Vector2(-HouseTex.Width, -HouseTex.Height) / (HouseScale * 2), new Vector2(1f / HouseScale, 1f / HouseScale), Color.White*AlphaTime);
            }
        }

        public override void Removed()
        {
            HouseTex?.Dispose();
            HouseOpenTex?.Dispose();
        }
    }
}
