using FSO.Common.Serialization.TypeSerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework
{
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited = true)]
    public class DataServiceModel : System.Attribute
    {
        public DataServiceModel()
        {
        }
    }
}
