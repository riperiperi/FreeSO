using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Voltron
{
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class cTSONetMessageParameter : System.Attribute
    {
        public object Value;

        public cTSONetMessageParameter(object value)
        {
            this.Value = value;
        }
    }
}
