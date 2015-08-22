using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.DataService
{
    public abstract class AbstractLoadingDataService <ID, VALUE> : AbstractDataService
    {
        private Dictionary<ID, VALUE> Items = new Dictionary<ID, VALUE>();

        public AbstractLoadingDataService(){
        }

        public VALUE Get(ID id){
            if (Items.ContainsKey(id))
            {
                return Items[id];
            }

            lock (Items){
                if (Items.ContainsKey(id)){
                    return Items[id];
                }

                var value = LoadOne(id);
                Items.Add(id, value);
            }

            return Items[id];
        }

        protected abstract VALUE LoadOne(ID id);
        protected abstract List<VALUE> LoadAll();

    }
}
