using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework.Attributes
{
    /// <summary>
    /// Properties with this attribute present will not be updated when the entity has been marked as client sourced.
    /// (eg. skills info is sourced from the current lot VM if the avatar in question is in it.)
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property, Inherited = true)]
    public class ClientSourced : System.Attribute
    {
        public ClientSourced()
        {
        }
    }
}
