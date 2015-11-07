using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework
{
    public abstract class EagerDataServiceProvider <KEY, VALUE> : AbstractDataServiceProvider<KEY, VALUE> where VALUE : IModel
    {
        protected Dictionary<KEY, Task<object>> Values = new Dictionary<KEY, Task<object>>();

        protected TimeSpan LazyLoadTimeout = TimeSpan.FromSeconds(10);
        protected bool OnMissingLazyLoad = true;
        protected bool OnLazyLoadCacheValue = false;

        public EagerDataServiceProvider(){
        }

        public override void Init()
        {
            PreLoad((KEY key, VALUE value) =>
            {
                Values.Add(key, Immediate(value));
            });
        }

        public override Task<object> Get(object key)
        {
            if (Values.ContainsKey((KEY)key)){
                return Values[(KEY)key];
            }else{
                if (OnMissingLazyLoad) {
                    var value = ResolveMissingKey(key);
                    if (OnLazyLoadCacheValue)
                    {
                        Values.Add((KEY)key, value);
                    }
                    return value;
                }else{
                    var tcs = new TaskCompletionSource<object>();
                    tcs.SetException(new Exception("Key not found"));
                    return tcs.Task;
                }
            }
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
