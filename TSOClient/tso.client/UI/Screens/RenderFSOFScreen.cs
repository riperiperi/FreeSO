using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Panels.WorldUI;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Files.RC;
using FSO.LotView;
using FSO.LotView.Facade;
using FSO.Server.Clients;
using FSO.SimAntics;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.NetPlay.Drivers;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace FSO.Client.UI.Screens
{
    public class RenderFSOFScreen : UIScreen
    {
        private UILoginDialog LoginDialog;
        private ApiClient api;

        public RenderFSOFScreen()
        {
            HIT.HITVM.Get().PlaySoundEvent(Model.UIMusic.None);
            LoginDialog = new UILoginDialog(Login);
            LoginDialog.Opacity = 0.9f;
            //Center
            LoginDialog.X = (ScreenWidth - LoginDialog.Width) / 2;
            LoginDialog.Y = (ScreenHeight - LoginDialog.Height) / 2;
            this.Add(LoginDialog);
        }

        public int TotalLotNum = 0;
        public List<uint> LotQueue = new List<uint>();

        public void Login()
        {
            api = new ApiClient(GlobalSettings.Default.GameEntryUrl);
            api.AdminLogin(LoginDialog.Username, LoginDialog.Password, (result) =>
            {
                api.GetLotList(1, (lots) =>
                {
                    LotQueue.AddRange(lots);
                    TotalLotNum += lots.Length;
                    RenderLot();
                });

            });
        }
        private void RenderLot()
        {
            if (LotQueue.Count > 0)
            {
                var lot = LotQueue[0];
                LotQueue.RemoveAt(0);
                api.GetFSOV(1, lot, (bt) =>
                {
                    try
                    {
                        if (bt == null)
                        {
                            RenderLot();
                        }
                        else
                        {
                            var fsof = RenderFSOF(bt);
                            using (var mem = new MemoryStream())
                            {
                                fsof.Save(mem);
                                api.UploadFSOF(1, lot, mem.ToArray(), (success) =>
                                {
                                    RenderLot();
                                });
                            }
                        }
                    } catch
                    {
                        RenderLot();
                    }
                });
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.F1))
                FSOFacade.Controller.ToggleDebugMenu();
        }

        public FSOF RenderFSOF(byte[] fsov)
        {
            var marshal = new VMMarshal();
            using (var mem = new MemoryStream(fsov)) {
                marshal.Deserialize(new BinaryReader(mem));
            }

            var world = new FSO.LotView.RC.WorldRC(GameFacade.GraphicsDevice);
            world.Opacity = 1;
            GameFacade.Scenes.Add(world);

            var globalLink = new VMTSOGlobalLinkStub();
            var driver = new VMServerDriver(globalLink);

            var vm = new VM(new VMContext(world), driver, new UIHeadlineRendererProvider());
            vm.Init();

            vm.Load(marshal);

            SetOutsideTime(GameFacade.GraphicsDevice, vm, world, 0.5f, false);
            world.State.PrepareLighting();
            var facade = new LotFacadeGenerator();
            facade.FLOOR_TILES = 64;
            facade.GROUND_SUBDIV = 5;
            facade.FLOOR_RES_PER_TILE = 2;

            SetAllLights(vm, world, 0.5f, 0);

            var result = facade.GetFSOF(GameFacade.GraphicsDevice, world, vm.Context.Blueprint, () => { SetAllLights(vm, world, 0.0f, 100); }, true);

            GameFacade.Scenes.Remove(world);
            world.Dispose();

            return result;
        }

        private void SetAllLights(VM vm, World world, float outsideTime, short contribution)
        {
            foreach (var light in vm.Entities.Where(x => x.Object.Resource.SemiGlobal?.Iff?.Filename == "lightglobals.iff"))
            {
                light.SetValue(SimAntics.Model.VMStackObjectVariable.LightingContribution, contribution);
            }
            vm.Context.Architecture.SignalAllDirty();
            vm.Context.Architecture.Tick();
            SetOutsideTime(GameFacade.GraphicsDevice, vm, world, outsideTime, false);
        }

        private static void SetOutsideTime(GraphicsDevice gd, VM vm, World world, float time, bool lightsOn)
        {
            vm.Context.Architecture.SetTimeOfDay(time);
            world.Force2DPredraw(gd);
            vm.Context.Architecture.SetTimeOfDay();
        }
    }
}
