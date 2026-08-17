using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.UI.Controls;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveGameplayScale : UIArchiveDialog
    {
        public UITextBox FundsInput;

        public UISlider SkillSlider;
        public UILabel SkillDisplay;

        public UISlider PayoutSlider;
        public UILabel PayoutDisplay;

        public UISlider PenaltySlider;
        public UILabel PenaltyDisplay;

        public UIButton SpeedyJobCheck;

        public UIButton HelpButton;
        public UIButton ResetButton;

        private readonly ArchiveConfiguration Config;
        private EventConfig Events;
        private bool IsChanged;

        public UIArchiveGameplayScale(ArchiveConfiguration config) : base(UIDialogStyle.OK, true)
        {
            Config = config;
            Caption = GetString("300");
            var vbox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Center };

            config.LoadEvents();

            Events = config.Events ?? new EventConfig() { catalog = [], modifiers = [] };

            TextStyle style = TextStyle.DefaultLabel.Clone();

            style.Shadow = true;
            style.Color = Color.White;

            UILabel desc;

            vbox.Add(desc = new UILabel()
            {
                Caption = GetString("301"),
                Wrapped = true
            });

            desc.Size = new Vector2(320, 90);

            var fundsBox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Left, Spacing = 0 };

            fundsBox.Add(new UILabel()
            {
                Caption = GetString("302")
            });

            fundsBox.Add(new UISpacer(250, 5));

            var fundsBox2 = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Middle };

            fundsBox2.Add(new UILabel() { Caption = "$" });

            fundsBox2.Add(FundsInput = new UITextBox()
            {
                Size = new Vector2(100, 25),
                CurrentText = config.InitialFunds.ToString()
            });

            fundsBox2.AutoSize();

            fundsBox.Add(fundsBox2);

            FundsInput.OnChange += FundsInput_OnChange;

            vbox.Add(fundsBox);

            vbox.Add(new UISpacer(10));

            var skillBox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Left };

            skillBox.Add(new UILabel()
            {
                Caption = GetString("303")
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
                Caption = GetString("304")
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
                Caption = GetString("305")
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

            var jobBox = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Middle };

            jobBox.Add(SpeedyJobCheck = new UIButton(GetTexture(0x0000083600000001))
            {
                Tooltip = GetString("307")
            });

            jobBox.Add(new UILabel()
            {
                Caption = GetString("306"),
                Tooltip = GetString("307")
            });

            vbox.Add(new UISpacer(10));

            vbox.Add(jobBox);

            vbox.Add(new UISpacer(10));

            var buttonsBox = new UIHBoxContainer();

            buttonsBox.Add(HelpButton = new UIButton()
            {
                Caption = GetString("127")
            });

            buttonsBox.Add(ResetButton = new UIButton()
            {
                Caption = GetString("128")
            });

            vbox.Add(buttonsBox);

            Add(vbox);

            HelpButton.OnButtonClick += HelpButton_OnButtonClick;
            ResetButton.OnButtonClick += ResetButton_OnButtonClick;

            SkillSlider.OnChange += SkillSlider_OnChange;
            PayoutSlider.OnChange += PayoutSlider_OnChange;
            PenaltySlider.OnChange += PenaltySlider_OnChange;
            SpeedyJobCheck.OnButtonClick += SpeedyJobCheck_OnButtonClick;

            UpdateDisplay(SkillDisplay, SkillSlider);
            UpdateDisplay(PayoutDisplay, PayoutSlider);
            UpdateDisplay(PenaltyDisplay, PenaltySlider, true);
            SpeedyJobCheck.Selected = Events.speedyJobProgression == 1;

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

        private void SpeedyJobCheck_OnButtonClick(UIElement button)
        {
            IsChanged = true;

            if (SpeedyJobCheck.Selected)
            {
                Events.speedyJobProgression = 0;
            }
            else
            {
                Events.speedyJobProgression = 1;
            }

            SpeedyJobCheck.Selected = Events.speedyJobProgression == 1;
        }

        private void FundsInput_OnChange(UIElement element)
        {
            if (int.TryParse(FundsInput.CurrentText, out int funds) && funds >= 0)
            {
                Config.InitialFunds = funds;
            }
            else
            {
                Config.InitialFunds = 0;
            }
        }

        private void ResetButton_OnButtonClick(UIElement button)
        {
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = GetString("128"),
                Message = GetString("129"),
                Buttons = [
                    new UIAlertButton(UIAlertButtonType.Yes, (btn) => { Reset(ref Events, true); UpdateAll(); UIScreen.RemoveDialog(alert); }, GetString("130")),
                    new UIAlertButton(UIAlertButtonType.No, (btn) => { Reset(ref Events, false); UpdateAll(); UIScreen.RemoveDialog(alert); }, GetString("131"))
                ]
            }, true);
        }

        private static void Reset(ref EventConfig events, bool tso)
        {
            events.skillSpeed = tso ? null : 5;
            events.payoutScale = tso ? null : 5;
            events.singleplayerPenalty = tso ? null : 0;
            events.speedyJobProgression = tso ? 0 : 1;
        }

        private void UpdateAll()
        {
            SkillSlider.Value = Events.skillSpeed ?? 1;
            PayoutSlider.Value = Events.payoutScale ?? 1;
            PenaltySlider.Value = Events.singleplayerPenalty ?? 1;
            SpeedyJobCheck.Selected = Events.speedyJobProgression == 1;

            UpdateDisplay(SkillDisplay, SkillSlider);
            UpdateDisplay(PayoutDisplay, PayoutSlider);
            UpdateDisplay(PenaltyDisplay, PenaltySlider, true);
        }

        private void HelpButton_OnButtonClick(UIElement button)
        {
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = GetString("127"),
                Message = GetString("126"),
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
