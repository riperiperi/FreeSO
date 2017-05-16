using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Method | AttributeTargets.Property, Inherited = true)]
    public class Demand : System.Attribute
    {
        private string Pattern;

        public Demand(string pattern)
        {
            this.Pattern = pattern;
        }
    }
}
