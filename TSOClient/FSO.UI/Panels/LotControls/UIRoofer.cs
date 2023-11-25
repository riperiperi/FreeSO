using System.Collections.Generic;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.UI.Panels.LotControls;

namespace FSO.Client.UI.Panels.LotControls
{
    public class UIRoofer : UICustomLotControl
    {
        public UIRoofer(VM vm, LotView.World world, ILotControl parent, List<int> parameters)
        {
            vm.SendCommand(new VMNetSetRoofCmd()
            {
                Pitch = vm.Context.Architecture.RoofPitch,
                Style = (uint)parameters[0]
            });
        }
        public override void MouseDown(UpdateState state)
        {
            return;
        }

        public override void MouseUp(UpdateState state)
        {
            return;
        }

        public override void Release()
        {
            return;
        }

        public override void Update(UpdateState state, bool scrolled)
        {
            return;
        }
    }
}
