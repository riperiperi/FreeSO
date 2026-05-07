using System;
using System.IO;
using FSO.Client.Rendering.City;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Top-of-screen toolbar for the city editor. Three rows:
    ///   1. Six mode buttons (Road / Terrain / Elev↑ / Flatten / Forest / Density)
    ///   2. Up to five modifier buttons, contextual to the current mode
    ///      (Terrain → Grass/Water/Rock/Snow/Sand, Forest types, density %)
    ///   3. Brush size ± + Save + a one-line status label
    ///
    /// All keyboard shortcuts still work; this is just a discoverability
    /// layer over the existing painter API.
    /// </summary>
    public class CityEditorToolbar : UIContainer
    {
        private readonly MapPainterPlugin _Painter;
        private readonly Terrain _City;

        // Mode row.
        private readonly PainterMode[] _Modes = {
            PainterMode.ROAD,
            PainterMode.TERRAINTYPE,
            PainterMode.ELEVATION_CIRCLE,
            PainterMode.ELEVATION_FLAT,
            PainterMode.FORESTTYPE,
            PainterMode.FORESTDENSITY,
        };
        private readonly string[] _ModeCaptions = {
            "Road", "Terrain", "Elev", "Flatten", "Forest", "Density"
        };
        private readonly UIButton[] _ModeButtons;

        // Modifier row — at most 5 buttons; captions change per mode.
        private readonly UIButton[] _ModifierButtons = new UIButton[5];

        // Brush row.
        private UILabel _BrushLabel;
        private UILabel _StatusLabel;

        // Track last-known state so we only refresh button text when something changes.
        private PainterMode _LastMode = (PainterMode)(-1);
        private int _LastBrushSize = -1;
        private int _LastModifier = -1;

        private const int BTN_WIDTH = 90;
        private const int BTN_PAD = 4;
        private const int ROW_Y_MODE = 10;
        private const int ROW_Y_MODIFIER = 44;
        private const int ROW_Y_BRUSH = 78;

        public CityEditorToolbar(MapPainterPlugin painter, Terrain city)
        {
            _Painter = painter;
            _City = city;
            _ModeButtons = new UIButton[_Modes.Length];
            BuildModeRow();
            BuildModifierRow();
            BuildBrushRow();
            // Sensible default: Terrain mode, Grass.
            _Painter.Mode = PainterMode.TERRAINTYPE;
            _Painter.SelectedModifier = 0;
        }

        private void BuildModeRow()
        {
            for (int i = 0; i < _Modes.Length; i++)
            {
                var idx = i;
                var btn = new UIButton {
                    Caption = _ModeCaptions[i],
                    X = 10 + i * (BTN_WIDTH + BTN_PAD),
                    Y = ROW_Y_MODE,
                    Width = BTN_WIDTH,
                };
                btn.OnButtonClick += _ => _Painter.Mode = _Modes[idx];
                Add(btn);
                _ModeButtons[i] = btn;
            }
        }

        private void BuildModifierRow()
        {
            for (int i = 0; i < _ModifierButtons.Length; i++)
            {
                var idx = i;
                var btn = new UIButton {
                    Caption = "",
                    X = 10 + i * (BTN_WIDTH + BTN_PAD),
                    Y = ROW_Y_MODIFIER,
                    Width = BTN_WIDTH,
                    Visible = false,
                };
                btn.OnButtonClick += _ => _Painter.SelectedModifier = idx;
                Add(btn);
                _ModifierButtons[i] = btn;
            }
        }

        private void BuildBrushRow()
        {
            int x = 10;

            var minus = new UIButton { Caption = "−", X = x, Y = ROW_Y_BRUSH, Width = 30 };
            minus.OnButtonClick += _ => _Painter.BrushSize = Math.Max(0, _Painter.BrushSize - 1);
            Add(minus);
            x += 34;

            _BrushLabel = new UILabel {
                Caption = "Brush: 0",
                X = x, Y = ROW_Y_BRUSH + 4, Size = new Vector2(80, 22),
            };
            Add(_BrushLabel);
            x += 90;

            var plus = new UIButton { Caption = "+", X = x, Y = ROW_Y_BRUSH, Width = 30 };
            plus.OnButtonClick += _ => _Painter.BrushSize += 1;
            Add(plus);
            x += 40;

            var save = new UIButton { Caption = "Save", X = x, Y = ROW_Y_BRUSH, Width = 80 };
            save.OnButtonClick += OnSaveClicked;
            Add(save);
            x += 90;

            _StatusLabel = new UILabel {
                Caption = "",
                X = x, Y = ROW_Y_BRUSH + 4, Size = new Vector2(400, 22),
            };
            Add(_StatusLabel);
        }

        private void OnSaveClicked(UIElement _)
        {
            try
            {
                var dir = Path.Combine(FSOEnvironment.UserDir, "CityPainterSave2/");
                Directory.CreateDirectory(dir);
                _City.MapData.Save(dir);
                CityBaker.Save(_City.MapData, dir);
                _StatusLabel.Caption = "Saved to " + dir;
            }
            catch (Exception ex)
            {
                _StatusLabel.Caption = "Save failed: " + ex.Message;
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            RefreshIfChanged();
        }

        private void RefreshIfChanged()
        {
            if (_Painter.Mode != _LastMode)
            {
                _LastMode = _Painter.Mode;
                RefreshModeRow();
                RefreshModifierRow();
            }
            if (_Painter.SelectedModifier != _LastModifier)
            {
                _LastModifier = _Painter.SelectedModifier;
                RefreshModifierRow();
            }
            if (_Painter.BrushSize != _LastBrushSize)
            {
                _LastBrushSize = _Painter.BrushSize;
                _BrushLabel.Caption = "Brush: " + _Painter.BrushSize;
            }
        }

        private void RefreshModeRow()
        {
            for (int i = 0; i < _ModeButtons.Length; i++)
            {
                var active = _Modes[i] == _Painter.Mode;
                _ModeButtons[i].Caption = active ? "[" + _ModeCaptions[i] + "]" : _ModeCaptions[i];
            }
        }

        private void RefreshModifierRow()
        {
            var labels = ModifierLabelsFor(_Painter.Mode);
            for (int i = 0; i < _ModifierButtons.Length; i++)
            {
                if (i < labels.Length)
                {
                    var active = i == _Painter.SelectedModifier;
                    _ModifierButtons[i].Caption = active ? "[" + labels[i] + "]" : labels[i];
                    _ModifierButtons[i].Visible = true;
                }
                else
                {
                    _ModifierButtons[i].Visible = false;
                }
            }
        }

        private static readonly string[] _TerrainLabels  = { "Grass", "Water", "Rock", "Snow", "Sand" };
        private static readonly string[] _ForestLabels   = { "Fir", "Birch", "Cactus", "Palm", "Clear" };
        private static readonly string[] _DensityLabels  = { "0%", "25%", "50%", "75%", "100%" };
        private static readonly string[] _NoLabels       = new string[0];

        private static string[] ModifierLabelsFor(PainterMode mode)
        {
            switch (mode)
            {
                case PainterMode.TERRAINTYPE:   return _TerrainLabels;
                case PainterMode.FORESTTYPE:    return _ForestLabels;
                case PainterMode.FORESTDENSITY: return _DensityLabels;
                default:                        return _NoLabels;  // ROAD / ELEVATION_*
            }
        }
    }
}