using System;

namespace FSO.Common.DataService.Framework.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class Persist : Attribute
    {
        public Persist()
        {
        }
    }
}
