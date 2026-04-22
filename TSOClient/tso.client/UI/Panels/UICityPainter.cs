using FSO.Client.Rendering.City;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.Rendering.City.Plugins.PainterModes;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.HIT;
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

    internal readonly struct UICityPainterIntensityConfig
    {
        public readonly bool Disable;
        public readonly float Min;
        public readonly float Max;
        public readonly bool AllowDecimal;

        public UICityPainterIntensityConfig()
        {
            Disable = true;
        }

        public UICityPainterIntensityConfig(float min, float max, bool allowDecimal)
        {
            Disable = false;
            Min = min;
            Max = max;
            AllowDecimal = allowDecimal;
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
        protected UICityPainterIntensityConfig DisabledIntensity = new UICityPainterIntensityConfig();
        protected UICityPainterIntensityConfig DefaultIntensity = new UICityPainterIntensityConfig(0.1f, 1, true);

        protected UICityPainter Painter { get; private set; }
        protected MapPainterPlugin MapPainter => Painter.MapPainter;
        public abstract PainterMode Mode { get; }
        public abstract string Graphic { get; }
        public abstract string PreviewText { get; }
        public abstract UICityPainterIntensityConfig IntensityConfig { get; }

        private UIHBoxContainer ModesHbox;
        private UIHBoxContainer TogglesHbox;
        private UIHBoxContainer RootHbox;
        private (UIButton, UICityPainterToolMode)[] Modes;
        private (UIButton, UICityPainterToolToggle)[] Toggles;

        private int SelectedMode = 0;
        protected float SelectedIntensity = 0.5f;

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

        private void UpdateToggles()
        {
            var toggles = Toggles;

            if (toggles != null)
            {
                for (int i = 0; i < toggles.Length; i++)
                {
                    var toggle = toggles[i];

                    toggle.Item1.Selected = toggle.Item2.Get();
                }
            }
        }

        public virtual void Selected()
        {
            MapPainter.SelectedModifier = SelectedMode;
            MapPainter.BrushIntensity = SelectedIntensity;

            UpdateSelectedMode();
            UpdateToggles();
        }

        protected void SetModes(ReadOnlySpan<UICityPainterToolMode> modes)
        {
            var result = new (UIButton, UICityPainterToolMode)[modes.Length];

            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;
            var strings = GameFacade.Strings;

            var buttonSeat = ui.Get("neighp_btab_seat.png").Get(gd);
            var position = new Vector2(14, 10);
            var seatOff = new Vector2(3, 3);

            for (int i = 0; i < modes.Length; i++)
            {
                var mode = modes[i];

                var seat = new UIImage(buttonSeat)
                {
                    Position = position
                };

                var button = new UIButton()
                {
                    Texture = ui.Get($"cityedit_tool_{mode.Graphic}.png").Get(gd),
                    Tooltip = strings.GetString("f130", mode.CaptionID.ToString()),
                    Position = position + seatOff
                };
                button.OnButtonClick += (btn) =>
                {
                    MapPainter.SelectedModifier = mode.ModeValue;
                    SelectedMode = mode.ModeValue;
                    UpdateSelectedMode();
                };

                Add(seat);
                Add(button);

                position.X += 33;
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

            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;
            var strings = GameFacade.Strings;

            var buttonSeat = ui.Get("neighp_btab_seat.png").Get(gd);
            var position = new Vector2(204, 10);
            var seatOff = new Vector2(3, 3);

            for (int i = 0; i < toggles.Length; i++)
            {
                var toggle = toggles[i];

                var seat = new UIImage(buttonSeat)
                {
                    Position = position
                };

                var button = new UIButton()
                {
                    Texture = ui.Get($"cityedit_tool_{toggle.Graphic}.png").Get(gd),
                    Tooltip = strings.GetString("f130", toggle.CaptionID.ToString()),
                    Selected = toggle.Get(),
                    Position = position + seatOff
                };

                button.OnButtonClick += (btn) =>
                {
                    var value = toggle.Get();
                    toggle.Set(!value);
                    button.Selected = !value;
                };

                Add(seat);
                Add(button);

                position.X -= 33;

                result[i] = (button, toggle);
            }

            TogglesHbox.AutoSize();
            RootHbox.AutoSize();

            Toggles = result;
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            SelectedIntensity = MapPainter.BrushIntensity;
        }
    }

    internal class UICityPainterElevationPreview : AbstractCityPainterPreview
    {
        public MapPainterSpraypaint Spray;

        public override void Init(UICityPainter painter)
        {
            Spray = new MapPainterSpraypaint(true);

            base.Init(painter);
        }

        public override void Draw(UISpriteBatch batch)
        {
            // Draw a grid representing the elevation change

            int tileCount = MapPainter.BrushSize * 2 + 2;
            int vertCount = tileCount + 1;
            PrepareTileMatrix(new Point(tileCount));

            bool[] tileTouched = new bool[tileCount * tileCount];
            float[] vertices = new float[vertCount * vertCount];
            float[] intensityVertices = vertices;
            int center = vertCount / 2;

            float baseSize = MapPainter.BrushSize + 0.5f;
            var multiplier = MathF.Pow(baseSize, 0.8f) * ((MapPainter.Accelerate) ? 8 : 4);

            var erasing = !MapPainter.Flatten && MapPainter.Erasing;

            if (erasing)
            {
                multiplier *= -1;
            }

            float intensity = 1;
            if (MapPainter.Flatten)
            {
                int vi = 0;
                for (int y = 0; y < vertCount; y++)
                {
                    for (int x = 0; x < vertCount; x++)
                    {
                        vertices[vi++] = (y - vertCount / 2f) * -0.5f;
                    }
                }

                multiplier = 50;
                intensityVertices = new float[vertCount * vertCount];
                var centerElev = 0;

                IMapPainterMode.BrushFunc(MapPainter.BrushSize, (x, y, strength) =>
                {
                        if (strength > 0)
                        {
                            int vertInd = (y + center) * vertCount + x + center;
                            var elev = vertices[vertInd];

                            var change = (centerElev - elev) / 50f * multiplier;
                            if (change > 0) change = Math.Max(0.02f, change);
                            else change = Math.Min(-0.02f, change);

                            vertices[vertInd] += change;
                            intensityVertices[vertInd] = Math.Max(Math.Abs(change), strength);
                        }
                });
            }
            else
            {
                intensity = MapPainter.BrushIntensity;
                multiplier *= intensity;
                IMapPainterMode.BrushFunc(MapPainter.BrushSize, (x, y, strength) =>
                {
                    if (strength > 0)
                    {
                        if (MapPainter.RoughTerrain)
                        {
                            strength = Spray.GetRoughEdge((256 + y) * 512 + 256 + x, strength, MapPainter.BrushSize);
                        }

                        vertices[(y + center) * vertCount + x + center] = strength * multiplier;
                    }
                });
            }

            Vector3 offset = new Vector3(-baseSize, -baseSize, 0);
            Color baseColor = erasing ? Color.Red : Color.White;

            for (int y = 0; y < tileCount; y++)
            {
                for (int x = 0; x < tileCount; x++)
                {
                    Vector3 v1 = new Vector3(x, y, vertices[y * vertCount + x]) + offset;
                    Vector3 v2 = new Vector3(x + 1, y, vertices[y * vertCount + x + 1]) + offset;
                    Vector3 v3 = new Vector3(x, y + 1, vertices[(y + 1) * vertCount + x]) + offset;
                    Vector3 v4 = new Vector3(x + 1, y + 1, vertices[(y + 1) * vertCount + x + 1]) + offset;

                    float e1 = intensityVertices[y * vertCount + x];
                    float e2 = intensityVertices[y * vertCount + x + 1];
                    float e3 = intensityVertices[(y + 1) * vertCount + x];
                    float e4 = intensityVertices[(y + 1) * vertCount + x + 1];

                    float mag = (e1 + e2 + e3 + e4) / 4;

                    Color color = baseColor * Math.Min(1f, Math.Abs(mag / multiplier));

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
        public override string Graphic => "elevation";
        public override string PreviewText => GameFacade.Strings.GetString("f130", "2");
        public override UICityPainterIntensityConfig IntensityConfig => DefaultIntensity;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);
            SetModes([]);
            SetToggles([
                new ("auto", 20, () => MapPainter.AutoTerrain, (value) => { MapPainter.AutoTerrain = value; }),
                new ("flat", 21, () => MapPainter.Flatten, (value) => { MapPainter.Flatten = value; }),
                new ("rough", 22, () => MapPainter.RoughTerrain, (value) => { MapPainter.RoughTerrain = value; })
            ]);
        }
    }

    internal class UICityPainterTerrainTypePreview : AbstractCityPainterPreview
    {
        public Texture2D[] TerrainTextures;
        public MapPainterSpraypaint Spray;
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

            Spray = new MapPainterSpraypaint(true);
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
            var spray = MapPainter.SprayBrush;
            var intensity = MapPainter.BrushIntensity * 0.8f + 0.2f; // Small bias to assist the display.

            BeginTile(batch, new Point(size * 2 + 1));
            IMapPainterMode.BrushFunc(size, (x, y, strength) =>
            {
                var multiplier = (MapPainter.Accelerate) ? 2 : 1;

                if (spray)
                {
                    var brushIntensity = Spray.GetSpraypaint((256 + y) * 512 + 256 + x, strength) * intensity * multiplier;
                    strength = brushIntensity - 0.3f;
                }

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
        public override string Graphic => "ttype";
        public override string PreviewText => GameFacade.Strings.GetString("f130", (30 + MapPainter.SelectedModifier).ToString());
        public override UICityPainterIntensityConfig IntensityConfig => MapPainter.SprayBrush ? DefaultIntensity : DisabledIntensity;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);
            SetModes([
                new ("grass", 30, 0),
                new ("water", 31, 1),
                new ("rock", 32, 2),
                new ("snow", 33, 3),
                new ("sand", 34, 4)
            ]);
            SetToggles([
                new ("spray", 12, () => MapPainter.SprayBrush, (value) => { MapPainter.SprayBrush = value; MapPainter.BrushIntensity = 0.5f; }),
            ]);
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
        public override string Graphic => "road";

        public UILabel RoadLabel;
        public override string PreviewText => GameFacade.Strings.GetString("f130", "4");
        public override UICityPainterIntensityConfig IntensityConfig => DisabledIntensity;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);

            var style = TextStyle.DefaultLabel.Clone();
            style.Shadow = true;

            RoadLabel = new UILabel
            {
                Caption = GameFacade.Strings.GetString("f130", "17"),
                Size = new Vector2(248, 51),
                Alignment = TextAlignment.Center | TextAlignment.Middle,
                CaptionStyle = style
            };
            Add(RoadLabel);

            SetModes([]);
            SetToggles([]);
        }
    }

    internal class UICityPainterForestsPreview : AbstractCityPainterPreview
    {
        private Texture2D Forests;
        public MapPainterSpraypaint Spray;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);

            Forests = LoadTSOTex("farzoom/forest00a.tga");
            Spray = new MapPainterSpraypaint(true);
        }

        public override void Draw(UISpriteBatch batch)
        {
            // Draw forests based on the brush and the type
            // If it's erasing then draw a red grid under them

            var fw = Forests.Width / 4;
            var fh = Forests.Height / 4;
            float intensityS = MapPainter.BrushIntensity;
            float intensityF = MapPainter.BrushIntensity - 1;
            int intensity = Math.Clamp((int)MathF.Round(intensityF), 0, 3);
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

            var spray = MapPainter.SprayBrush;

            IMapPainterMode.BrushFunc(size, (x, y, strength) =>
            {
                var multiplier = (MapPainter.Accelerate) ? 2 : 1;

                if (spray)
                {
                    var brushIntensity = Spray.GetSpraypaint((256 + y) * 512 + 256 + x, strength) * intensityS * multiplier;
                    intensity = Math.Clamp((int)MathF.Round(brushIntensity * 8), 0, 4) - 1;

                    if (intensity >= 0)
                    {
                        var src = new Rectangle(intensity * fw, type * fh, fw, fh);
                        var dst = GetTilePosition(x, y, fw, fh);

                        DrawLocalTexture(batch, Forests, src, dst.Item1, dst.Item2, tint);
                    }
                }
                else
                {
                    if (strength > 0)
                    {
                        var src = new Rectangle(intensity * fw, type * fh, fw, fh);
                        var dst = GetTilePosition(x, y, fw, fh);

                        DrawLocalTexture(batch, Forests, src, dst.Item1, dst.Item2, tint);
                    }
                }
            });
        }
    }

    internal class UICityPainterForestsOptions : AbstractCityPainterOptions
    {
        private UICityPainterIntensityConfig NonSprayIntensity = new UICityPainterIntensityConfig(1, 4, false);
        public override PainterMode Mode => PainterMode.FOREST;
        public override string Graphic => "forests";
        public override string PreviewText => GameFacade.Strings.GetString("f130", (40 + MapPainter.SelectedModifier).ToString());
        public override UICityPainterIntensityConfig IntensityConfig => MapPainter.SprayBrush ? DefaultIntensity : NonSprayIntensity;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);
            SetModes([
                new ("heavy", 40, 0),
                new ("light", 41, 1),
                new ("cacti", 42, 2),
                new ("palm", 43, 3),
            ]);
            SetToggles([
                new ("spray", 12, () => MapPainter.SprayBrush, (value) =>
                { 
                    MapPainter.SprayBrush = value;

                    MapPainter.BrushIntensity = value ? 4f : 0.5f;
                }),
            ]);
        }
    }

    internal class UICityPainter : UIContainer
    {
        private const float ThumbDisplayDuration = 3.5f;
        private const float ThumbDisplayFade = 1;
        private const float ThumbFlashDuration = 0.2f;
        private struct ModeUI
        {
            public readonly UIImage TabBackground;
            public readonly UIButton TabButton;
            public readonly AbstractCityPainterOptions Options;
            public readonly AbstractCityPainterPreview Preview;

            public ModeUI(UIImage tabBackground, UIButton tabButton, AbstractCityPainterOptions options, AbstractCityPainterPreview preview)
            {
                TabBackground = tabBackground;
                TabButton = tabButton;
                Options = options;
                Preview = preview;
            }
        }

        public UIImage BackgroundImage { get; set; }
        public UIButton DialogNameButton { get; set; }
        public UIButton CloseButton { get; set; }
        public UIButton LockButton { get; set; }
        public UIButton CameraButton { get; set; }

        public UIButton UndoButton { get; set; }
        public UIButton RedoButton { get; set; }

        public readonly MapPainterPlugin MapPainter;
        private readonly ModeUI[] Modes;

        private readonly UIButton PreviewBg;
        private int ActiveIndex = -1;

        private readonly UILabel BrushSizeLabel;
        private readonly UISlider BrushSizeSlider;

        private readonly UILabel BrushIntensityLabel;
        private readonly UISlider BrushIntensitySlider;

        private readonly Texture2D LockedGraphic;
        private readonly Texture2D UnlockedGraphic;
        private readonly UILabel PreviewLabel;

        private readonly Terrain Terrain;

        private readonly Vector2[] TabBackgroundPositions = [
            new Vector2(203, -5),
            new Vector2(246, -5),
            new Vector2(291, -5),
            new Vector2(336, -5)
        ];

        private RenderTarget2D CityThumbnailTarget;
        private Texture2D CityThumbnailTexture;
        private float CityThumbnailTimer;

        public UICityPainter(Terrain terrain)
        {
            Terrain = terrain;
            MapPainter = new MapPainterPlugin(terrain);

            Modes = [
                GenerateMode<UICityPainterElevationOptions, UICityPainterElevationPreview>(0),
                GenerateMode<UICityPainterTerrainTypeOptions, UICityPainterTerrainTypePreview>(1),
                GenerateMode<UICityPainterRoadsOptions, UICityPainterRoadsPreview>(2),
                GenerateMode<UICityPainterForestsOptions, UICityPainterForestsPreview>(3),
            ];

            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            Add(BackgroundImage = new UIImage(ui.Get("cityedit_bg.png").Get(gd)));

            Add(DialogNameButton = new UIButton(GetTexture(0x00000AFE00000001))
            {
                Caption = GameFacade.Strings.GetString("f130", "16"),
                Size = new Vector2(193, 18),
                Position = new Vector2(11, 8)
            });

            UIUtils.MakeDraggable(BackgroundImage, this, true);

            LockedGraphic = ui.Get("cityedit_locked.png").Get(gd);
            UnlockedGraphic = ui.Get("cityedit_unlocked.png").Get(gd);

            Add(CloseButton = new UIButton(ui.Get("neighp_closebtn.png").Get(gd))
            {
                Position = new Vector2(446, 26),
                Tooltip = GameFacade.Strings.GetString("f130", "19")
            });

            Add(LockButton = new UIButton(LockedGraphic)
            {
                Position = new Vector2(9, 46),
                Tooltip = GameFacade.Strings.GetString("f130", "18")
            });

            Add(CameraButton = new UIButton(ui.Get("cityedit_camera.png").Get(gd))
            {
                Position = new Vector2(12, 98),
                Tooltip = GameFacade.Strings.GetString("f130", "8")
            });

            Add(UndoButton = new UIButton(ui.Get("cityedit_undo.png").Get(gd))
            {
                Position = new Vector2(44, 122),
                Tooltip = GameFacade.Strings.GetString("f130", "6")
            });

            Add(RedoButton = new UIButton(ui.Get("cityedit_redo.png").Get(gd))
            {
                Position = new Vector2(176, 122),
                Tooltip = GameFacade.Strings.GetString("f130", "7")
            });

            Add(PreviewBg = new UIButton(GetTexture(0x0000079300000001))
            {
                Position = new Vector2(55, 42),
                Tooltip = GameFacade.Strings.GetString("f130", "13")
            });

            var font = TextStyle.DefaultLabel.Clone();
            font.Color = Color.White;
            font.Size = 9;
            font.Shadow = true;

            Add(PreviewLabel = new UILabel()
            {
                Position = new Vector2(67, 135),
                Size = new Vector2(109, 17),
                Alignment = TextAlignment.Center | TextAlignment.Top,
                CaptionStyle = font
            });

            (BrushSizeLabel, BrushSizeSlider) = CreateSlider(new Vector2(223, 87), 102, 10);
            (BrushIntensityLabel, BrushIntensitySlider) = CreateSlider(new Vector2(340, 87), 102, 11);

            BrushSizeSlider.Value = 0;
            BrushSizeSlider.MinValue = 0;
            BrushSizeSlider.MaxValue = 25;
            BrushSizeSlider.AllowDecimals = false;

            BrushSizeSlider.OnChange += (slider) =>
            {
                MapPainter.BrushSize = (int)BrushSizeSlider.Value;
            };

            BrushIntensitySlider.Value = 0;
            BrushIntensitySlider.MinValue = 0;
            BrushIntensitySlider.MaxValue = 10;
            BrushIntensitySlider.AllowDecimals = true;
            BrushIntensitySlider.OnChange += (slider) =>
            {
                MapPainter.BrushIntensity = BrushIntensitySlider.Value;
            };

            foreach (var mode in Modes)
            {
                mode.TabBackground.Visible = false;
                Add(mode.TabBackground);
            }

            int i = 0;
            foreach (var mode in Modes)
            {
                mode.TabButton.Position = new Vector2(226 + 45 * (i++), 8);

                Add(mode.TabButton);
            }

            UpdateLockedGraphic();
            LockButton.OnButtonClick += ToggleLock;
            CameraButton.OnButtonClick += TakeScreenshot;
            PreviewBg.OnButtonClick += InvertBrush;

            CloseButton.OnButtonClick += Close;

            SetMode(PainterMode.ROAD);
        }

        private void InvertBrush(UIElement button)
        {
            
        }

        private void UpdateLockedGraphic()
        {
            LockButton.Texture = MapPainter.LockProperties ? LockedGraphic : UnlockedGraphic;
            LockButton.Tooltip = GameFacade.Strings.GetString("f130", MapPainter.LockProperties ? "18" : "9");
        }

        private void ToggleLock(UIElement button)
        {
            MapPainter.LockProperties = !MapPainter.LockProperties;
            UpdateLockedGraphic();
        }

        private void EnsureThumbnailTarget()
        {
            CityThumbnailTarget ??= new RenderTarget2D(GameFacade.GraphicsDevice, 720, 540, false, SurfaceFormat.Color, DepthFormat.Depth24);
        }

        private void TakeScreenshot(UIElement button)
        {
            var sound = HIT.HITVM.Get().PlaySoundEvent(UISounds.CameraPhoto);
            (sound as HITThread).WriteVar(0x31, 1);

            var gd = GameFacade.GraphicsDevice;
            EnsureThumbnailTarget();

            Terrain.DrawThumbnail(gd, CityThumbnailTarget);

            CityThumbnailTexture?.Dispose();
            CityThumbnailTexture = TextureUtils.Decimate(CityThumbnailTarget, gd, 4, false);
            CityThumbnailTimer = 0;

            /*
            using (var file = File.Create("sandrise.png"))
            {
                CityThumbnailTexture.SaveAsPng(file, CityThumbnailTexture.Width, CityThumbnailTexture.Height);
            }
            */
        }

        private (UILabel, UISlider) CreateSlider(Vector2 position, float width, int stringIndex)
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            var font = TextStyle.DefaultLabel.Clone();
            font.Color = Color.White;
            font.Size = 9;
            font.Shadow = true;

            var label = new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f130", stringIndex.ToString()),
                CaptionStyle = font,
                Alignment = TextAlignment.Top | TextAlignment.Center,
                Position = position,
                Size = new Vector2(width, 1)
            };

            var slider = new UISlider()
            {
                Orientation = 0,
                Texture = ui.Get("cityedit_slider.png").Get(gd),
                Position = position + new Vector2(0, 16),
                Size = new Vector2(width, 17),
            };

            Add(label);
            Add(slider);

            return (label, slider);
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
            if (ActiveIndex != -1)
            {
                ref var activeUi = ref Modes[ActiveIndex];

                Remove(activeUi.Options);
                Remove(activeUi.Preview);
                activeUi.TabBackground.Visible = false;
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

            ui.TabBackground.Visible = true;

            var options = ui.Options;
            options.Position = new Vector2(209, 40);
            options.Size = new Vector2(248, 90);
            Add(options);

            options.Selected();

            var preview = ui.Preview;
            preview.Position = PreviewBg.Position;
            preview.Size = PreviewBg.Size;
            Add(preview);

            ActiveIndex = index;

            MapPainter.SwitchMode(mode);
        }

        private ModeUI GenerateMode<TOptions, TPreview>(int index) where TOptions : AbstractCityPainterOptions, new() where TPreview : AbstractCityPainterPreview, new()
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            var background = new UIImage(ui.Get($"cityedit_tab{index+1}.png").Get(gd));
            var options = new TOptions();
            var preview = new TPreview();
            var button = new UIButton(ui.Get($"cityedit_{options.Graphic}.png").Get(gd));

            var strings = GameFacade.Strings;

            background.Position = TabBackgroundPositions[index];

            button.Tooltip = strings.GetString("f130", (index + 2).ToString());
            button.OnButtonClick += (btn) => SetMode(options.Mode);

            options.Init(this);
            preview.Init(this);
            return new ModeUI(
                background,
                button,
                options,
                preview
                );
        }

        private void SetSliderEnabled(UISlider slider, UILabel label, bool enabled)
        {
            float opacity = enabled ? 1f : 0.5f;

            if (slider.Opacity != opacity)
            {
                slider.Opacity = opacity;
                label.Opacity = opacity;
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            BrushSizeSlider.Value = MapPainter.BrushSize;
            BrushIntensitySlider.Value = MapPainter.BrushIntensity;

            if (ActiveIndex != -1)
            {
                ref var activeUi = ref Modes[ActiveIndex];

                var label = activeUi.Options.PreviewText;
                if (PreviewLabel.Caption != label)
                {
                    PreviewLabel.Caption = label;
                }

                var intensity = activeUi.Options.IntensityConfig;

                SetSliderEnabled(BrushSizeSlider, BrushSizeLabel, activeUi.Options.Mode != PainterMode.ROAD);
                SetSliderEnabled(BrushIntensitySlider, BrushIntensityLabel, !intensity.Disable);

                if (BrushIntensitySlider.MinValue != intensity.Min) BrushIntensitySlider.MinValue = intensity.Min;
                if (BrushIntensitySlider.MaxValue != intensity.Max) BrushIntensitySlider.MaxValue = intensity.Max;
                if (BrushIntensitySlider.AllowDecimals != intensity.AllowDecimal) BrushIntensitySlider.AllowDecimals = intensity.AllowDecimal;
            }

            CityThumbnailTimer += 1f / FSOEnvironment.RefreshRate;
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);

            if (CityThumbnailTexture != null)
            {
                var white = TextureGenerator.GetPxWhite(batch.GraphicsDevice);

                var whiteCol = Color.White;
                var borderCol = Color.LightSlateGray;
                var shadowCol = Color.Black * 0.3f;
                var size = new Vector2(CityThumbnailTexture.Width, CityThumbnailTexture.Height);
                var basePos = new Vector2(31, 168);
                var shadowOffset = new Vector2(7, 7);
                var borderOffset = new Vector2(4, 4);
                var whiteOffset = new Vector2(3, 3);

                float alpha = CityThumbnailTimer > ThumbDisplayDuration ? Math.Max(0, 1 - (CityThumbnailTimer - ThumbDisplayDuration) / ThumbDisplayFade) : 1;

                if (alpha != 1)
                {
                    whiteCol *= alpha;
                    borderCol *= alpha;
                    shadowCol *= alpha;
                }

                if (alpha != 0)
                {
                    DrawLocalTexture(batch, white, null, basePos + shadowOffset - borderOffset, size + borderOffset * 2, shadowCol);
                    DrawLocalTexture(batch, white, null, basePos - borderOffset, size + borderOffset * 2, borderCol);
                    DrawLocalTexture(batch, white, null, basePos - whiteOffset, size + whiteOffset * 2, whiteCol);
                    DrawLocalTexture(batch, CityThumbnailTexture, null, basePos, Vector2.One, Color.White * alpha);

                    if (CityThumbnailTimer < ThumbFlashDuration)
                    {
                        float flashAlpha = Math.Max(0, 1 - CityThumbnailTimer / ThumbFlashDuration);
                        DrawLocalTexture(batch, white, null, basePos, size, whiteCol * flashAlpha);
                    }
                }
            }
        }
    }
}
