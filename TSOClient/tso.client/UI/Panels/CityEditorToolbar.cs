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

        // Status label auto-fade. Hold full-opacity for HOLD seconds,
        // then linearly fade to 0 over FADE seconds, then clear text.
        // SetStatus is called by every Save / Load / Generate / Clear
        // result so users get the same toast-style feedback across
        // all operations.
        private DateTime _StatusSetAt = DateTime.MinValue;
        private const double STATUS_HOLD_SECONDS = 2.0;
        private const double STATUS_FADE_SECONDS = 1.0;

        // The directory the user most recently loaded from / saved to.
        // Used to pre-fill the Save / Load path prompts so the user
        // doesn't have to retype the full path each time. null if the
        // current map has never been associated with a directory yet
        // (e.g., a fresh procedural Generate that hasn't been saved).
        private string _CurrentPath;

        public CityEditorToolbar(MapPainterPlugin painter, Terrain city, string initialPath)
        {
            _Painter = painter;
            _City = city;
            _CurrentPath = initialPath;
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

            var save = new UIButton { Caption = "Save", X = x, Y = ROW_Y_BRUSH, Width = 60 };
            save.OnButtonClick += OnSaveClicked;
            Add(save);
            x += 64;

            var saveAs = new UIButton { Caption = "Save As", X = x, Y = ROW_Y_BRUSH, Width = 80 };
            saveAs.OnButtonClick += OnSaveAsClicked;
            Add(saveAs);
            x += 84;

            var load = new UIButton { Caption = "Load", X = x, Y = ROW_Y_BRUSH, Width = 80 };
            load.OnButtonClick += OnLoadClicked;
            Add(load);
            x += 84;

            var clear = new UIButton { Caption = "Clear", X = x, Y = ROW_Y_BRUSH, Width = 80 };
            clear.OnButtonClick += OnClearClicked;
            Add(clear);
            x += 84;

            var gen = new UIButton { Caption = "Generate", X = x, Y = ROW_Y_BRUSH, Width = 90 };
            gen.OnButtonClick += OnGenerateClicked;
            Add(gen);
            x += 100;

            _StatusLabel = new UILabel {
                Caption = "",
                X = x, Y = ROW_Y_BRUSH + 4, Size = new Vector2(400, 22),
            };
            Add(_StatusLabel);
        }

        // Save: silent overwrite of the current path if known. Useful for
        // iterative work — paint a bit, hit Save, paint, hit Save. If no
        // current path exists yet (fresh procgen / loaded blank scaffold),
        // falls through to the same prompt as Save As.
        private void OnSaveClicked(UIElement _)
        {
            if (!string.IsNullOrEmpty(_CurrentPath))
            {
                SaveTo(_CurrentPath);
                return;
            }
            PromptForSavePath();
        }

        // Save As: always prompts. For exporting / branching to a new
        // directory without overwriting the source.
        private void OnSaveAsClicked(UIElement _)
        {
            PromptForSavePath();
        }

        private void PromptForSavePath()
        {
            string suggested = _CurrentPath ?? Path.Combine(
                FSOEnvironment.UserDir, "CityPainterSave2");

            UIAlert prompt = null;
            prompt = UIScreen.GlobalShowAlert(new UIAlertOptions
            {
                Title = "Save Map",
                Message = "Enter the absolute directory path to save into.\n" +
                          "Existing files in that directory will be overwritten.",
                TextEntry = true,
                Buttons = UIAlertButton.OkCancel(
                    btn =>
                    {
                        var entered = prompt.ResponseText;
                        UIScreen.RemoveDialog(prompt);
                        if (!string.IsNullOrEmpty(entered)) SaveTo(entered);
                    },
                    btn => UIScreen.RemoveDialog(prompt))
            }, true);
            prompt.ResponseText = suggested;
        }

        private void SaveTo(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                _City.MapData.Save(dir);
                CityBaker.Save(_City, dir);
                _CurrentPath = dir;
                SetStatus("Saved to " + dir);
            }
            catch (Exception ex)
            {
                SetStatus("Save failed: " + ex.Message);
            }
        }

        // Forces a full re-bake of the city geometry + assets on the next
        // Draw frame. Calling GenerateCityMesh alone sometimes leaves the
        // visible mesh stale (the display lags until the user interacts
        // with the map); setting RegenData=true plus invalidating the
        // camera projection makes the next frame redraw cleanly.
        private void RefreshMapDisplay()
        {
            _City.GenerateCityMesh(GameFacade.GraphicsDevice, null);
            _City.RegenData = true;
            _City.Camera?.ProjectionDirty();
        }

        private void OnLoadClicked(UIElement _)
        {
            string suggested = _CurrentPath ?? "";

            UIAlert prompt = null;
            prompt = UIScreen.GlobalShowAlert(new UIAlertOptions
            {
                Title = "Load Map",
                Message = "Enter the absolute path to a city directory.\n" +
                          "Warning: any unsaved changes will be discarded.",
                TextEntry = true,
                Buttons = UIAlertButton.OkCancel(
                    btn =>
                    {
                        var entered = prompt.ResponseText;
                        UIScreen.RemoveDialog(prompt);
                        if (!string.IsNullOrEmpty(entered)) LoadFrom(entered);
                    },
                    btn => UIScreen.RemoveDialog(prompt))
            }, true);
            prompt.ResponseText = suggested;
        }

        private void LoadFrom(string dir)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    SetStatus("Load failed: directory not found");
                    return;
                }
                _City.MapData.Load(dir, LoadTex, "png");
                // CityMapData.Load only reads the five engine layers
                // (elevation/terraintype/forest*/road). vertexcolor is
                // owned by CityContent and only populated by its own
                // LoadContent at city-init time — so without this
                // explicit reload, a Load via the toolbar uses the
                // stale GPU texture from the originally-launched city.
                var vcPath = Path.Combine(dir, "vertexcolor.png");
                if (File.Exists(vcPath) && _City.Content != null)
                {
                    _City.Content.VertexColor?.Dispose();
                    _City.Content.VertexColor = LoadTex(vcPath);
                }
                RefreshMapDisplay();
                _CurrentPath = dir;
                SetStatus("Loaded " + dir);
            }
            catch (Exception ex)
            {
                SetStatus("Load failed: " + ex.Message);
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

        private void OnGenerateClicked(UIElement _)
        {
            var dlg = new UIGenerateMapDialog(p =>
            {
                try
                {
                    CityProcGen.Generate(_City.MapData, p);
                    // Re-bake the vertex color tint texture in-memory so
                    // the elevation-driven lush/dry color gradient shows
                    // immediately. Otherwise the visible tint stays
                    // whatever was loaded from disk until next Save+Load.
                    CityBaker.UpdateLiveVertexColor(_City);
                    RefreshMapDisplay();
                    SetStatus("Generated " + p.Type + " (seed " + p.Seed + ")");
                }
                catch (Exception ex)
                {
                    SetStatus("Generate failed: " + ex.Message);
                }
            });
            UIScreen.GlobalShowDialog(dlg, true);
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
                RefreshMapDisplay();
                SetStatus("Map cleared");
            }
            catch (Exception ex)
            {
                SetStatus("Clear failed: " + ex.Message);
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            UpdateStatusFade();
            RefreshIfChanged();
        }

        // Toast-style fade: hold, fade out, clear. Called every frame; a
        // no-op once the label is empty.
        private void UpdateStatusFade()
        {
            if (string.IsNullOrEmpty(_StatusLabel.Caption)) return;
            double elapsed = (DateTime.Now - _StatusSetAt).TotalSeconds;
            if (elapsed < STATUS_HOLD_SECONDS)
            {
                _StatusLabel.Opacity = 1f;
            }
            else if (elapsed < STATUS_HOLD_SECONDS + STATUS_FADE_SECONDS)
            {
                float t = (float)((elapsed - STATUS_HOLD_SECONDS) / STATUS_FADE_SECONDS);
                _StatusLabel.Opacity = 1f - t;
            }
            else
            {
                _StatusLabel.Caption = "";
                _StatusLabel.Opacity = 1f;
            }
        }

        private void SetStatus(string text)
        {
            _StatusLabel.Caption = text;
            _StatusLabel.Opacity = 1f;
            _StatusSetAt = DateTime.Now;
        }

        /// <summary>
        /// True when the given point (in toolbar-local coords, which
        /// equal screen coords because the toolbar sits at 0,0) lands
        /// on top of any visible UIButton. Used by CityEditorScreen to
        /// suppress city-renderer mouse handling only directly under
        /// buttons — gaps between buttons stay paintable.
        /// </summary>
        public bool IsPointOverButton(float x, float y)
        {
            foreach (var child in GetChildren())
            {
                var btn = child as UIButton;
                if (btn == null || !btn.Visible) continue;
                var b = btn.GetBounds();
                if (x >= btn.X && x < btn.X + b.Width &&
                    y >= btn.Y && y < btn.Y + b.Height)
                    return true;
            }
            return false;
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