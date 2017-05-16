using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework.Attributes
{
    [System.AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class Persist : System.Attribute
    {
        public Persist()
        {
        }
    }
}
