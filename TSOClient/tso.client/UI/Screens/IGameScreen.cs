using FSO.Client.UI.Panels;
using FSO.SimAntics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Screens
{
    public interface IGameScreen
    {
        bool InLot { get; }
        int ZoomLevel { get; set; }
        int Rotation { get; set; }
        sbyte Level { get; set; }
        sbyte Stories { get; }
        uint VisualBudget { get; set; }
        int ScreenHeight { get; }

        UILotControl LotControl { get; set; }
        VM vm { get; set; }
    }
}
