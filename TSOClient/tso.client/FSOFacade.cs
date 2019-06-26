using FSO.Client.Network;
using FSO.Client.UI.Hints;
using FSO.Client.UI.Panels;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client
{
    public class FSOFacade
    {
        public static KernelBase Kernel;
        public static GameController Controller;
        public static UIMessageController MessageController = new UIMessageController();
        public static NetworkStatus NetStatus = new NetworkStatus();

        public static UIHintManager Hints;
    }
}
