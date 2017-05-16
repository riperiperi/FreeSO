using FSO.Common.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework
{
    public abstract class EagerDataServiceProvider <KEY, VALUE> : AbstractDataServiceProvider<KEY, VALUE> where VALUE : IModel
    {
        protected ConcurrentDictionary<KEY, object> Values = new ConcurrentDictionary<KEY, object>();

        protected TimeSpan LazyLoadTimeout = TimeSpan.FromSeconds(10);
        protected bool OnMissingLazyLoad = true;
        protected bool OnLazyLoadCacheValue = false;

        public EagerDataServiceProvider(){
        }

        public override void Init()
        {
            PreLoad(Insert);
        }

        protected virtual VALUE LoadOne(KEY key)
        {
            return default(VALUE);
        }

        protected virtual void Insert(KEY key, VALUE value)
        {
            Values.AddOrUpdate(key, value, (oKey, oValue) => value);
        }

        protected virtual VALUE Remove(KEY key)
        {
            object value;
            Values.TryRemove(key, out value);
            return (VALUE)value;
        }

        public override Task<object> Get(object key)
        {
            if (Values.ContainsKey((KEY)key)){
                return Immediate(Values[(KEY)key]);
            }else{
                if (OnMissingLazyLoad) {
                    var value = ResolveMissingKey(key);
                    if (OnLazyLoadCacheValue)
                    {
                        value = Immediate(Values.GetOrAdd((KEY)key, value));
                    }
                    return value;
                }else{
                    var tcs = new TaskCompletionSource<object>();
                    tcs.SetException(new Exception("Key not found"));
                    return tcs.Task;
                }
            }
        }

        public override void Invalidate(object key)
        {
            var newVal = LoadOne((KEY)key);
            if (newVal != null)
            {
                Insert((KEY)key, newVal);
            } else
            {
                Remove((KEY)key);
            }
        }

        public override void Invalidate(object key, object replacement)
        {
            Insert((KEY)key, (VALUE)replacement);
        }

        private Task<object> Immediate(object value)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(value);
            return tcs.Task;
        }

        private Task<object> ResolveMissingKey(object key)
        {
            var cts = new CancellationTokenSource(LazyLoadTimeout);
            return Task.Factory.StartNew<object>(() =>
            {
                return (object)LazyLoad((KEY)key);
            }, cts.Token);
        }

        protected abstract void PreLoad(Callback<KEY, VALUE> appender);
        
        protected virtual VALUE LazyLoad(KEY key)
        {
            return ModelActivator.NewInstance<VALUE>();
        }
    }
}
