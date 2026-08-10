using System.Collections.Concurrent;

namespace FSO.Common.DataService.Framework
{
    public abstract class ReceiveOnlyServiceProvider<KEY, VALUE> : AbstractDataServiceProvider<KEY, VALUE> where VALUE : IModel
    {
        //protected Dictionary<KEY, VALUE> Items = new Dictionary<KEY, VALUE>();
        protected ConcurrentDictionary<KEY, Task<object>> Values = [];
        protected TimeSpan LazyLoadTimeout = TimeSpan.FromSeconds(10);

        public override Task<object> Get(object key)
        {
            if (!(key is KEY))
            {
                throw new Exception("Key must be of type " + typeof(KEY));
            }

            var castKey = (KEY)key;

            return Values.GetOrAdd(castKey, (KEY key) => ResolveMissingKey(key));
        }

        private Task<object> ResolveMissingKey(object key)
        {
            var cts = new CancellationTokenSource(LazyLoadTimeout);
            return Task.Factory.StartNew<object>(() =>
            {
                return (object)CreateInstance((KEY)key);
            }, cts.Token);
        }

        protected virtual VALUE CreateInstance(KEY key)
        {
            return ModelActivator.NewInstance<VALUE>();
        }
    }
}
