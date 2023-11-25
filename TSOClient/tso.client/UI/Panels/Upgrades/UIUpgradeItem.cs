using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Content.Upgrades.Model.Runtime;
using FSO.SimAntics;
using FSO.SimAntics.Model.TSOPlatform;
using Microsoft.Xna.Framework;
using System;
using System.Text;

namespace FSO.Client.UI.Panels.Upgrades
{
    public class UIUpgradeItem : UICachedContainer
    {
        //In the order of 206_querypanelstrings.cst
        private static string[] MotiveKeys =
        {
            "hunger",
            "comfort",
            "hygiene",
            "bladder",
            "energy",
            "fun",
            "room",

            "cooking",
            "mechanical",
            "logic",
            "body",
            "creativity",
            "charisma",
            "study"
        };

        public UIImage Background; //may make this a button?
        public UILabel Title;
        public UILabel TitlePrice;
        public UILabel Description;
        public UILabel Ads;
        public UIImage UpgradedTick;
        public bool CanPurchase;

        public int Price;

        private UIMouseEventRef ClickHandler;

        private VMEntity Entity;
        private int Level;
        private RuntimeUpgradeLevel RuntimeLevel;

        public event Action<int> OnHoveredLevel;
        public event Action<int> OnClickedLevel;
        public float Highlight = 0f;
        public UILotControl LotParent;

        public UIUpgradeItem(UILotControl parent, VMEntity ent, int level)
        {
            LotParent = parent;
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;
            Entity = ent;
            Level = level;

            Background = new UIImage(ui.Get(((level % 2) == 0) ? "up_item_panel.png" : "up_item_panel_alt.png").Get(gd));
            Add(Background);

            Title = new UILabel();
            Title.Alignment = TextAlignment.Right | TextAlignment.Top;
            Title.CaptionStyle = TextStyle.Create(Color.White, 12, true);
            Title.Position = new Vector2(512, 12);
            Title.Size = Vector2.One;
            Add(Title);

            TitlePrice = new UILabel();
            TitlePrice.Position = new Microsoft.Xna.Framework.Vector2(512, 12);
            TitlePrice.CaptionStyle = TextStyle.Create(new Color(115, 255, 115), 12, true);
            TitlePrice.Alignment = TextAlignment.Right | TextAlignment.Top;
            TitlePrice.Size = Vector2.One;
            Add(TitlePrice);

            Description = new UILabel();
            Description.Position = new Vector2(512, 31);
            Description.CaptionStyle = TextStyle.Create(TextStyle.DefaultLabel.Color, 9, true);
            Description.Alignment = TextAlignment.Right | TextAlignment.Top;
            Description.Size = Vector2.One;
            Add(Description);

            Ads = new UILabel();
            Ads.Position = new Vector2(512, 47);
            Ads.CaptionStyle = TextStyle.Create(Color.White, 9, true);
            Ads.Alignment = TextAlignment.Right | TextAlignment.Top;
            Ads.Size = Vector2.One;
            Add(Ads);

            UpgradedTick = new UIImage(ui.Get("up_tick.png").Get(gd));
            UpgradedTick.Position = new Vector2(494, 12);
            Add(UpgradedTick);

            Render();
            Size = new Vector2(Background.Width, Background.Height);

            ClickHandler = ListenForMouse(new Rectangle(6, 6, (int)Background.Width - 12, (int)Background.Height - 12), MouseHandler);
        }

        private void MouseHandler(UIMouseEventType type, UpdateState state)
        {
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    OnHoveredLevel?.Invoke(Level);
                    break;
                case UIMouseEventType.MouseOut:
                    OnHoveredLevel?.Invoke(-1);
                    break;
                case UIMouseEventType.MouseDown:
                    Highlight = -0.15f;
                    break;
                case UIMouseEventType.MouseUp:
                    SetHighlight(false);
                    OnClickedLevel?.Invoke(Level);
                    break;
            }
        }

        public void UpdateCanPurchase()
        {
            CanPurchase = Price <= LotParent.Budget;
            TitlePrice.CaptionStyle.Color = CanPurchase ? new Color(115, 255, 115) : new Color(255, 153, 153);
        }

        public void SetActive(bool active)
        {

        }

        public void SetHighlight(bool highlight)
        {
            Highlight = highlight ? 0.15f : 0f;
        }

        public void UpdateMatrix()
        {
            var oldInval = Invalidated;
            CalculateMatrix();
            Invalidated = oldInval;
        }

        public override void Draw(UISpriteBatch batch)
        {
            var effect = LotView.WorldContent.SpriteEffect;
            _BlendColor = new Color(0f, 1f, 1f, Opacity);
            

            effect.CurrentTechnique = effect.Techniques["HSVEffect"];
            effect.Parameters["Highlight"].SetValue(Highlight);
            batch.SetEffect(effect);
            base.Draw(batch);

            batch.SetEffect();
            effect.Parameters["Highlight"].SetValue(0f);
        }

        private string RenderAds(string[] ads)
        {
            var result = new StringBuilder();
            var first = false;
            foreach (var ad in ads)
            {
                if (first)
                    first = true;
                else
                    result.Append(' ');
                var split = ad.Split(':');
                if (split.Length == 2)
                {
                    var ind = Array.IndexOf(MotiveKeys, split[0]);
                    if (ind == -1)
                        result.Append(ad);
                    else
                        result.Append(GameFacade.Strings.GetString("206", (ind+4).ToString(), new string[] { split[1] }));
                }
                else
                {
                    result.Append(ad);
                }
            }
            return result.ToString();
        }

        public void Render()
        {
            int currentUpgrade = (Entity.PlatformState as VMTSOObjectState)?.UpgradeLevel ?? 0;
            var isBought = !Entity.GhostImage;

            int ind = Level;
            var filename = Entity.Object.Resource.MainIff.Filename;
            var file = Content.Content.Get().Upgrades.GetRuntimeFile(filename);
            var guid = (Entity.MasterDefinition ?? Entity.Object.OBJ).GUID;
            var level = file?.GetLevel(guid, Level);
            if (level != null)
            {
                var name = level.Level.Name;
                var desc = level.Level.Description;
                int presetParse;
                if (int.TryParse(name, out presetParse))
                {
                    name = GameFacade.Strings.GetString("f124", (presetParse * 2 + 1).ToString());
                    desc = GameFacade.Strings.GetString("f124", (presetParse * 2 + 2).ToString());
                }
                Title.Caption = name;
                Description.Caption = desc;

                
                Price = Content.Content.Get().Upgrades.GetUpgradePrice(filename, guid, Level) ?? Entity.MultitileGroup.InitialPrice;
                var item = Content.Content.Get().WorldCatalog.GetItemByGUID(guid);

                if (Level == 0)
                {
                    if (item != null)
                        Price = (int)item.Value.Price;
                    else
                        Price = Entity.MultitileGroup.InitialPrice; //todo: catalog price?
                }
                //!level.Relative   TODO: add relative price recursively onto previous prices
                if (isBought)
                {
                    UpdateCanPurchase();
                    Price -= Entity.MultitileGroup.Price;
                    TitlePrice.Caption = "+$" + Price.ToString("##,#0");
                } else
                {
                    UpdateCanPurchase();
                    TitlePrice.Caption = "$" + Price.ToString("##,#0");
                }
                
                var priceWidth = TitlePrice.CaptionStyle.MeasureString(TitlePrice.Caption);

                UpgradedTick.Visible = currentUpgrade >= Level && isBought;
                TitlePrice.Visible = !UpgradedTick.Visible;
                if (UpgradedTick.Visible) {
                    priceWidth.X = 20;
                }

                Title.X = 512 - (priceWidth.X + 8);

                var ads = level.Ads;
                Ads.Caption = RenderAds(level.Ads);
            } else {

                Title.Caption = "Very Expensive";

                TitlePrice.Caption = "+$1,000,000";
                var priceWidth = TitlePrice.CaptionStyle.MeasureString(TitlePrice.Caption);

                Title.X = 512 - (priceWidth.X + 8);

                Description.Caption = "Equivalent to the best product in the catalog. Who says a metal chair can’t be comfy?";
                Ads.Caption = "Fun: 10, Hunger: 10, +Cooking";
            }
        }
    }
}
