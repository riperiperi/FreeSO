using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework.Attributes
{
    /// <summary>
    /// The original TSO game is C++. It seems to me like TSO used to have some fixed size arrays in the data model and kept the
    /// ram alive for them. Instead of removing an item from an array it would null a specific field in the child and expect
    /// the code to not count it. This does not work well in a fully object based system so  these decorations help define
    /// the intended behavior for the DataService to handle.
    /// 
    /// If this decoration is on an attribute and the value is set to null (0) the parent object is deleted (removed from arrays)
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Property, Inherited = true)]
    public class Key : System.Attribute
    {
        public Key()
        {
        }
    }
}
