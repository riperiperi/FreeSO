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
            var reload = false;

            if (Values.ContainsKey(castKey)){
                var val = Values[castKey];
                if (RequiresReload(castKey, (VALUE)val.Result)) reload = true;
                else return Values[castKey];
            }

            lock (Values)
            {
                if (reload)
                {
                    Task<object> oldVal;
                    Values.TryRemove(castKey, out oldVal);

                    var result = ResolveMissingKey(castKey, (VALUE)oldVal.Result);
                    return Values.GetOrAdd(castKey, result);
                }
                else if (Values.ContainsKey(castKey))
                {
                    return Values[castKey];
                } else
                {
                    var result = ResolveMissingKey(castKey);
                    return Values.GetOrAdd(castKey, result);
                }


            }
        }

        private Task<object> ResolveMissingKey(object key)
        {
            return ResolveMissingKey(key, default(VALUE));
        }

        private Task<object> ResolveMissingKey(object key, VALUE oldVal)
        {
            var cts = new CancellationTokenSource(LazyLoadTimeout);
            return Task.Factory.StartNew<object>(() =>
            {
                return (object)LazyLoad((KEY)key, oldVal);
            }, cts.Token);
        }
        protected virtual VALUE LazyLoad(KEY key, VALUE oldVal)
        {
            return ModelActivator.NewInstance<VALUE>();
        }

        protected virtual VALUE LazyLoad(KEY key)
        {
            return LazyLoad(key, default(VALUE));
        }

        protected virtual bool RequiresReload(KEY key, VALUE value)
        {
            return false;
        }
    }
}
