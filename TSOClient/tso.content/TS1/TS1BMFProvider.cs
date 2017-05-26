using FSO.Content.Framework;
using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.TS1
{
    public class TS1BMFProvider : TS1SubProvider<Mesh>
    {
        public TS1BMFProvider(TS1Provider baseProvider) : base(baseProvider, ".bmf")
        {
        }

        public override Mesh Get(string name)
        {
            return base.Get(name.Replace(".mesh", "").ToLowerInvariant() + ".bmf");
        }
    }
}
