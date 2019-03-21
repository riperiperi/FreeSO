using FSO.Client.UI.Hints;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Profile;
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

        public static UIHintManager Hints;
    }
}
