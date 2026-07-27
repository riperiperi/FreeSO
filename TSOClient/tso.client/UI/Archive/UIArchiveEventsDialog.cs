using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveEventsDialog : UIDialog
    {
        private ArchiveConfiguration Config;
        private EventConfig Events;
        private TextStyle ModifierHeaderStyle;
        private TextStyle GroupHeaderStyle;

        private UIVBoxContainer RootVBox;
        private UIVBoxContainer ModifierVBox;
        private UIHBoxContainer TabHBox;
        private UIContainer ActiveModifierEditor;
        private UIButton[] ModifierButtons;
        private UIContainer[] ModifierEditors;

        private UIHBoxContainer ManualHBox;
        private UIButton ManualClearButton;
        private UIButton ManualTimedButton;

        private UILabel TimedDuration;

        private List<Action> CheckUpdateCallbacks;

        private bool ManualMode = false;
        private bool IsChanged = false;

        public UIArchiveEventsDialog(ArchiveConfiguration config) : base(UIDialogStyle.OK, true)
        {
            var gd = GameFacade.GraphicsDevice;
            var custom = Content.Content.Get().CustomUI;
            var tabTex = custom.Get("archive_tab.png").Get(gd);

            Caption = "Events";
            Config = config;

            config.LoadEvents();

            Events = config.Events ?? new EventConfig() { catalog = [], modifiers = [] };
            ManualMode = !Events.timed;

            CheckUpdateCallbacks = [];

            ModifierHeaderStyle = TextStyle.DefaultLabel.Clone();
            ModifierHeaderStyle.Shadow = true;
            ModifierHeaderStyle.Color = Color.White;
            ModifierHeaderStyle.Size = 16;

            GroupHeaderStyle = TextStyle.DefaultLabel.Clone();
            GroupHeaderStyle.Shadow = true;
            GroupHeaderStyle.Size = 14;

            var vbox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Center };
            RootVBox = vbox;

            var modeHbox = new UIHBoxContainer() { Spacing = 16 };

            modeHbox.Add(new UILabel()
            {
                Caption = "Event schedule:"
            });
            AddCheck(modeHbox, "Timed", (check) => check.Selected = !ManualMode, (elem) => ManualMode = false, true);
            AddCheck(modeHbox, "Manual", (check) => check.Selected = ManualMode, (elem) => ManualMode = true, true);

            modeHbox.AutoSize();

            vbox.Add(modeHbox);
            vbox.Add(new UISpacer(10));

            var modifierVBox = new UIVBoxContainer();
            ModifierVBox = modifierVBox;

            var tabHbox = new UIHBoxContainer() { Spacing = 0 };

            ModifierButtons = new UIButton[Events.modifiers.Length];
            ModifierEditors = new UIContainer[Events.modifiers.Length];

            for (int i = 0; i < Events.modifiers.Length; i++)
            {
                var modifier = Events.modifiers[i];

                var btn = new UIButton()
                {
                    Texture = tabTex,
                    Caption = modifier.label,
                    AutoMargins = 32
                };

                int btnI = i;
                btn.OnButtonClick += (elem) =>
                {
                    SetModifierEditor(btnI);
                };

                ModifierButtons[i] = btn;

                tabHbox.Add(btn);
            }

            modifierVBox.Add(tabHbox);
            TabHBox = tabHbox;

            for (int i = 0; i < Events.modifiers.Length; i++)
            {
                ModifierEditors[i] = GenerateModifier(i);
            }

            modifierVBox.Add(new UISpacer(10));

            modifierVBox.AutoSize();
            vbox.Add(modifierVBox);

            var manualHbox = new UIHBoxContainer();
            manualHbox.Add(ManualClearButton = new UIButton()
            {
                Caption = "Clear"
            });
            manualHbox.Add(ManualTimedButton = new UIButton()
            {
                Caption = "Simulate Timed"
            });

            ManualClearButton.OnButtonClick += ClearManual;
            ManualTimedButton.OnButtonClick += SimulateTimed;

            ManualHBox = manualHbox;

            TimedDuration = new UILabel();
            TimedDuration.CaptionStyle = TimedDuration.CaptionStyle.Clone();
            TimedDuration.CaptionStyle.Shadow = true;
            TimedDuration.CaptionStyle.Color = Color.White;

            vbox.Add(TimedDuration);

            UpdateModifierButtons();

            vbox.Position = new Vector2(20, 45);

            if (Events.modifiers.Length > 0)
            {
                SetModifierEditor(0);
            }
            else
            {
                AutoSize();
            }

            Add(vbox);

            OKButton.OnButtonClick += OKButton_OnButtonClick;
        }

        private void SimulateTimed(UIElement button)
        {
            // Matches manual with timed in the selected category.
            var i = Array.IndexOf(ModifierEditors, ActiveModifierEditor);

            if (i == -1)
            {
                return;
            }

            ref var modifier = ref Events.modifiers[i];

            for (int j = 0; j < modifier.options.Length; j++)
            {
                ref var option = ref modifier.options[j];

                if (option.enableTimed)
                {
                    ClearOverlapping(i, in option);
                    option.enableManual = true;
                }
            }

            UpdateCheckButtons();
        }

        private void ClearManual(UIElement button)
        {
            // Clears the selected category.
            var i = Array.IndexOf(ModifierEditors, ActiveModifierEditor);

            if (i == -1)
            {
                return;
            }

            ref var modifier = ref Events.modifiers[i];

            for (int j = 0; j < modifier.options.Length; j++)
            {
                modifier.options[j].enableManual = false;
            }

            UpdateCheckButtons();
        }

        private int GetManualCount(in EventModifier modifier)
        {
            int count = 0;

            foreach (var option in modifier.options)
            {
                if (option.enableManual)
                {
                    count++;
                }
            }

            return count;
        }

        private void UpdateModifierButtons()
        {
            for (int i = 0; i < ModifierButtons.Length; i++)
            {
                var button = ModifierButtons[i];
                var modifier = Events.modifiers[i];

                button.Caption = ManualMode ? $"{modifier.label} ({GetManualCount(in modifier)})" : modifier.label;
            }

            TabHBox.AutoSize();

            bool manualHboxVisible = ManualHBox.Parent?.GetChildren().Contains(ManualHBox) ?? false;

            if (manualHboxVisible != ManualMode)
            {
                if (ManualMode)
                {
                    RootVBox.Add(ManualHBox);
                    RootVBox.Remove(TimedDuration);
                }
                else
                {
                    RootVBox.Remove(ManualHBox);
                    RootVBox.Add(TimedDuration);
                }
            }

            AutoSize();
        }

        private void UpdateCheckButtons()
        {
            foreach (var action in CheckUpdateCallbacks)
            {
                action();
            }

            UpdateModifierButtons();
        }

        private static Texture2D GetCheckTexture(bool radio)
        {
            return GetTexture(radio ? 0x0000045200000001u : 0x0000083600000001u);
        }

        private void AutoSize()
        {
            var vbox = RootVBox;
            vbox.AutoSize();

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 70);
        }

        private void SetModifierEditor(int i)
        {
            var vbox = ModifierVBox;
            var children = vbox.GetChildren();
            int insertIndex = children.IndexOf(TabHBox) + 1;
            if (ActiveModifierEditor != null)
            {
                insertIndex = children.IndexOf(ActiveModifierEditor);
                vbox.Remove(ActiveModifierEditor);
            }

            ActiveModifierEditor = ModifierEditors[i];

            vbox.AddAt(insertIndex, ActiveModifierEditor);

            for (int j = 0; j < ModifierButtons.Length; j++)
            {
                ModifierButtons[j].Selected = j == i;
            }

            var modifier = Events.modifiers[i];
            var (start, end) = EventConfig.GetNextRange(modifier.startDate, modifier.endDate);
            TimedDuration.Caption = $"{start:d} - {end:d}";
            TimedDuration.AutoSize();

            AutoSize();
        }

        private void OKButton_OnButtonClick(UIElement button)
        {
            if (IsChanged)
            {
                Events.timed = !ManualMode;

                Config.Events = Events;

                Config.SaveEvents();
            }

            UIScreen.RemoveDialog(this);
        }

        private ref bool GetCheckVar(ref EventModifierOption option)
        {
            if (ManualMode)
            {
                return ref option.enableManual;
            }
            else
            {
                return ref option.enableTimed;
            }
        }

        private void AddCheck(UIContainer container, string label, Action<UIButton> updateChecked, ButtonClickDelegate onClick, bool radio = false)
        {
            var hbox = new UIHBoxContainer();

            var check = new UIButton(GetCheckTexture(radio));

            Action updateMethod = () =>
            {
                updateChecked(check);
            };

            check.OnButtonClick += (elem) =>
            {
                onClick(elem);

                IsChanged = true;

                UpdateCheckButtons();
            };

            CheckUpdateCallbacks.Add(updateMethod);

            updateMethod();

            hbox.Add(check);

            var labelElem = new UILabel()
            {
                Caption = label
            };

            hbox.Add(labelElem);

            hbox.AutoSize();
            container.Add(hbox);
        }

        private void ClearOverlapping(int modifierId, in EventModifierOption option)
        {
            if (option.unique == null)
            {
                return;
            }

            // Need to clear all other overlapping uniques before checking this one.

            if (ManualMode)
            {
                for (int i = 0; i < Events.modifiers.Length; i++)
                {
                    ref var modifier = ref Events.modifiers[i];

                    for (int j = 0; j < modifier.options.Length; j++)
                    {
                        ref var otherOption = ref modifier.options[j];

                        if (otherOption.unique == option.unique)
                        {
                            GetCheckVar(ref otherOption) = false;
                        }
                    }
                }
            }
            else
            {
                // For timed, it's just within the same modifier.
                ref var modifier = ref Events.modifiers[modifierId];

                for (int j = 0; j < modifier.options.Length; j++)
                {
                    ref var otherOption = ref modifier.options[j];

                    if (otherOption.unique == option.unique)
                    {
                        GetCheckVar(ref otherOption) = false;
                    }
                }
            }
        }

        private void GenerateOption(UIContainer container, int modifierId, int optionId)
        {
            var option = Events.modifiers[modifierId].options[optionId];

            AddCheck(
                container, 
                option.label, 
                (check) =>
                {
                    var option = Events.modifiers[modifierId].options[optionId];

                    check.Selected = GetCheckVar(ref option);
                },
                (elem) =>
                {
                    ref var option = ref Events.modifiers[modifierId].options[optionId];
                    ref var isChecked = ref GetCheckVar(ref option);

                    if (!isChecked)
                    {
                        ClearOverlapping(modifierId, in option);
                    }

                    isChecked = !isChecked;
                },
                option.unique != null);
        }

        private void GenerateOptionGroup(UIContainer container, string categoryLabel, int modifierId, int[] optionIds)
        {
            var vbox = new UIVBoxContainer();

            var label = new UILabel()
            {
                Caption = categoryLabel,
                CaptionStyle = GroupHeaderStyle,
            };

            vbox.Add(label);

            foreach (int option in optionIds)
            {
                GenerateOption(vbox, modifierId, option);
            }

            vbox.AutoSize();
            container.Add(vbox);
        }

        private UIContainer GenerateModifier(int modifierId)
        {
            var modifier = Events.modifiers[modifierId];
            var vbox = new UIVBoxContainer();

            var optByCategory = modifier.options.Select((x, index) => (index, x)).GroupBy((option) => option.x.category).ToArray();

            for (int i = 0; i < optByCategory.Length; i += 2)
            {
                var hbox = new UIHBoxContainer();
                hbox.Add(new UISpacer(20));

                var groupOne = optByCategory[i];

                GenerateOptionGroup(hbox, groupOne.First().x.category, modifierId, groupOne.Select(x => x.index).ToArray());

                if (i + 1 < optByCategory.Length)
                {
                    hbox.Add(new UISpacer(20));

                    var groupTwo = optByCategory[i + 1];
                    GenerateOptionGroup(hbox, groupTwo.First().x.category, modifierId, groupTwo.Select(x => x.index).ToArray());
                }

                vbox.Add(hbox);
            }

            return vbox;
        }
    }
}
