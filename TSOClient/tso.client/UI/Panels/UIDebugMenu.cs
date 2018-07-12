using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.Debug.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.Server.Clients;
using Ninject;
using System.Diagnostics;
using FSO.SimAntics.NetPlay.Model.Commands;

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

            var benchmarkBtn = new UIButton();
            benchmarkBtn.Caption = "VM Performance Benchmark (100k ticks)";
            benchmarkBtn.Position = new Microsoft.Xna.Framework.Vector2(160, 170);
            benchmarkBtn.Width = 300;
            benchmarkBtn.OnButtonClick += x =>
            {
                var core = (GameFacade.Screens.CurrentUIScreen as IGameScreen);
                if (core == null || core.vm == null)
                {
                    UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        Message = "A VM must be running to benchmark performance."
                    }, true);
                    return;
                }
                var watch = new Stopwatch();
                watch.Start();

                var vm = core.vm;
                var tick = vm.Scheduler.CurrentTickID + 1;
                for (int i=0; i<100000; i++)
                {
                    vm.InternalTick(tick++);
                }

                watch.Stop();

                UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Message = "Ran 100k ticks in "+watch.ElapsedMilliseconds+"ms."
                }, true);
            };
            Add(benchmarkBtn);

            var resyncBtn = new UIButton();
            resyncBtn.Caption = "Force Resync";
            resyncBtn.Position = new Microsoft.Xna.Framework.Vector2(160, 210);
            resyncBtn.Width = 300;
            resyncBtn.OnButtonClick += x =>
            {
                var core = (GameFacade.Screens.CurrentUIScreen as IGameScreen);
                if (core == null || core.vm == null)
                {
                    UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        Message = "A VM must be running to force a resync."
                    }, true);
                    return;
                }
                core.vm.SendCommand(new VMRequestResyncCmd());
            };
            Add(resyncBtn);

            serverNameBox = new UITextBox();
            serverNameBox.X = 50;
            serverNameBox.Y = 300 - 54;
            serverNameBox.SetSize(500 - 100, 25);
            serverNameBox.CurrentText = GlobalSettings.Default.GameEntryUrl;

            Add(serverNameBox);
        }
        private UITextBox serverNameBox;

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.M))
            {
                //temporary until data service can inform people they're mod
                //now i know what you're thinking - but these requests are permission checked server side anyways
                GameFacade.EnableMod = true;
            }

            if (serverNameBox.CurrentText != GlobalSettings.Default.GameEntryUrl)
            {
                GlobalSettings.Default.GameEntryUrl = serverNameBox.CurrentText;
                GlobalSettings.Default.CitySelectorUrl = serverNameBox.CurrentText;
                var auth = FSOFacade.Kernel.Get<AuthClient>();
                auth.SetBaseUrl(serverNameBox.CurrentText);
                var city = FSOFacade.Kernel.Get<CityClient>();
                city.SetBaseUrl(serverNameBox.CurrentText);
                GlobalSettings.Default.Save();
            }
        }
    }
}
