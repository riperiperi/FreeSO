using System;
using System.IO;
using FSO.Client.Rendering.City;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Files;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Top-of-screen toolbar for the city editor. Three rows:
    ///   1. Six mode buttons (Road / Terrain / Elev↑ / Flatten / Forest / Density)
    ///   2. Up to five modifier buttons, contextual to the current mode
    ///      (Terrain → Grass/Water/Rock/Snow/Sand, Forest types, density %)
    ///   3. Brush size ± / Save / Load / Clear + a one-line status label
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
            x += 84;

            var load = new UIButton { Caption = "Load", X = x, Y = ROW_Y_BRUSH, Width = 80 };
            load.OnButtonClick += OnLoadClicked;
            Add(load);
            x += 84;

            var clear = new UIButton { Caption = "Clear", X = x, Y = ROW_Y_BRUSH, Width = 80 };
            clear.OnButtonClick += OnClearClicked;
            Add(clear);
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
                CityBaker.Save(_City, dir);
                _StatusLabel.Caption = "Saved to " + dir;
            }
            catch (Exception ex)
            {
                _StatusLabel.Caption = "Save failed: " + ex.Message;
            }
        }

        private void OnLoadClicked(UIElement _)
        {
            // Slot dirs are written by Save (this toolbar) and the F2..F11
            // hotkeys in MapPainterPlugin. Show the user only the ones that
            // actually exist on disk.
            var available = new System.Collections.Generic.List<int>();
            for (int i = 2; i <= 11; i++)
            {
                var dir = Path.Combine(FSOEnvironment.UserDir, "CityPainterSave" + i + "/");
                if (Directory.Exists(dir)) available.Add(i);
            }

            if (available.Count == 0)
            {
                UIAlert.Alert("Load City",
                    "No saved cities found.\nSave one first (Save button), then come back here.",
                    true);
                return;
            }

            UIAlert alert = null;
            var buttons = new System.Collections.Generic.List<UIAlertButton>();
            foreach (var slot in available)
            {
                var capturedSlot = slot;
                buttons.Add(new UIAlertButton(UIAlertButtonType.OK,
                    (btn) => { UIScreen.RemoveDialog(alert); LoadSlot(capturedSlot); },
                    "Slot " + capturedSlot));
            }
            buttons.Add(new UIAlertButton(UIAlertButtonType.Cancel,
                (btn) => { UIScreen.RemoveDialog(alert); }));

            alert = UIScreen.GlobalShowAlert(new UIAlertOptions
            {
                Title = "Load City",
                Message = "Pick a saved city to load.\nWarning: discards any unsaved changes.",
                Buttons = buttons.ToArray()
            }, true);
        }

        private void LoadSlot(int slot)
        {
            try
            {
                var dir = Path.Combine(FSOEnvironment.UserDir, "CityPainterSave" + slot + "/");
                _City.MapData.Load(dir, LoadTex, "png");
                _City.GenerateCityMesh(GameFacade.GraphicsDevice, null);
                _StatusLabel.Caption = "Loaded slot " + slot;
            }
            catch (Exception ex)
            {
                _StatusLabel.Caption = "Load failed: " + ex.Message;
            }
        }

        // Mirrors MapPainterPlugin.LoadTex — small helper to materialize a
        // PNG path into a Texture2D for CityMapData.Load.
        private Texture2D LoadTex(string path)
        {
            using (var strm = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try
                {
                    return ImageLoader.FromStream(GameFacade.GraphicsDevice, strm);
                }
                catch
                {
                    return new Texture2D(GameFacade.GraphicsDevice, 1, 1);
                }
            }
        }

        private void OnClearClicked(UIElement _)
        {
            UIAlert.YesNo("Clear Map",
                "This wipes elevation, terrain type, roads, and forests\n" +
                "for the current map. Unsaved work will be lost.\n\nClear?",
                true,
                yes => { if (yes) ClearMap(); });
        }

        // Reset all five engine layers to a blank-canvas baseline:
        //   - elevation: flat, slightly above sea level (visible as land)
        //   - terraintype: grass everywhere
        //   - roads / forest density / forest type: empty
        // The engine input file (vertexcolor.png) is regenerated by Save,
        // so we don't need to touch that here.
        private void ClearMap()
        {
            try
            {
                var md = _City.MapData;
                int n = md.ElevationData.Length;
                for (int i = 0; i < n; i++)
                {
                    md.ElevationData[i] = 64;          // flat plateau just above water
                    md.TerrainType[i] = 0;             // grass
                    md.TerrainTypeColorData[i] = new Color(0, 255, 0);
                    md.RoadData[i] = 0;
                    md.ForestDensityData[i] = 0;
                    md.ForestTypeData[i] = new Color(0, 0, 0);
                }
                _City.GenerateCityMesh(GameFacade.GraphicsDevice, null);
                _StatusLabel.Caption = "Map cleared";
            }
            catch (Exception ex)
            {
                _StatusLabel.Caption = "Clear failed: " + ex.Message;
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