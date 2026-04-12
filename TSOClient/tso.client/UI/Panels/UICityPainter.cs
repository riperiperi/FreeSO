using FSO.Client.Rendering.City;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.Rendering.City.Plugins.PainterModes;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using XnaMatrix = Microsoft.Xna.Framework.Matrix;

namespace FSO.Client.UI.Panels
{
    internal readonly struct UICityPainterToolMode
    {
        public readonly string Graphic;
        public readonly int CaptionID;
        public readonly int ModeValue;

        public UICityPainterToolMode(string graphic, int captionId, int modeValue)
        {
            Graphic = graphic;
            CaptionID = captionId;
            ModeValue = modeValue;
        }
    }

    internal readonly struct UICityPainterToolToggle
    {
        public readonly string Graphic;
        public readonly int CaptionID;
        public readonly Func<bool> Get;
        public readonly Action<bool> Set;

        public UICityPainterToolToggle(string graphic, int captionId, Func<bool> get, Action<bool> set)
        {
            Graphic = graphic;
            CaptionID = captionId;
            Get = get;
            Set = set;
        }
    }

    internal abstract class AbstractCityPainterPreview : UIElement
    {
        private Vector2 _Size;
        public override Vector2 Size { get => _Size; set => _Size = value; }

        protected UICityPainter Painter { get; private set; }
        protected MapPainterPlugin MapPainter => Painter.MapPainter;

        private XnaMatrix TileMatrix;
        private float TileScale;

        public AbstractCityPainterPreview()
        {
        }

        public virtual void Init(UICityPainter painter)
        {
            Painter = painter;
        }

        protected Texture2D LoadFSOTex(string name)
        {
            string path = Path.Combine("Content/Textures/terrain/", name);

            return TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, path);
        }

        protected Texture2D[] LoadFSOTex(ReadOnlySpan<string> names)
        {
            var result = new Texture2D[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                result[i] = LoadFSOTex(names[i]);
            }

            return result;
        }

        protected Texture2D LoadTSOTex(string path)
        {
            string gamepath = GameFacade.GameFilePath($"gamedata/{path}");

            return TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, gamepath);
        }

        protected Texture2D[] LoadTSOTex(ReadOnlySpan<string> paths)
        {
            var result = new Texture2D[paths.Length];

            for (int i = 0; i < paths.Length; i++)
            {
                result[i] = LoadTSOTex(paths[i]);
            }

            return result;
        }

        private (XnaMatrix, float) GetTileSpaceMatrix(Point size)
        {
            var diagSize = (size.X + size.Y) / 2f;
            float diag = MathF.Sqrt(2);

            float scale = 1 / diagSize;

            float tileWidth = scale * 128 / diag;

            return (
                XnaMatrix.CreateTranslation(new Vector3(-0.5f, -0.5f, 0)) *
                XnaMatrix.CreateRotationZ(MathF.PI / 4f) *
                XnaMatrix.CreateScale(new Vector3(tileWidth, tileWidth / 2, 1)),
                scale);
        }

        protected void PrepareTileMatrix(Point size)
        {
            (TileMatrix, TileScale) = GetTileSpaceMatrix(size);
        }

        protected (Vector2, Vector2) GetTilePosition(int x, int y, int width, int height)
        {
            var ctr = Vector2.Transform(new Vector2(x + 0.5f, y + 0.5f), TileMatrix) + (Size / 2f);
            var scale = TileScale;

            return (ctr - new Vector2(width / 2, height - 32) * scale, new Vector2(scale, scale));
        }

        protected void BeginTile(UISpriteBatch batch, Point size)
        {
            batch.Pause();
            // Calculate a new matrix in tile space starting at the center of this component.

            var trueScale = Scale;
            var trueCenter = LocalPoint(Size.X / 2f, Size.Y / 2f) / Scale;

            var toCenter = XnaMatrix.CreateTranslation(new Vector3(trueCenter, 0)) * XnaMatrix.CreateScale(new Vector3(trueScale, 1));

            var mat = GetTileSpaceMatrix(size).Item1 * toCenter;

            batch.Begin(transformMatrix: mat);
        }

        protected void EndTile(UISpriteBatch batch)
        {
            batch.End();
            batch.Resume();
        }

        protected void DrawLine(UISpriteBatch batch, Vector3 from, Vector3 to, float lineWidth, Color color)
        {
            var px = TextureGenerator.GetPxWhite(batch.GraphicsDevice);

            float heightScale = -32f * TileScale;

            var fromScreen = Vector2.Transform(new Vector2(from.X, from.Y), TileMatrix) + new Vector2(0, from.Z * heightScale);
            var toScreen = Vector2.Transform(new Vector2(to.X, to.Y), TileMatrix) + new Vector2(0, to.Z * heightScale);

            var hSize = Size / 2;
            var fromOrigin = LocalPoint(fromScreen + hSize);
            var toOrigin = LocalPoint(toScreen + hSize);
            var dir = toOrigin - fromOrigin;
            var dist = dir.Length();

            float rotation = (float)Math.Atan2(dir.Y, dir.X);

            batch.Draw(px, fromOrigin - new Vector2(0, lineWidth/-2), null, color, rotation, new Vector2(0, 0.5f), new Vector2(dist, lineWidth), SpriteEffects.None, 0);
        }
    }

    internal abstract class AbstractCityPainterOptions : UIContainer
    {
        protected UICityPainter Painter { get; private set; }
        private MapPainterPlugin MapPainter => Painter.MapPainter;
        public abstract PainterMode Mode { get; }

        private UIHBoxContainer ModesHbox;
        private UIHBoxContainer TogglesHbox;
        private UIHBoxContainer RootHbox;
        private (UIButton, UICityPainterToolMode)[] Modes;
        private (UIButton, UICityPainterToolToggle)[] Toggles;

        public AbstractCityPainterOptions()
        {
            RootHbox = new UIHBoxContainer();
            ModesHbox = new UIHBoxContainer();
            TogglesHbox = new UIHBoxContainer();

            RootHbox.Add(ModesHbox);
            RootHbox.Add(TogglesHbox);

            Add(RootHbox);
        }

        public virtual void Init(UICityPainter painter)
        {
            Painter = painter;
        }

        private void UpdateSelectedMode()
        {
            var modes = Modes;

            if (modes != null)
            {
                for (int i = 0; i < modes.Length; i++)
                {
                    var mode = modes[i];

                    mode.Item1.Selected = MapPainter.SelectedModifier == mode.Item2.ModeValue;
                }
            }
        }

        protected void SetModes(ReadOnlySpan<UICityPainterToolMode> modes)
        {
            var result = new (UIButton, UICityPainterToolMode)[modes.Length];

            var strings = GameFacade.Strings;

            for (int i = 0; i < modes.Length; i++)
            {
                var mode = modes[i];

                var button = new UIButton() { Caption = strings.GetString("f130", mode.CaptionID.ToString()) };
                button.OnButtonClick += (btn) =>
                {
                    MapPainter.SelectedModifier = mode.ModeValue;
                    UpdateSelectedMode();
                };

                ModesHbox.Add(button);

                result[i] = (button, mode);
            }

            Modes = result;
            ModesHbox.AutoSize();
            RootHbox.AutoSize();

            UpdateSelectedMode();
        }

        protected void SetToggles(ReadOnlySpan<UICityPainterToolToggle> toggles)
        {
            var result = new (UIButton, UICityPainterToolToggle)[toggles.Length];

            var strings = GameFacade.Strings;

            for (int i = 0; i < toggles.Length; i++)
            {
                var toggle = toggles[i];

                var button = new UIButton() { Caption = strings.GetString("f130", toggle.CaptionID.ToString()), Selected = toggle.Get() };
                button.OnButtonClick += (btn) =>
                {
                    var value = toggle.Get();
                    toggle.Set(!value);
                    button.Selected = !value;
                };

                TogglesHbox.Add(button);

                result[i] = (button, toggle);
            }

            TogglesHbox.AutoSize();
            RootHbox.AutoSize();

            Toggles = result;
        }
    }

    internal class UICityPainterElevationPreview : AbstractCityPainterPreview
    {
        public override void Draw(UISpriteBatch batch)
        {
            // Draw a grid representing the elevation change

            int tileCount = MapPainter.BrushSize * 2 + 2;
            int vertCount = tileCount + 1;
            PrepareTileMatrix(new Point(tileCount));

            bool[] tileTouched = new bool[tileCount * tileCount];
            float[] vertices = new float[vertCount * vertCount];
            int center = vertCount / 2;

            float baseSize = MapPainter.BrushSize + 0.5f;
            var multiplier = MathF.Pow(baseSize, 0.8f) * ((MapPainter.Accelerate) ? 6 : 3);

            if (MapPainter.Erasing)
            {
                multiplier *= -1;
            }

            IMapPainterMode.BrushFunc(MapPainter.BrushSize, (x, y, strength) =>
            {
                if (strength > 0)
                {
                    vertices[(y + center) * vertCount + x + center] = strength * multiplier;
                }
            });

            Vector3 offset = new Vector3(-baseSize, -baseSize, 0);
            Color baseColor = MapPainter.Erasing ? Color.Red : Color.White;

            for (int y = 0; y < tileCount; y++)
            {
                for (int x = 0; x < tileCount; x++)
                {
                    Vector3 v1 = new Vector3(x, y, vertices[y * vertCount + x]) + offset;
                    Vector3 v2 = new Vector3(x + 1, y, vertices[y * vertCount + x + 1]) + offset;
                    Vector3 v3 = new Vector3(x, y + 1, vertices[(y + 1) * vertCount + x]) + offset;
                    Vector3 v4 = new Vector3(x + 1, y + 1, vertices[(y + 1) * vertCount + x + 1]) + offset;

                    float mag = (v1.Z + v2.Z + v3.Z + v4.Z) / 4;

                    Color color = baseColor * Math.Min(1f, Math.Abs(mag * 0.5f));

                    DrawLine(batch, v1, v2, 2, color);
                    DrawLine(batch, v3, v4, 2, color);
                    DrawLine(batch, v1, v3, 2, color);
                    DrawLine(batch, v2, v4, 2, color);
                }
            }
        }
    }

    internal class UICityPainterElevationOptions : AbstractCityPainterOptions
    {
        public override PainterMode Mode => PainterMode.ELEVATION_CIRCLE;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);
            SetModes([]);
            SetToggles([
                new ("cedit_auto.png", 20, () => false, (value) => { }),
                new ("cedit_flat.png", 21, () => false, (value) => { }),
                new ("cedit_rough.png", 22, () => false, (value) => { })
            ]);
        }
    }

    internal class UICityPainterTerrainTypePreview : AbstractCityPainterPreview
    {
        public Texture2D[] TerrainTextures;
        public override void Init(UICityPainter painter)
        {
            base.Init(painter);

            TerrainTextures = LoadTSOTex([
                "terrain/newformat/gr.tga",
                "terrain/newformat/wt.tga",
                "terrain/newformat/rk.tga",
                "terrain/newformat/sn.tga",
                "terrain/newformat/sd.tga",
                ]);
        }

        private int PosMod(int x, int m)
        {
            return (x % m + m) % m;
        }

        public override void Draw(UISpriteBatch batch)
        {
            // Draw the terrain brush result

            var size = MapPainter.BrushSize;

            var tex = TerrainTextures[MapPainter.SelectedModifier];
            var texSegment = new Point(tex.Width / 4, tex.Height / 4);

            BeginTile(batch, new Point(size * 2 + 1));
            IMapPainterMode.BrushFunc(size, (x, y, strength) =>
            {
                var multiplier = (MapPainter.Accelerate) ? 2 : 1;
                if (strength > 0)
                {
                    batch.Draw(tex, new Rectangle(x, y, 1, 1), new Rectangle(PosMod(x, 4) * texSegment.X, PosMod(y, 4) * texSegment.Y, texSegment.X, texSegment.Y), Color.White);
                }
            });

            EndTile(batch);
        }
    }

    internal class UICityPainterTerrainTypeOptions : AbstractCityPainterOptions
    {
        public override PainterMode Mode => PainterMode.TERRAINTYPE;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);
            SetModes([
                new ("cedit_grass.png", 30, 0),
                new ("cedit_water.png", 31, 1),
                new ("cedit_rock.png", 32, 2),
                new ("cedit_snow.png", 33, 3),
                new ("cedit_sand.png", 34, 4)
            ]);
            SetToggles([]);
        }
    }

    internal class UICityPainterRoadsPreview : AbstractCityPainterPreview
    {
        private Texture2D[] RoadTilePreview;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);

            RoadTilePreview = LoadFSOTex([
                "roadcorner02.png",
                "roadcorner04.png",
                "road01.png",
                "road04.png",
                "roadcorner01.png",
                "roadcorner08.png",
            ]);
        }

        public override void Draw(UISpriteBatch batch)
        {
            // Just draw a road at the middle.

            var erasing = MapPainter.Erasing;
            var tint = erasing ? Color.White * 0.5f : Color.White;

            BeginTile(batch, new Point(3, 3));

            for (int i = 0; i < RoadTilePreview.Length; i++)
            {
                int x = i % 2;
                int y = i / 2;
                batch.Draw(RoadTilePreview[i], new Rectangle((x * 2) - 1, y * 2 - 2, 2, 2), tint);
            }

            EndTile(batch);

            if (MapPainter.Erasing)
            {
                PrepareTileMatrix(new Point(3, 3));

                var roadSize = 0.22f;

                var v1 = new Vector3(1 - roadSize, 0 - roadSize, 0);
                var v2 = new Vector3(1 + roadSize, 0 - roadSize, 0);
                var v3 = new Vector3(1 + roadSize, 2 + roadSize, 0);
                var v4 = new Vector3(1 - roadSize, 2 + roadSize, 0);

                Color color = Color.Red;

                DrawLine(batch, v1, v2, 2, color);
                DrawLine(batch, v2, v3, 2, color);
                DrawLine(batch, v3, v4, 2, color);
                DrawLine(batch, v4, v1, 2, color);
            }
        }
    }

    internal class UICityPainterRoadsOptions : AbstractCityPainterOptions
    {
        public override PainterMode Mode => PainterMode.ROAD;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);
            SetModes([]);
            SetToggles([]);
        }
    }

    internal class UICityPainterForestsPreview : AbstractCityPainterPreview
    {
        private Texture2D Forests;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);

            Forests = LoadTSOTex("farzoom/forest00a.tga");
        }

        public override void Draw(UISpriteBatch batch)
        {
            // Draw forests based on the brush and the type
            // If it's erasing then draw a red grid under them

            var fw = Forests.Width / 4;
            var fh = Forests.Height / 4;
            float intensityF = 1f;
            int intensity = Math.Clamp((int)MathF.Floor(intensityF * 4), 0, 3);
            int type = MapPainter.SelectedModifier;

            var size = MapPainter.BrushSize;

            Color tint = Color.White;

            PrepareTileMatrix(new Point(size * 2 + 1));

            if (MapPainter.Erasing)
            {
                tint *= 0.5f;

                IMapPainterMode.BrushFunc(size, (x, y, strength) =>
                {
                    var multiplier = (MapPainter.Accelerate) ? 2 : 1;
                    if (strength > 0)
                    {
                        var v1 = new Vector3(x, y, 0);
                        var v2 = new Vector3(x + 1, y, 0);
                        var v3 = new Vector3(x + 1, y + 1, 0);
                        var v4 = new Vector3(x, y + 1, 0);

                        Color color = Color.Red * Math.Min(1f, strength + 0.5f);

                        DrawLine(batch, v1, v2, 2, color);
                        DrawLine(batch, v2, v3, 2, color);
                        DrawLine(batch, v3, v4, 2, color);
                        DrawLine(batch, v4, v1, 2, color);
                    }
                });
            }

            IMapPainterMode.BrushFunc(size, (x, y, strength) =>
            {
                var multiplier = (MapPainter.Accelerate) ? 2 : 1;
                if (strength > 0)
                {
                    var src = new Rectangle(intensity * fw, type * fh, fw, fh);
                    var dst = GetTilePosition(x, y, fw, fh);

                    DrawLocalTexture(batch, Forests, src, dst.Item1, dst.Item2, tint);
                }
            });
        }
    }

    internal class UICityPainterForestsOptions : AbstractCityPainterOptions
    {
        public override PainterMode Mode => PainterMode.FORESTTYPE;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);
            SetModes([
                new ("cedit_heavy.png", 40, 0),
                new ("cedit_light.png", 41, 1),
                new ("cedit_cacti.png", 42, 2),
                new ("cedit_palm.png", 43, 3),
            ]);
            SetToggles([]);
        }
    }

    internal class UICityPainter : UIDialog
    {
        private struct ModeUI
        {
            public readonly UIButton TabButton;
            public readonly AbstractCityPainterOptions Options;
            public readonly AbstractCityPainterPreview Preview;

            public ModeUI(UIButton tabButton, AbstractCityPainterOptions options, AbstractCityPainterPreview preview)
            {
                TabButton = tabButton;
                Options = options;
                Preview = preview;
            }
        }

        public readonly MapPainterPlugin MapPainter;
        private readonly ModeUI[] Modes;

        private readonly UIButton PreviewBg;
        private readonly UIHBoxContainer RootBox;
        private readonly UIVBoxContainer SharedControlsBox;
        private readonly UIVBoxContainer ToolsBox;
        private AbstractCityPainterPreview ActivePreview;

        private readonly UISlider BrushSizeSlider;
        private readonly UISlider BrushIntensitySlider;

        private readonly Terrain Terrain;

        public UICityPainter(Terrain terrain) : base(UIDialogStyle.Close, true)
        {
            Terrain = terrain;
            MapPainter = new MapPainterPlugin(terrain);

            Modes = [
                GenerateMode<UICityPainterElevationOptions, UICityPainterElevationPreview>(0),
                GenerateMode<UICityPainterTerrainTypeOptions, UICityPainterTerrainTypePreview>(1),
                GenerateMode<UICityPainterRoadsOptions, UICityPainterRoadsPreview>(2),
                GenerateMode<UICityPainterForestsOptions, UICityPainterForestsPreview>(3),
            ];

            PreviewBg = new UIButton(GetTexture(0x0000079300000001));

            RootBox = new UIHBoxContainer()
            {
                VerticalAlignment = UIContainerVerticalAlignment.Middle
            };

            SharedControlsBox = new UIVBoxContainer()
            {
                HorizontalAlignment = UIContainerHorizontalAlignment.Center
            };

            SharedControlsBox.Add(PreviewBg);
            SharedControlsBox.Add(BrushSizeSlider = new UISlider()
            {
                Orientation = 0,
                Texture = GetTexture(0x42500000001),
                MinValue = 0f,
                MaxValue = 25f,
                Value = 0,
                AllowDecimals = false,
            });
            BrushSizeSlider.SetSize(140f, 12f);
            BrushSizeSlider.OnChange += (slider) =>
            {
                MapPainter.BrushSize = (int)BrushSizeSlider.Value;
            };

            SharedControlsBox.Add(BrushIntensitySlider = new UISlider()
            {
                Orientation = 0,
                Texture = GetTexture(0x42500000001),
                MinValue = 0f,
                MaxValue = 10f,
                AllowDecimals = true,
            });
            BrushIntensitySlider.SetSize(140f, 12f);
            BrushIntensitySlider.OnChange += (slider) =>
            {
                // TODO
            };

            SharedControlsBox.AutoSize();

            RootBox.Add(SharedControlsBox);

            ToolsBox = new UIVBoxContainer();

            var modesBox = new UIHBoxContainer();
            foreach (var mode in Modes)
            {
                modesBox.Add(mode.TabButton);
            }
            modesBox.AutoSize();
            ToolsBox.Add(modesBox);

            ToolsBox.AutoSize();
            RootBox.Add(ToolsBox);

            RootBox.AutoSize();

            RootBox.Position = new Vector2(20, 35);
            SetSize(600, (int)RootBox.Size.Y + 60);

            Add(RootBox);

            CloseButton.OnButtonClick += Close;

            SetMode(PainterMode.ROAD);
        }

        private void Close(UIElement button)
        {
            SetActive(false);
        }

        public void SetActive(bool active)
        {
            if (active)
            {
                Visible = true;
                Terrain.Plugin = MapPainter;
            }
            else
            {
                Visible = false;
                Terrain.Plugin = null;
            }
        }

        private void SetMode(PainterMode mode)
        {
            var children = ToolsBox.GetChildren();
            while (children.Count > 1)
            {
                ToolsBox.Remove(children.Last());
            }

            if (ActivePreview != null)
            {
                DynamicOverlay.Remove(ActivePreview);
            }

            int index = Array.FindIndex(Modes, (ui) => ui.Options.Mode == mode);

            if (index == -1)
            {
                return;
            }

            for (int i = 0; i < Modes.Length; i++)
            {
                Modes[i].TabButton.Selected = i == index;
            }

            ref var ui = ref Modes[index];

            // Put the options and preview in the UI.

            ToolsBox.Add(ui.Options);
            ToolsBox.AutoSize();
            RootBox.AutoSize();

            var preview = ui.Preview;
            preview.Position = RootBox.Position + PreviewBg.Position;
            preview.Size = PreviewBg.Size;
            DynamicOverlay.Add(preview);

            ActivePreview = preview;

            MapPainter.SwitchMode(mode);
        }

        private ModeUI GenerateMode<TOptions, TPreview>(int index) where TOptions : AbstractCityPainterOptions, new() where TPreview : AbstractCityPainterPreview, new()
        {
            var button = new UIButton();
            var options = new TOptions();
            var preview = new TPreview();

            var strings = GameFacade.Strings;

            button.Caption = strings.GetString("f130", (index + 2).ToString());
            button.OnButtonClick += (btn) => SetMode(options.Mode);

            options.Init(this);
            preview.Init(this);
            return new ModeUI(
                button,
                options,
                preview
                );
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            BrushSizeSlider.Value = MapPainter.BrushSize;
        }
    }
}
