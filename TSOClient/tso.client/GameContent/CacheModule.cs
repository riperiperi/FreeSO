using FSO.Common.Utils.Cache;
using Ninject.Activation;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.GameContent
{
    public class CacheModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ICache>().ToProvider(typeof(CacheProvider)).InSingletonScope();
        }
    }

    public class CacheProvider : IProvider<ICache>
    {
        public Type Type
        {
            get
            {
                return typeof(ICache);
            }
        }

        public object Create(IContext context)
        {
            var cache = new FileSystemCache("./fso_cache", 10 * 1024 * 1024);
            return cache;
        }
    }
}
