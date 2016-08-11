using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework
{
    public abstract class LazyDataServiceProvider <KEY, VALUE> : AbstractDataServiceProvider<KEY, VALUE> where VALUE : IModel
    {
        //protected Dictionary<KEY, VALUE> Items = new Dictionary<KEY, VALUE>();
        protected ConcurrentDictionary<KEY, Task<object>> Values = new ConcurrentDictionary<KEY, Task<object>>();
        protected TimeSpan LazyLoadTimeout = TimeSpan.FromSeconds(10);

        public override Task<object> Get(object key)
        {
            if (!(key is KEY))
            {
                throw new Exception("Key must be of type " + typeof(KEY));
            }

            var castKey = (KEY)key;

            if (Values.ContainsKey(castKey)){
                return Values[castKey];
            }

            lock (Values)
            {
                if (Values.ContainsKey(castKey)){
                    return Values[castKey];
                }

                var result = ResolveMissingKey(castKey);
                return Values.GetOrAdd(castKey, result);
            }
        }

        private Task<object> ResolveMissingKey(object key)
        {
            var cts = new CancellationTokenSource(LazyLoadTimeout);
            return Task.Factory.StartNew<object>(() =>
            {
                return (object)LazyLoad((KEY)key);
            }, cts.Token);
        }

        protected virtual VALUE LazyLoad(KEY key)
        {
            return ModelActivator.NewInstance<VALUE>();
        }
    }
}
