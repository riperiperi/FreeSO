using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.CityPainter.Options
{
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
}
