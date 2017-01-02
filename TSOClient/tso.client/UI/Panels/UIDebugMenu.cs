using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.Debug.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    public class UIDebugMenu : UIDialog
    {
        private UIImage Background;
        private UIButton ContentBrowserBtn;

        public UIDebugMenu() : base(UIDialogStyle.Tall, true)
        {
            SetSize(500, 300);
            Caption = "Debug Tools";

            Position = new Microsoft.Xna.Framework.Vector2(
                (GlobalSettings.Default.GraphicsWidth / 2.0f) - 250,
                (GlobalSettings.Default.GraphicsHeight / 2.0f) - 150
            );

            Add(new UIImage()
            {
                Texture = GetTexture(0x00000Cbfb00000001),
                Position = new Microsoft.Xna.Framework.Vector2(40, 95)
            });

            ContentBrowserBtn = new UIButton();
            ContentBrowserBtn.Caption = "Browse Content";
            ContentBrowserBtn.Position = new Microsoft.Xna.Framework.Vector2(160, 50);
            ContentBrowserBtn.Width = 300;
            ContentBrowserBtn.OnButtonClick += x =>
            {
                //ShowTool(new ContentBrowser());
            };
            Add(ContentBrowserBtn);

            var connectLocalBtn = new UIButton();
            connectLocalBtn.Caption = (GlobalSettings.Default.UseCustomServer) ? "Use default server (TSO)" : "Use custom defined server";
            connectLocalBtn.Position = new Microsoft.Xna.Framework.Vector2(160, 90);
            connectLocalBtn.Width = 300;
            connectLocalBtn.OnButtonClick += x =>
            {
                GlobalSettings.Default.UseCustomServer = !GlobalSettings.Default.UseCustomServer;
                connectLocalBtn.Caption = (GlobalSettings.Default.UseCustomServer) ? "Use default server (TSO)" : "Use custom defined server";
                GlobalSettings.Default.Save();
            };
            Add(connectLocalBtn);

            var cityPainterBtn = new UIButton();
            cityPainterBtn.Caption = "Toggle City Painter";
            cityPainterBtn.Position = new Microsoft.Xna.Framework.Vector2(160, 130);
            cityPainterBtn.Width = 300;
            cityPainterBtn.OnButtonClick += x =>
            {
                var core = (GameFacade.Screens.CurrentUIScreen as CoreGameScreen);
                if (core == null) return;
                if (core.CityRenderer.Plugin == null)
                {
                    core.CityRenderer.Plugin = new Rendering.City.Plugins.MapPainterPlugin(core.CityRenderer);
                    cityPainterBtn.Caption = "Disable City Painter";
                }
                else
                {
                    core.CityRenderer.Plugin = null;
                    cityPainterBtn.Caption = "Enable City Painter";
                }
            };
            Add(cityPainterBtn);
        }
    }
}
