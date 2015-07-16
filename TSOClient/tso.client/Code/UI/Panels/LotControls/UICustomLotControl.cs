using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Common.rendering.framework.model;

namespace TSOClient.Code.UI.Panels.LotControls
{
    public interface UICustomLotControl
    {
        void MouseDown(UpdateState state);
        void MouseUp(UpdateState state);
        void Update(UpdateState state, bool scrolled);

        void Release();
    }
}
