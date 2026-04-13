using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.UI.Controls;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveGameplayScale : UIDialog
    {
        public UISlider SkillSlider;
        public UILabel SkillDisplay;

        public UISlider PayoutSlider;
        public UILabel PayoutDisplay;

        public UISlider PenaltySlider;
        public UILabel PenaltyDisplay;

        public UIButton HelpButton;
        public UIButton ResetButton;

        private EventConfig Events;
        private bool IsChanged;

        public UIArchiveGameplayScale(ArchiveConfiguration config) : base(UIDialogStyle.OK, true)
        {
            Caption = "Skill/Money Cheats";
            var vbox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Center };

            config.LoadEvents();

            Events = config.Events ?? new EventConfig() { catalog = [], modifiers = [] };

            TextStyle style = TextStyle.DefaultLabel.Clone();

            style.Shadow = true;
            style.Color = Color.White;

            UILabel desc;

            vbox.Add(desc = new UILabel()
            {
                Caption = "Increase the speed of skilling or the payout of money objects.\nSome objects may not correctly report multiplied payouts - keep an eye on your balance.",
                Wrapped = true
            });

            desc.Size = new Vector2(320, 90);

            var skillBox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Left };

            skillBox.Add(new UILabel()
            {
                Caption = "Skill speed: "
            });

            skillBox.Add(SkillSlider = new UISlider()
            {
                Orientation = 0,
                Texture = GetTexture(0x42500000001),
                Size = new Vector2(250, 10),
                MinValue = 1,
                MaxValue = 25,
                AllowDecimals = true,
                Value = Events.skillSpeed ?? 1,
            });

            skillBox.Add(SkillDisplay = new UILabel()
            {
                Size = new Vector2(250, 10),
                Alignment = TextAlignment.Center,
                CaptionStyle = style
            });

            vbox.Add(skillBox);

            vbox.Add(new UISpacer(10));

            var payoutBox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Left };

            payoutBox.Add(new UILabel()
            {
                Caption = "Payout multiplier: "
            });

            payoutBox.Add(PayoutSlider = new UISlider()
            {
                Orientation = 0,
                Texture = GetTexture(0x42500000001),
                Size = new Vector2(250, 10),
                MinValue = 1,
                MaxValue = 10,
                AllowDecimals = true,
                Value = Events.payoutScale ?? 1
            });

            payoutBox.Add(PayoutDisplay = new UILabel()
            {
                Size = new Vector2(250, 10),
                Alignment = TextAlignment.Center,
                CaptionStyle = style
            });

            vbox.Add(payoutBox);

            vbox.Add(new UISpacer(10));

            var penaltyBox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Left };

            penaltyBox.Add(new UILabel()
            {
                Caption = "Singleplayer Penalty: "
            });

            penaltyBox.Add(PenaltySlider = new UISlider()
            {
                Orientation = 0,
                Texture = GetTexture(0x42500000001),
                Size = new Vector2(250, 10),
                MinValue = 0,
                MaxValue = 1,
                AllowDecimals = true,
                Value = Events.singleplayerPenalty ?? 1
            });

            penaltyBox.Add(PenaltyDisplay = new UILabel()
            {
                Size = new Vector2(250, 10),
                Alignment = TextAlignment.Center,
                CaptionStyle = style
            });

            vbox.Add(penaltyBox);

            vbox.Add(new UISpacer(10));

            var buttonsBox = new UIHBoxContainer();

            buttonsBox.Add(HelpButton = new UIButton()
            {
                Caption = GameFacade.Strings.GetString("f128", "127")
            });

            buttonsBox.Add(ResetButton = new UIButton()
            {
                Caption = GameFacade.Strings.GetString("f128", "128")
            });

            vbox.Add(buttonsBox);

            Add(vbox);

            HelpButton.OnButtonClick += HelpButton_OnButtonClick;
            ResetButton.OnButtonClick += ResetButton_OnButtonClick;

            SkillSlider.OnChange += SkillSlider_OnChange;
            PayoutSlider.OnChange += PayoutSlider_OnChange;
            PenaltySlider.OnChange += PenaltySlider_OnChange;

            UpdateDisplay(SkillDisplay, SkillSlider);
            UpdateDisplay(PayoutDisplay, PayoutSlider);
            UpdateDisplay(PenaltyDisplay, PenaltySlider, true);

            vbox.AutoSize();
            vbox.Position = new Vector2(20, 35);

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 70);

            OKButton.OnButtonClick += (elem) =>
            {
                if (IsChanged)
                {
                    config.Events = Events;

                    config.SaveEvents();
                }

                UIScreen.RemoveDialog(this);
            };
        }

        private void ResetButton_OnButtonClick(UIElement button)
        {
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = GameFacade.Strings.GetString("f128", "128"),
                Message = GameFacade.Strings.GetString("f128", "129"),
                Buttons = [
                    new UIAlertButton(UIAlertButtonType.Yes, (btn) => { Reset(ref Events, true); UpdateAll(); UIScreen.RemoveDialog(alert); }, GameFacade.Strings.GetString("f128", "130")),
                    new UIAlertButton(UIAlertButtonType.No, (btn) => { Reset(ref Events, false); UpdateAll(); UIScreen.RemoveDialog(alert); }, GameFacade.Strings.GetString("f128", "131"))
                ]
            }, true);
        }

        private static void Reset(ref EventConfig events, bool tso)
        {
            events.skillSpeed = tso ? null : 5;
            events.payoutScale = tso ? null : 5;
            events.singleplayerPenalty = tso ? null : 0;
        }

        private void UpdateAll()
        {
            SkillSlider.Value = Events.skillSpeed ?? 1;
            PayoutSlider.Value = Events.payoutScale ?? 1;
            PenaltySlider.Value = Events.singleplayerPenalty ?? 1;

            UpdateDisplay(SkillDisplay, SkillSlider);
            UpdateDisplay(PayoutDisplay, PayoutSlider);
            UpdateDisplay(PenaltyDisplay, PenaltySlider, true);
        }

        private void HelpButton_OnButtonClick(UIElement button)
        {
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = GameFacade.Strings.GetString("f128", "127"),
                Message = GameFacade.Strings.GetString("f128", "126"),
                Width = 600,
                Buttons = [
                    new UIAlertButton(UIAlertButtonType.OK, (btn) => { UIScreen.RemoveDialog(alert); }),
                ]
            }, true);
        }

        private void UpdateDisplay(UILabel display, UISlider slider, bool percent = false)
        {
            var value = slider.Value;

            display.Caption = percent ? $"{(value * 100).ToString("0.00")}%" : $"{value.ToString("0.00")}x";
        }

        private void PayoutSlider_OnChange(UIElement element)
        {
            IsChanged = true;

            Events.payoutScale = PayoutSlider.Value;
            UpdateDisplay(PayoutDisplay, PayoutSlider);
        }

        private void SkillSlider_OnChange(UIElement element)
        {
            IsChanged = true;

            Events.skillSpeed = SkillSlider.Value;
            UpdateDisplay(SkillDisplay, SkillSlider);
        }

        private void PenaltySlider_OnChange(UIElement element)
        {
            IsChanged = true;

            Events.singleplayerPenalty = PenaltySlider.Value;
            UpdateDisplay(PenaltyDisplay, PenaltySlider, true);
        }

    }
}
