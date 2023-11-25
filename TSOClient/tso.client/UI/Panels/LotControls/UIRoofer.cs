using System.Collections.Generic;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Client.UI.Panels.LotControls
{
    public class UIRoofer : UICustomLotControl
    {
        public UIRoofer(VM vm, LotView.World world, UILotControl parent, List<int> parameters)
        {
            vm.SendCommand(new VMNetSetRoofCmd()
            {
                Pitch = vm.Context.Architecture.RoofPitch,
                Style = (uint)parameters[0]
            });
        }
        public void MouseDown(UpdateState state)
        {
            return;
        }

        public void MouseUp(UpdateState state)
        {
            return;
        }

        public void Release()
        {
            return;
        }

        public void Update(UpdateState state, bool scrolled)
        {
            return;
        }
    }
}
