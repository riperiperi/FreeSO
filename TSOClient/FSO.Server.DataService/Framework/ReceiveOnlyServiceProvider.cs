using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework
{
    public abstract class ReceiveOnlyServiceProvider<KEY, VALUE> : AbstractDataServiceProvider<KEY, VALUE> where VALUE : IModel
    {
        //protected Dictionary<KEY, VALUE> Items = new Dictionary<KEY, VALUE>();
        protected Dictionary<KEY, Task<object>> Values = new Dictionary<KEY, Task<object>>();
        protected TimeSpan LazyLoadTimeout = TimeSpan.FromSeconds(10);

        public override Task<object> Get(object key)
        {
            if (!(key is KEY))
            {
                throw new Exception("Key must be of type " + typeof(KEY));
            }

            var castKey = (KEY)key;

            if (Values.ContainsKey(castKey))
            {
                return Values[castKey];
            }

            lock (Values)
            {
                if (Values.ContainsKey(castKey))
                {
                    return Values[castKey];
                }

                var result = ResolveMissingKey(castKey);
                Values.Add(castKey, result);
                return result;
            }
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
