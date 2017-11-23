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
        public TS1BMFProvider(TS1Provider baseProvider) : base(baseProvider, new string[] { ".bmf", ".skn" })
        {
        }

        public override void Init()
        {
            base.Init();
        }

        public override Mesh Get(string name)
        {
            var s = name.Replace(".mesh", "").ToLowerInvariant();
            return base.Get(s + ".bmf") ?? base.Get(s + ".skn");
        }
    }
}
