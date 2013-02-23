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


        public CoreGameScreen()
        {
            ucp = new UIUCP();
            this.Add(ucp);
        }
    }
}
