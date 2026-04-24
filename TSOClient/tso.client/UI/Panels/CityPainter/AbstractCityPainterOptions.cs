using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.CityPainter
{
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
}
