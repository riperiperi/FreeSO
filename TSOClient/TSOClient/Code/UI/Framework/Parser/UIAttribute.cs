using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Code.UI.Framework.Parser
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class UIAttribute : System.Attribute
    {
        public string Name { get; set; }

        public UIAttribute(string name)
        {
            this.Name = name;
        }
    }
}
