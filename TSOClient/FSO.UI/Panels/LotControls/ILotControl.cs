using FSO.LotView;
using FSO.LotView.Model;
using FSO.SimAntics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.UI.Panels.LotControls
{
    public interface ILotControl
    {
        VMEntity ActiveEntity { get; }
        World World { get; }
        int Budget { get; }
        I3DRotate Rotate { get; }
    }
}
