using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Server.Domain.Shards;

namespace FSO.Server.Domain
{
    public class Domain : IDomain
    {
        private IKernel Kernel;
        private IShards _Shards;

        public Domain(Ninject.IKernel kernel)
        {
            this.Kernel = kernel;
            _Shards = kernel.Get<IShards>();
        }

        public IShards Shards
        {
            get{
                return _Shards;
            }
        }
    }
}
