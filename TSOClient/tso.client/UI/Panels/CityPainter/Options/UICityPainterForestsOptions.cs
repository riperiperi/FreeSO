using FSO.Client.Rendering.City.Plugins;

namespace FSO.Client.UI.Panels.CityPainter.Options
{
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
}
