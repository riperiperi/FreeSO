using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Framework
{
    public abstract class EagerDataServiceProvider <KEY, VALUE> : AbstractDataServiceProvider<KEY, VALUE> where VALUE : IModel
    {
        public EagerDataServiceProvider(){
            PreLoad((KEY key, VALUE value) =>
            {
                // Items.Add(key, value);
            });
        }

        protected abstract void PreLoad(Callback<KEY, VALUE> appender);
    }
}
