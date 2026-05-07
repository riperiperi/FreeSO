using System;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Parameter panel for <see cref="CityProcGen"/>. Lets the editor user
    /// pick a map type + four 3-step settings (Height / Water / Roughness
    /// / Forest) and click Generate to roll a fresh map. Stays open after
    /// generating so the user can tweak a knob and re-roll.
    /// </summary>
    public class UIGenerateMapDialog : UIDialog
    {
        private readonly Action<CityProcGen.Parameters> _OnGenerate;
        private readonly CityProcGen.Parameters _Params = CityProcGen.Parameters.DefaultsFor(CityProcGen.MapType.Island);

        private UIButton[] _TypeButtons;
        private UIButton[] _HeightButtons;
        private UIButton[] _WaterButtons;
        private UIButton[] _RoughnessButtons;
        private UIButton[] _ForestButtons;

        private const int W = 540;
        private const int H = 360;

        // Keep label / button geometry in sync — labels are right-aligned
        // up to LABEL_RIGHT, button row begins immediately after.
        private const int LEFT_PAD     = 30;
        private const int LABEL_RIGHT  = 180;
        private const int ROW_BTN_X    = 200;
        private const int FIRST_ROW_Y  = 50;
        private const int ROW_HEIGHT   = 38;
        private const int LEVEL_BTN_W  = 80;
        private const int LEVEL_BTN_PAD = 6;

        public UIGenerateMapDialog(Action<CityProcGen.Parameters> onGenerate)
            : base(UIDialogStyle.Standard | UIDialogStyle.Close, true)
        {
            _OnGenerate = onGenerate;
            Opacity = 0.95f;
            Caption = "Generate Map";
            SetSize(W, H);

            BuildTypeRow(FIRST_ROW_Y);
            BuildLevelRow("Avg Height:", FIRST_ROW_Y + ROW_HEIGHT * 1, out _HeightButtons,
                lvl => { _Params.HeightAvg = lvl; RefreshLevelRow(_HeightButtons, _Params.HeightAvg); });
            BuildLevelRow("Water Cover:", FIRST_ROW_Y + ROW_HEIGHT * 2, out _WaterButtons,
                lvl => { _Params.WaterRatio = lvl; RefreshLevelRow(_WaterButtons, _Params.WaterRatio); });
            BuildLevelRow("Roughness:", FIRST_ROW_Y + ROW_HEIGHT * 3, out _RoughnessButtons,
                lvl => { _Params.Roughness = lvl; RefreshLevelRow(_RoughnessButtons, _Params.Roughness); });
            BuildLevelRow("Forest:", FIRST_ROW_Y + ROW_HEIGHT * 4, out _ForestButtons,
                lvl => { _Params.ForestDensity = lvl; RefreshLevelRow(_ForestButtons, _Params.ForestDensity); });

            BuildBottomRow();

            CloseButton.OnButtonClick += _ => UIScreen.RemoveDialog(this);

            // Sync visual selection with the type's defaults.
            // GlobalShowDialog will handle centering on the current screen.
            ApplyTypeDefaults(_Params.Type);
        }

        private void BuildTypeRow(int y)
        {
            Add(NewLabel("Map Type:", LEFT_PAD, y + 6));
            string[] captions = { "Island", "Coastal", "Inland", "Mountains" };
            CityProcGen.MapType[] types = {
                CityProcGen.MapType.Island, CityProcGen.MapType.Coastal,
                CityProcGen.MapType.Inland, CityProcGen.MapType.Mountains
            };
            _TypeButtons = new UIButton[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                int idx = i;
                var btn = new UIButton {
                    Caption = captions[i],
                    X = ROW_BTN_X + i * (LEVEL_BTN_W + LEVEL_BTN_PAD),
                    Y = y,
                    Width = LEVEL_BTN_W,
                };
                btn.OnButtonClick += _ => ApplyTypeDefaults(types[idx]);
                Add(btn);
                _TypeButtons[i] = btn;
            }
        }

        private void BuildLevelRow(string label, int y, out UIButton[] buttons,
            Action<CityProcGen.Level> onClick)
        {
            Add(NewLabel(label, LEFT_PAD, y + 6));
            string[] captions = { "Low", "Medium", "High" };
            CityProcGen.Level[] levels = {
                CityProcGen.Level.Low, CityProcGen.Level.Medium, CityProcGen.Level.High
            };
            buttons = new UIButton[3];
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var btn = new UIButton {
                    Caption = captions[i],
                    X = ROW_BTN_X + i * (LEVEL_BTN_W + LEVEL_BTN_PAD),
                    Y = y,
                    Width = LEVEL_BTN_W,
                };
                btn.OnButtonClick += _ => onClick(levels[idx]);
                Add(btn);
                buttons[i] = btn;
            }
        }

        private void BuildBottomRow()
        {
            int y = H - 50;

            var cancel = new UIButton {
                Caption = "Cancel",
                X = W - 280, Y = y, Width = 110,
            };
            cancel.OnButtonClick += _ => UIScreen.RemoveDialog(this);
            Add(cancel);

            var generate = new UIButton {
                Caption = "Generate",
                X = W - 150, Y = y, Width = 120,
            };
            generate.OnButtonClick += _ => Generate();
            Add(generate);
        }

        private void Generate()
        {
            // Fresh random seed each click so identical knobs still
            // produce different rolls. The caller (toolbar) shows the
            // seed in the status label after generation so the user
            // knows what was used.
            _Params.Seed = new Random().Next(int.MinValue + 1, int.MaxValue);
            _OnGenerate?.Invoke(_Params);
        }

        private void ApplyTypeDefaults(CityProcGen.MapType type)
        {
            var d = CityProcGen.Parameters.DefaultsFor(type);
            _Params.Type = type;
            _Params.HeightAvg = d.HeightAvg;
            _Params.WaterRatio = d.WaterRatio;
            _Params.Roughness = d.Roughness;
            _Params.ForestDensity = d.ForestDensity;

            RefreshTypeRow();
            RefreshLevelRow(_HeightButtons, _Params.HeightAvg);
            RefreshLevelRow(_WaterButtons, _Params.WaterRatio);
            RefreshLevelRow(_RoughnessButtons, _Params.Roughness);
            RefreshLevelRow(_ForestButtons, _Params.ForestDensity);
        }

        private void RefreshTypeRow()
        {
            CityProcGen.MapType[] types = {
                CityProcGen.MapType.Island, CityProcGen.MapType.Coastal,
                CityProcGen.MapType.Inland, CityProcGen.MapType.Mountains
            };
            string[] captions = { "Island", "Coastal", "Inland", "Mountains" };
            for (int i = 0; i < _TypeButtons.Length; i++)
            {
                bool active = types[i] == _Params.Type;
                _TypeButtons[i].Caption = active ? "[" + captions[i] + "]" : captions[i];
            }
        }

        private void RefreshLevelRow(UIButton[] buttons, CityProcGen.Level current)
        {
            CityProcGen.Level[] levels = {
                CityProcGen.Level.Low, CityProcGen.Level.Medium, CityProcGen.Level.High
            };
            string[] captions = { "Low", "Medium", "High" };
            for (int i = 0; i < buttons.Length; i++)
            {
                bool active = levels[i] == current;
                buttons[i].Caption = active ? "[" + captions[i] + "]" : captions[i];
            }
        }

        private static UILabel NewLabel(string text, int x, int y)
        {
            return new UILabel {
                Caption = text,
                X = x, Y = y,
                Size = new Vector2(LABEL_RIGHT - x, 22),
            };
        }
    }
}