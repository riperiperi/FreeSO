using FSO.Client.Rendering.City.Plugins;

namespace FSO.Client.UI.Panels.CityPainter.Options
{
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
}
