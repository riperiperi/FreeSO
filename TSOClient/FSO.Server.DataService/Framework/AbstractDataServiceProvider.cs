using FSO.Common.Security;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework
{
    public abstract class AbstractDataServiceProvider<KEY, VALUE> : IDataServiceProvider where VALUE : IModel
    {
        public virtual void DemandMutation(object entity, MutationType type, string path, object value, ISecurityContext context)
        {
        }

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
