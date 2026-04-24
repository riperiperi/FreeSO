using FSO.Client.Rendering.City.Plugins;

namespace FSO.Client.UI.Panels.CityPainter.Options
{
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
}
