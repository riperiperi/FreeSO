using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.LotView.Model
{
    public enum AvatarDisplayFlags
    {
        ShowAsGhost = 1,
        TSOGhost = 8,
        FSOGroundAlign = 1 << 12
    }
}
