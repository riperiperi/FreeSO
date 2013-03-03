using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Panels;

namespace TSOClient.Code.UI.Screens
{
    public class CoreGameScreen : GameScreen
    {
        private UIUCP ucp;
        private UIGizmo gizmo;

        public CoreGameScreen()
        {
            ucp = new UIUCP();
            ucp.Y = ScreenHeight - 210;
            this.Add(ucp);

            gizmo = new UIGizmo();
            gizmo.X = ScreenWidth - 300;
            gizmo.Y = ScreenHeight - 300;
            this.Add(gizmo);
        }
    }
}
