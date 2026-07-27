using FSO.Files.RC;
using FSO.LotView;
using FSO.LotView.Facade;
using FSO.SimAntics;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.Utils
{
    public class FSOFHelper
    {
        private GraphicsDevice gd;
        private VM vm;
        private World world;

        private List<(VMEntity, short)> allLights;

        public FSOFHelper(GraphicsDevice gd, VM vm, World world)
        {
            this.gd = gd;
            this.vm = vm;
            this.world = world;

            allLights = [.. vm.Entities.Where(x => x.Object.Resource.SemiGlobal?.Iff?.Filename == "lightglobals.iff")
                .Select(x => (x, x.GetValue(SimAntics.Model.VMStackObjectVariable.LightingContribution)))];
        }

        public void SetAllLights(float outsideTime, short contribution)
        {
            foreach (var light in allLights)
            {
                light.Item1.SetValue(FSO.SimAntics.Model.VMStackObjectVariable.LightingContribution, contribution);
            }
            vm.Context.Architecture.SignalRedraw();
            vm.Context.Architecture.Tick();
            SetOutsideTime(outsideTime);
        }

        public void RestoreLights()
        {
            foreach (var light in allLights)
            {
                light.Item1.SetValue(FSO.SimAntics.Model.VMStackObjectVariable.LightingContribution, light.Item2);
            }
            vm.Context.Architecture.SignalRedraw();
            vm.Context.Architecture.Tick();
            vm.Context.Architecture.SetTimeOfDay();
            world.Force2DPredraw(gd);
        }

        public void SetOutsideTime(float time)
        {
            vm.Context.Architecture.SetTimeOfDay(time);
            world.Force2DPredraw(gd);
            vm.Context.Architecture.SetTimeOfDay();
        }

        public FSOF GenerateIngameFSOF()
        {
            /*
            SetOutsideTime(0.5f);
            */
            world.State.PrepareLighting();
            var facade = new LotFacadeGenerator();
            facade.FLOOR_TILES = 64;
            facade.GROUND_SUBDIV = 5;
            facade.FLOOR_RES_PER_TILE = 2;

            SetAllLights(0.5f, 0);

            var result = facade.GetFSOF(gd, world, vm.Context.Blueprint, () => { SetAllLights(0.0f, 100); }, true);

            RestoreLights();

            return result;
        }
    }
}
