using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.HIT;
using FSO.SimAntics;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Model.Commands;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Upgrades
{
    public class UIUpgradeList : UIContainer
    {
        public UIImage Background;
        public UIUpgradeThermo Thermometer;
        public UILabel TitleLabel;
        public List<UIUpgradeItem> Items = new List<UIUpgradeItem>();
        private List<UITweenInstance> ActiveTweens = new List<UITweenInstance>();
        public VMEntity ActiveEntity;
        public UILotControl LotParent;

        public int CurrentUpgrade;
        public int TotalUpgrades;
        public bool Shown;
        public float TargetHeight;
        public int HoveredLevel;

        private float _BackgroundHeight;
        public float BackgroundHeight
        {
            get { return _BackgroundHeight; }
            set {
                Background.Y = -(value - 26);
                Background.Height = value;
                TitleLabel.Y = Background.Y + 10;
                _BackgroundHeight = value;
            }
        }

        private float _FadePct;
        public float FadePct
        {
            get { return _FadePct; }
            set
            {
                TitleLabel.Opacity = Opacity * (1 - value);
                Background.Opacity = Opacity * (1 - value);
                Thermometer.Opacity = Opacity * (1 - value);

                _FadePct = value;
            }
        }

        private float _HidePct;
        public float HidePct
        {
            get { return _HidePct; }
            set {
                if (value == 1) Visible = false;
                else Visible = true;

                var offset = Math.Min(0.1f, 1 / (float)Items.Count);
                var mOff = 1 - offset;
                var minSize = 1 - Items.Count * offset;
                for (int i=0; i<Items.Count; i++)
                {
                    var item = Items[i];
                    var off = (Items.Count - i - 1) * offset;
                    var itemHide = Math.Max(Math.Min((value - off) / minSize, 1), 0);
                    item.X = (float)Math.Cos(itemHide * Math.PI * 4f) * 400f * (float)Math.Pow(itemHide, 3f);
                    item.Opacity = Opacity * ((1 - itemHide) * 2);
                    var vis = item.Opacity > 0;
                    if (vis != item.Visible) item.Visible = vis;
                    item.UpdateMatrix();
                }
                _HidePct = value;

            }
        }

        public UIUpgradeList(UILotControl parent)
        {
            LotParent = parent;
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            Background = new UIImage(ui.Get("up_background.png").Get(gd)).With9Slice(0, 0, 40, 90);
            Background.X = 191;
            Background.BlockInput();
            Add(Background);

            TitleLabel = new UILabel();
            TitleLabel.Caption = "Congratulations! This object is fully upgraded.";
            TitleLabel.CaptionStyle = TextStyle.Create(Color.White, 12, true);
            TitleLabel.Position = new Vector2(557, 16);
            TitleLabel.Size = new Vector2(1, 1);
            TitleLabel.Alignment = TextAlignment.Right | TextAlignment.Top;
            Add(TitleLabel);

            Thermometer = new UIUpgradeThermo();
            Thermometer.Position = new Vector2(549, -73);
            Thermometer.OnHoveredLevel += Hover;
            Thermometer.OnClickedLevel += Click;
            Add(Thermometer);

            BackgroundHeight = 110;
            HidePct = 1;
        }

        public void Hover(int level)
        {
            Thermometer.SetHighlightLevel(level);
            for (int i=0; i<Items.Count; i++)
            {
                Items[i].SetHighlight(i == level);
            }
        }

        public void Click(int level)
        {
            HITVM.Get().PlaySoundEvent(UISounds.Click);
            if (level == CurrentUpgrade || level < 0 || level >= Items.Count)
            {
                HITVM.Get().PlaySoundEvent(UISounds.Error);
                return;
            }
            if (ActiveEntity.GhostImage)
            {
                var item = Items[level];
                item.UpdateCanPurchase(); //make sure this is up to date.
                if (!item.CanPurchase)
                {
                    HITVM.Get().PlaySoundEvent(UISounds.Error);
                    return;
                }

                //object has not been bought yet
                //instantly switch the upgrade level. Notify the query panel that the level and price has changed.
                foreach (var obj in ActiveEntity.MultitileGroup.Objects)
                {
                    var state = obj.PlatformState as VMTSOObjectState;
                    if (state != null)
                    {
                        state.UpgradeLevel = (byte)level;
                    }
                }
                
                var price = item.Price;
                ActiveEntity.MultitileGroup.InitialPrice = price;
                // notify the querypanel that it needs to update
                (Parent.Parent as UIQueryPanel)?.ReloadEntityInfo(false);
                PopulateUpgrades();
            } else
            {
                //this upgrade will cost the user money immediately. display a dialog confirming the cost.
                if (level < CurrentUpgrade)
                {
                    HITVM.Get().PlaySoundEvent(UISounds.Error);
                    return;
                }
                var item = Items[level];
                item.UpdateCanPurchase(); //make sure this is up to date.
                if (!item.CanPurchase)
                {
                    HITVM.Get().PlaySoundEvent(UISounds.Error);
                    return;
                }
                var price = item.TitlePrice.Caption.TrimStart('+');
                UIAlert.YesNo(
                    GameFacade.Strings.GetString("f125", "10", new string[] { item.Title.Caption }),
                    GameFacade.Strings.GetString("f125", "11", new string[] { ActiveEntity.ToString(), item.Title.Caption, price }),
                    true,
                    (answer) =>
                    {
                        if (answer)
                        {
                            //send the command to the VM!
                            var vm = LotParent.vm;
                            if (vm != null)
                            {
                                HITVM.Get().PlaySoundEvent(UISounds.ObjectPlace);
                                vm.SendCommand(
                                    new VMNetUpgradeCmd()
                                    {
                                        ObjectPID = ActiveEntity.PersistID,
                                        TargetUpgradeLevel = (byte)level
                                    });
                                //register our waiting function for reloading
                                vm.OnGenericVMEvent += ReloadUpgrades;
                            }

                        }
                    });
            }
        }

        private void ReloadUpgrades(VMEventType type, object data)
        {
            if (type != VMEventType.TSOUpgraded) return;
            (Parent.Parent as UIQueryPanel)?.ReloadEntityInfo(false);
            PopulateUpgrades();
            var vm = LotParent.vm;

            if (vm != null)
            {
                vm.OnGenericVMEvent -= ReloadUpgrades;
            }
        }

        public void PopulateUpgrades()
        {
            foreach (var item in Items)
            {
                Remove(item);
            }
            Items.Clear();

            CurrentUpgrade = (ActiveEntity.PlatformState as VMTSOObjectState)?.UpgradeLevel ?? 0;
            TotalUpgrades = 0;
            var file = Content.Content.Get().Upgrades.GetRuntimeFile(ActiveEntity.Object.Resource.MainIff.Filename);
            var guid = (ActiveEntity.MasterDefinition ?? ActiveEntity.Object.OBJ).GUID;
            if (file != null)
            {
                var config = file.GetConfig(guid);
                if (config != null) TotalUpgrades = ((config.Limit ?? (file.Levels.Count-1)) - config.Level) + 1;
            }

            if (CurrentUpgrade >= TotalUpgrades) CurrentUpgrade = Math.Max(0, TotalUpgrades-1);

            for (var i=0; i<TotalUpgrades; i++)
            {
                var item = new UIUpgradeItem(LotParent, ActiveEntity, i);
                item.SetActive(CurrentUpgrade >= i);
                item.Y = -(112 + 70 * i);
                item.X = 0;
                item.OnHoveredLevel += Hover;
                item.OnClickedLevel += Click;
                Items.Add(item);
                Add(item);
            }
            Thermometer.SetTotalLevels(Items.Count);
            Thermometer.SetTargetFill(CurrentUpgrade);
            TargetHeight = Math.Max(130, 107 + 70 * Items.Count);

            SetTitle();
        }

        public void SetTitle()
        {
            string id;
            if (TotalUpgrades == 0) id = "12";
            else {
                //object not bought
                if (ActiveEntity.GhostImage)
                {
                    if (CurrentUpgrade == 0) id = "3";
                    else id = "5";
                }
                else //object bought
                {
                    if (CurrentUpgrade == TotalUpgrades - 1) id = "4";
                    else id = "2";
                }
            }

            if (id == "5")
                TitleLabel.Caption = GameFacade.Strings.GetString("f125", id, new string[] { Items[CurrentUpgrade].Title.Caption });
            else
                TitleLabel.Caption = GameFacade.Strings.GetString("f125", id);
        }

        public void FinishTweens()
        {
            foreach (var tween in ActiveTweens)
            {
                tween.Complete();
            }
            ActiveTweens.Clear();
        }

        public void StopTweens()
        {
            foreach (var tween in ActiveTweens)
            {
                tween.Stop();
            }
            ActiveTweens.Clear();
        }

        public void Show(VMEntity ent)
        {
            Shown = true;
            Visible = true;
            ActiveEntity = ent;
            PopulateUpgrades();

            var advLength = 0.8f + Items.Count * 0.1f;
            StopTweens();
            ActiveTweens.Add(
            GameFacade.Screens.Tween.To(this, 0.75f, new Dictionary<string, float>() { { "BackgroundHeight", TargetHeight }, { "FadePct", 0f } }, TweenQuad.EaseOut)
            );
            ActiveTweens.Add(
            GameFacade.Screens.Tween.To(this, advLength, new Dictionary<string, float>() { { "HidePct", 0f } }, TweenLinear.EaseNone)
            );
        }

        public void Hide()
        {
            Thermometer.SetTargetFill(0f);
            Shown = false;

            var advLength = 0.8f + Items.Count * 0.1f;
            StopTweens();
            ActiveTweens.Add(
            GameFacade.Screens.Tween.To(this, 0.75f, new Dictionary<string, float>() { { "BackgroundHeight", 130 }, { "FadePct", 1 } }, TweenQuad.EaseIn)
            );
            ActiveTweens.Add(
            GameFacade.Screens.Tween.To(this, advLength, new Dictionary<string, float>() { { "HidePct", 1f } }, TweenLinear.EaseNone)
            );
        }

        private float LastOpacity = 0;
        public override void Update(UpdateState state)
        {
            base.Update(state);

            if (Opacity < 1 || LastOpacity < 1)
            {
                if (Opacity == 0 && ActiveTweens.Count > 0)
                {
                    FinishTweens();
                }
                FadePct = FadePct; //update these to multiply in our opacity
                HidePct = HidePct;
            }
            LastOpacity = Opacity;

        }
    }
}
