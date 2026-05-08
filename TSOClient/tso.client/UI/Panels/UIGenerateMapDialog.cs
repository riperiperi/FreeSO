using System;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Parameter panel for <see cref="CityProcGen"/>. Lets the editor
    /// user pick a map type plus six 3-step knobs (Elevation, Water,
    /// Detail, Forest, Rivers, Lakes) and Generate / Reroll. Stays open
    /// after each generation so the user can tweak knobs and re-roll.
    /// Captions are semantic per-knob (Flat/Rolling/Steep, Sparse/Mod/
    /// Heavy, etc.) instead of generic Low/Medium/High so the meaning
    /// is obvious without reading code.
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
        private UIButton[] _RiversButtons;
        private UIButton[] _LakesButtons;

        // Per-knob captions, more semantic than Low/Medium/High.
        private static readonly string[] _ElevationCaptions = { "Flat",   "Rolling",  "Steep"  };
        private static readonly string[] _WaterCaptions     = { "Sparse", "Moderate", "Heavy"  };
        private static readonly string[] _DetailCaptions    = { "Smooth", "Natural",  "Jagged" };
        private static readonly string[] _ForestCaptions    = { "Sparse", "Moderate", "Dense"  };
        private static readonly string[] _CountCaptions     = { "None",   "Few",      "Many"   };

        // Eight types laid out in a 4x2 grid. Internal enum names stay
        // (Archipelago etc.) but display captions are short.
        private static readonly CityProcGen.MapType[] _Types = {
            CityProcGen.MapType.Island,    CityProcGen.MapType.Archipelago,
            CityProcGen.MapType.Coastal,   CityProcGen.MapType.Inland,
            CityProcGen.MapType.Lakeland,  CityProcGen.MapType.Highland,
            CityProcGen.MapType.Mountains, CityProcGen.MapType.Plateau,
        };
        private static readonly string[] _TypeCaptions = {
            "Island",   "Atolls",
            "Coastal",  "Inland",
            "Lakeland", "Highland",
            "Mountains","Plateau",
        };

        private const int W = 540;
        private const int H = 540;

        // Last seed used (display + Reroll source). Set after each
        // Generate / Reroll click; shown to the user so they can note
        // a roll they liked.
        private UILabel _SeedLabel;

        // Layout constants. Type row fits 4 buttons per line at 80×38;
        // setting rows below also use 80px buttons for visual rhythm.
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

            // Two rows of type buttons (8 total).
            BuildTypeRows(FIRST_ROW_Y);

            // Setting rows. Each row index is offset by 2 to skip the
            // two type rows.
            BuildLevelRow("Elevation:", FIRST_ROW_Y + ROW_HEIGHT * 2, _ElevationCaptions, out _HeightButtons,
                lvl => { _Params.HeightAvg = lvl;
                    RefreshLevelRow(_HeightButtons, _ElevationCaptions, _Params.HeightAvg); });
            BuildLevelRow("Water:", FIRST_ROW_Y + ROW_HEIGHT * 3, _WaterCaptions, out _WaterButtons,
                lvl => { _Params.WaterRatio = lvl;
                    RefreshLevelRow(_WaterButtons, _WaterCaptions, _Params.WaterRatio); });
            BuildLevelRow("Detail:", FIRST_ROW_Y + ROW_HEIGHT * 4, _DetailCaptions, out _RoughnessButtons,
                lvl => { _Params.Roughness = lvl;
                    RefreshLevelRow(_RoughnessButtons, _DetailCaptions, _Params.Roughness); });
            BuildLevelRow("Forest:", FIRST_ROW_Y + ROW_HEIGHT * 5, _ForestCaptions, out _ForestButtons,
                lvl => { _Params.ForestDensity = lvl;
                    RefreshLevelRow(_ForestButtons, _ForestCaptions, _Params.ForestDensity); });
            BuildLevelRow("Rivers:", FIRST_ROW_Y + ROW_HEIGHT * 6, _CountCaptions, out _RiversButtons,
                lvl => { _Params.Rivers = lvl;
                    RefreshLevelRow(_RiversButtons, _CountCaptions, _Params.Rivers); });
            BuildLevelRow("Lakes:", FIRST_ROW_Y + ROW_HEIGHT * 7, _CountCaptions, out _LakesButtons,
                lvl => { _Params.Lakes = lvl;
                    RefreshLevelRow(_LakesButtons, _CountCaptions, _Params.Lakes); });

            BuildBottomRow();

            CloseButton.OnButtonClick += _ => UIScreen.RemoveDialog(this);

            // Sync visual selection with the type's defaults.
            ApplyTypeDefaults(_Params.Type);
        }

        private void BuildTypeRows(int y)
        {
            Add(NewLabel("Map Type:", LEFT_PAD, y + 6));
            _TypeButtons = new UIButton[_Types.Length];
            for (int i = 0; i < _Types.Length; i++)
            {
                int idx = i;
                int row = i / 4;
                int col = i % 4;
                var btn = new UIButton {
                    Caption = _TypeCaptions[i],
                    X = ROW_BTN_X + col * (LEVEL_BTN_W + LEVEL_BTN_PAD),
                    Y = y + row * ROW_HEIGHT,
                    Width = LEVEL_BTN_W,
                };
                btn.OnButtonClick += _ => ApplyTypeDefaults(_Types[idx]);
                Add(btn);
                _TypeButtons[i] = btn;
            }
        }

        private void BuildLevelRow(string label, int y, string[] captions,
            out UIButton[] buttons, Action<CityProcGen.Level> onClick)
        {
            Add(NewLabel(label, LEFT_PAD, y + 6));
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
            int seedY = H - 92;
            int btnY  = H - 50;

            _SeedLabel = new UILabel {
                Caption = "Last seed: (none yet)",
                X = LEFT_PAD, Y = seedY + 4, Size = new Vector2(W - LEFT_PAD * 2, 22),
            };
            Add(_SeedLabel);

            var cancel = new UIButton {
                Caption = "Cancel",
                X = LEFT_PAD, Y = btnY, Width = 90,
            };
            cancel.OnButtonClick += _ => UIScreen.RemoveDialog(this);
            Add(cancel);

            // Reroll: regenerate with a new random seed, current knobs.
            var reroll = new UIButton {
                Caption = "Reroll",
                X = W - 270, Y = btnY, Width = 100,
            };
            reroll.OnButtonClick += _ => Generate();
            Add(reroll);

            var generate = new UIButton {
                Caption = "Generate",
                X = W - 150, Y = btnY, Width = 120,
            };
            generate.OnButtonClick += _ => Generate();
            Add(generate);
        }

        private void Generate()
        {
            _Params.Seed = new Random().Next(int.MinValue + 1, int.MaxValue);
            _OnGenerate?.Invoke(_Params);
            if (_SeedLabel != null) _SeedLabel.Caption = "Last seed: " + _Params.Seed;
        }

        private void ApplyTypeDefaults(CityProcGen.MapType type)
        {
            var d = CityProcGen.Parameters.DefaultsFor(type);
            _Params.Type = type;
            _Params.HeightAvg = d.HeightAvg;
            _Params.WaterRatio = d.WaterRatio;
            _Params.Roughness = d.Roughness;
            _Params.ForestDensity = d.ForestDensity;
            _Params.Rivers = d.Rivers;
            _Params.Lakes = d.Lakes;

            RefreshTypeRow();
            RefreshLevelRow(_HeightButtons,    _ElevationCaptions, _Params.HeightAvg);
            RefreshLevelRow(_WaterButtons,     _WaterCaptions,     _Params.WaterRatio);
            RefreshLevelRow(_RoughnessButtons, _DetailCaptions,    _Params.Roughness);
            RefreshLevelRow(_ForestButtons,    _ForestCaptions,    _Params.ForestDensity);
            RefreshLevelRow(_RiversButtons,    _CountCaptions,     _Params.Rivers);
            RefreshLevelRow(_LakesButtons,     _CountCaptions,     _Params.Lakes);
        }

        private void RefreshTypeRow()
        {
            for (int i = 0; i < _TypeButtons.Length; i++)
            {
                bool active = _Types[i] == _Params.Type;
                _TypeButtons[i].Caption = active ? "[" + _TypeCaptions[i] + "]" : _TypeCaptions[i];
            }
        }

        private void RefreshLevelRow(UIButton[] buttons, string[] captions, CityProcGen.Level current)
        {
            CityProcGen.Level[] levels = {
                CityProcGen.Level.Low, CityProcGen.Level.Medium, CityProcGen.Level.High
            };
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