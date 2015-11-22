using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Regulators
{
    public class RegulatorsModule : NinjectModule
    {
        public override void Load()
        {
            Bind<LoginRegulator>().To<LoginRegulator>().InSingletonScope();
            Bind<CityConnectionRegulator>().To<CityConnectionRegulator>().InSingletonScope();
            Bind<CreateASimRegulator>().To<CreateASimRegulator>().InSingletonScope();
            Bind<PurchaseLotRegulator>().To<PurchaseLotRegulator>().InSingletonScope();
            Bind<LotConnectionRegulator>().To<LotConnectionRegulator>().InSingletonScope();
        }
    }
}
