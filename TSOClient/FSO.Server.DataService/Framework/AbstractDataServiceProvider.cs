using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework
{
    public abstract class AbstractDataServiceProvider<KEY, VALUE> : IDataServiceProvider where VALUE : IModel
    {
        public abstract Task<object> Get(object key);
        
        public Type GetKeyType()
        {
            return typeof(KEY);
        }

        public Type GetValueType()
        {
            return typeof(VALUE);
        }
    }

}
