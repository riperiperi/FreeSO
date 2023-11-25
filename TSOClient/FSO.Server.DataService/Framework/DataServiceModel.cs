using System;

namespace FSO.Common.DataService.Framework
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class DataServiceModel : Attribute
    {
        public DataServiceModel()
        {
        }
    }
}
