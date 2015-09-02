using FSO.Client.Network.Regulators;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.Network
{
    public class NetworkModule : NinjectModule
    {
        public override void Load(){
            Bind<LoginRegulator>().To<LoginRegulator>().InSingletonScope();
        }
    }
}
