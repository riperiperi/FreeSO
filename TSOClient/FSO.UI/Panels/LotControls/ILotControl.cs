using FSO.LotView;
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
    }
}
