using FSO.Client.Controllers;
using FSO.Client.Rendering.City;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Client.UI.Panels.CityPainter.Options;
using FSO.Client.UI.Panels.CityPainter.Previews;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.Domain.Realestate;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.HIT;
using FSO.Server.Protocol.Electron.Packets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels.CityPainter
{
    internal readonly struct UICityPainterToolMode(string graphic, int captionId, int modeValue)
    {
        public readonly string Graphic = graphic;
        public readonly int CaptionID = captionId;
        public readonly int ModeValue = modeValue;
    }

    internal readonly struct UICityPainterToolToggle(string graphic, int captionId, Func<bool> get, Action<bool> set)
    {
        public readonly string Graphic = graphic;
        public readonly int CaptionID = captionId;
        public readonly Func<bool> Get = get;
        public readonly Action<bool> Set = set;
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

    internal class UICityPainter : UIContainer
    {
        private const float ThumbDisplayDuration = 3.5f;
        private const float ThumbDisplayFade = 1;
        private const float ThumbFlashDuration = 0.2f;
        private readonly struct ModeUI(UIImage tabBackground, UIButton tabButton, AbstractCityPainterOptions options, AbstractCityPainterPreview preview)
        {
            public readonly UIImage TabBackground = tabBackground;
            public readonly UIButton TabButton = tabButton;
            public readonly AbstractCityPainterOptions Options = options;
            public readonly AbstractCityPainterPreview Preview = preview;
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
        private readonly TerrainController TController;
        private readonly CityUndoStack UndoStack;

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

            TController = Terrain.FindController<TerrainController>();
            UndoStack = TController.Realestate.UndoStack;

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
                Caption = GameFacade.CurrentCityName,
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

            UndoButton.OnButtonClick += Undo;
            RedoButton.OnButtonClick += Redo;

            DialogNameButton.OnButtonClick += ChangeName;

            CloseButton.OnButtonClick += Close;

            SetMode(PainterMode.ROAD);

            UndoStack.UndoChanged += UndoChanged;

            UndoChanged();
        }

        private void ChangeName(UIElement button)
        {
            var dialog = new UILotPurchaseDialog()
                .AsRenameDialog(
                    GameFacade.CurrentCityName,
                    GameFacade.Strings.GetString("f130", "51"),
                    GameFacade.Strings.GetString("f130", "52"));

            dialog.OnNameChosen += (name) =>
            {
                // TODO: set on server
                TController.UpdateCityName(name);
                UIScreen.RemoveDialog(dialog);
            };

            UIScreen.GlobalShowDialog(new DialogReference
            {
                Dialog = dialog,
                Controller = this,
                Modal = true,
            });
        }

        private void Redo(UIElement button)
        {
            if (!UndoStack.CanRedo()) return;

            PlayRepeatableSound(UISounds.BuildDragToolUp);

            var toRedo = UndoStack.Redo();

            if (toRedo != null)
            {
                TController.CommitMapChange(toRedo);
            }
        }

        private void Undo(UIElement button)
        {
            if (!UndoStack.CanUndo()) return;

            PlayRepeatableSound(UISounds.BuildDragToolUp);

            int? uid = UndoStack.Undo();

            if (uid != null)
            {
                TController.SendCityCommand(CityUpdateCommandMode.Undo, uid.Value);
            }
        }

        private void UndoChanged()
        {
            UndoButton.Disabled = !UndoStack.CanUndo();
            RedoButton.Disabled = !UndoStack.CanRedo();
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

        private void PlayRepeatableSound(string sfx)
        {
            var sound = HIT.HITVM.Get().PlaySoundEvent(sfx);
            (sound as HITThread).WriteVar(0x31, 1);
        }

        private void TakeScreenshot(UIElement button)
        {
            PlayRepeatableSound(UISounds.CameraPhoto);

            var gd = GameFacade.GraphicsDevice;
            EnsureThumbnailTarget();

            Terrain.DrawThumbnail(gd, CityThumbnailTarget);

            CityThumbnailTexture?.Dispose();
            CityThumbnailTexture = TextureUtils.Decimate(CityThumbnailTarget, gd, 4, false);
            CityThumbnailTimer = 0;

            byte[] data;
            using (var mem = new MemoryStream())
            {
                CityThumbnailTexture.SaveAsPng(mem, CityThumbnailTexture.Width, CityThumbnailTexture.Height);

                data = mem.ToArray();
            }

            TController.UpdateThumbnail(data);
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
                TController.HideTooltip();
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

            if (!Visible)
            {
                return;
            }

            if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.Z))
            {
                if (state.CtrlDown)
                {
                    if (state.ShiftDown)
                    {
                        Redo(RedoButton);
                    }
                    else
                    {
                        Undo(UndoButton);
                    }
                }
            }

            if (GameFacade.CurrentCityName != DialogNameButton.Caption)
            {
                DialogNameButton.Caption = GameFacade.CurrentCityName;
            }

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
