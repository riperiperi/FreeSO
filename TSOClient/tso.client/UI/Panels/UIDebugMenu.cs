using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.Common.Rendering.Framework.Model;
using FSO.Server.Clients;
using Ninject;
using System.Diagnostics;
using FSO.Client.Controllers;
using FSO.Common;

namespace FSO.Client.UI.Panels
{
    public class UIDebugMenu : UIDialog
    {
        private UIImage Background;
        private UIButton ContentBrowserBtn;

        public UIDebugMenu() : base(UIDialogStyle.Tall, true)
        {
            SetSize(500, 320);
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
            ContentBrowserBtn.Caption = "Nhood Global Cycle";
            ContentBrowserBtn.Position = new Microsoft.Xna.Framework.Vector2(160, 50);
            ContentBrowserBtn.Width = 300;
            ContentBrowserBtn.OnButtonClick += x =>
            {
                UIAlert.Prompt("Force Cycle", "How many days in the future do you want neighbourhoods to process?", true, (response) =>
                {
                    uint result;
                    if (!uint.TryParse(response, out result)) return;

                    FindController<CoreGameScreenController>()?.NeighborhoodProtocol.PretendDate(ClientEpoch.Now + 60 * 60 * 24 * result, (code) => { });
                });
            };
            Add(ContentBrowserBtn);

            var cityPainterBtn = new UIButton();
            cityPainterBtn.Caption = "Trigger hollow.fsoh regeneration";
            cityPainterBtn.Position = new Microsoft.Xna.Framework.Vector2(160, 90);
            cityPainterBtn.Width = 300;
            cityPainterBtn.OnButtonClick += x =>
            {
                var core = (GameFacade.Screens.CurrentUIScreen as CoreGameScreen);
                if (core == null) return;

                var controller = core.FindController<CoreGameScreenController>();

                if (controller == null) return;

                if (controller.ModerationLevel < 3)
                {
                    UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        Message = "You must be super admin to run this command."
                    }, true);
                    return;
                }

                UIAlert.YesNo("Lot cleanup", "Do you want to fully re-save lots that have been moved (yes), or just update all lots (no)?", true, (answer) =>
                {
                    controller.RegenerateHollowLots(answer);

                    UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        Message = "Regenerating hollow lots - this might take some time. Check the logs for the current progress."
                    }, true);
                });
            };
            Add(cityPainterBtn);

            var ngbhBtn = new UIButton();
            ngbhBtn.Caption = "Ngbh Editor";
            ngbhBtn.Position = new Microsoft.Xna.Framework.Vector2(160, 130);
            ngbhBtn.Width = 300;
            ngbhBtn.OnButtonClick += x =>
            {
                var core = (GameFacade.Screens.CurrentUIScreen as CoreGameScreen);
                if (core == null) return;
                if (core.CityRenderer.Plugin == null)
                {
                    core.CityRenderer.Plugin = new Rendering.City.Plugins.NeighbourhoodEditPlugin(core.CityRenderer);
                    ngbhBtn.Caption = "Disable Editor";
                }
                else
                {
                    core.CityRenderer.Plugin = null;
                    ngbhBtn.Caption = "Neighborhood Editor (local)";
                }
            };
            Add(ngbhBtn);

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
            resyncBtn.Caption = "Lot Disconnect";
            resyncBtn.Position = new Microsoft.Xna.Framework.Vector2(160, 210);
            resyncBtn.Width = 150;
            resyncBtn.OnButtonClick += x =>
            {
                var kernel = FSOFacade.Kernel;
                if (kernel != null)
                {
                    var reg = kernel.Get<Regulators.LotConnectionRegulator>();
                    reg.Client.Disconnect();
                }
                /*
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
                */
            };
            Add(resyncBtn);

            var cityDCBtn = new UIButton();
            cityDCBtn.Caption = "City Disconnect";
            cityDCBtn.Position = new Microsoft.Xna.Framework.Vector2(160+150, 210);
            cityDCBtn.Width = 150;
            cityDCBtn.OnButtonClick += x =>
            {
                var kernel = FSOFacade.Kernel;
                if (kernel != null)
                {
                    var reg = kernel.Get<Regulators.CityConnectionRegulator>();
                    reg.Client.Disconnect();
                }
            };
            Add(cityDCBtn);

            var saveUpgradesBtn = new UIButton();
            saveUpgradesBtn.Caption = "Export Upgrades";
            saveUpgradesBtn.Position = new Microsoft.Xna.Framework.Vector2(160, 250);
            saveUpgradesBtn.Width = 300;
            saveUpgradesBtn.OnButtonClick += x =>
            {
                var content = Content.Content.Get();
                if (content.Upgrades?.ActiveFile == null)
                {
                    UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        Message = "No Upgrades File available to export, try joining a server with upgrades."
                    }, true);
                    return;
                }
                content.Upgrades.SaveJSONTuning();
                UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Message = "Upgrades File was exported to Content/upgrades.json."
                }, true);
            };
            Add(saveUpgradesBtn);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.M))
            {
                // Enables client-side permissions overrides. (similar to move-objects)
                // Now I know what you're thinking - but these requests are permission checked server side anyways.
                GameFacade.EnableMod = true;
            }
        }
    }
}
