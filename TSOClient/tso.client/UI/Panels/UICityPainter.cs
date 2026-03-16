using FSO.Client.Rendering.City;
using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Extensions.Options;

namespace FSO.Client.UI.Panels
{
    internal abstract class AbstractCityPainterPreview : UIElement
    {
        protected UICityPainter Painter { get; private set; }
        public AbstractCityPainterPreview()
        {
        }

        public virtual void Init(UICityPainter painter)
        {
            Painter = painter;
        }
    }

    internal abstract class AbstractCityPainterOptions : UIContainer
    {
        protected UICityPainter Painter { get; private set; }
        public abstract PainterMode Mode { get; }

        public AbstractCityPainterOptions()
        {
        }

        public virtual void Init(UICityPainter painter)
        {
            Painter = painter;
        }

        protected void SetModes(Span<string> modes)
        {

        }

        protected void SetToggles(Span<string> toggles)
        {

        }
    }

    internal class UICityPainterElevationPreview : AbstractCityPainterPreview
    {
        public UICityPainterElevationPreview(UICityPainter parent) : base(parent)
        {
        }

        public override void Draw(UISpriteBatch batch)
        {
            
        }
    }

    internal class UICityPainterElevationOptions : AbstractCityPainterOptions
    {
        public override PainterMode Mode => PainterMode.ELEVATION_CIRCLE;


        public UICityPainterElevationOptions(UICityPainter parent) : base(parent)
        {
            SetModes([]);
            SetToggles([]);
        }
    }

    internal class UICityPainterTerrainTypePreview : AbstractCityPainterPreview
    {
        public UICityPainterTerrainTypePreview(UICityPainter parent) : base(parent)
        {
        }

        public override void Draw(UISpriteBatch batch)
        {

        }
    }

    internal class UICityPainterTerrainTypeOptions : AbstractCityPainterOptions
    {
        public override PainterMode Mode => PainterMode.TERRAINTYPE;

        public UICityPainterTerrainTypeOptions(UICityPainter parent) : base(parent)
        {
            SetModes([]);
            SetToggles([]);
        }
    }

    internal class UICityPainterRoadsPreview : AbstractCityPainterPreview
    {
        public UICityPainterRoadsPreview(UICityPainter parent) : base(parent)
        {
        }

        public override void Draw(UISpriteBatch batch)
        {
            // Just draw a road at the middle.
        }
    }

    internal class UICityPainterRoadsOptions : AbstractCityPainterOptions
    {
        public override PainterMode Mode => PainterMode.ROAD;

        public UICityPainterRoadsOptions(UICityPainter parent) : base(parent)
        {
            SetModes([]);
            SetToggles([]);
        }
    }

    internal class UICityPainterForestsPreview : AbstractCityPainterPreview
    {
        public UICityPainterForestsPreview(UICityPainter parent) : base(parent)
        {
        }

        public override void Draw(UISpriteBatch batch)
        {
            // Just draw a road at the middle.
        }
    }

    internal class UICityPainterForestsOptions : AbstractCityPainterOptions
    {
        public override PainterMode Mode => PainterMode.FORESTTYPE;

        public UICityPainterForestsOptions(UICityPainter parent) : base(parent)
        {
            SetModes([]);
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

        private Terrain Terrain;
        private ModeUI[] Modes;

        public UICityPainter() : base(UIDialogStyle.Close, true)
        {
            Modes = [
                GenerateMode<UICityPainterElevationOptions, UICityPainterElevationPreview>(),
            ];
        }

        private ModeUI GenerateMode<TOptions, TPreview>() where TOptions : AbstractCityPainterOptions, new() where TPreview : AbstractCityPainterPreview, new()
        {
            var button = new UIButton();
            var options = new TOptions();
            var preview = new TPreview();

            options.Init(this);
            preview.Init(this);
            return new ModeUI(
                new UIButton(),
                new TOptions(),
                new TPreview()
                );
        }
    }
}
