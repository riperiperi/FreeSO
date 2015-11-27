using FSO.Server.Framework.Aries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.DataService
{
    public interface IDataServiceSyncFactory
    {
        IDataServiceSync<T> Get<T>(params string[] fields);
    }

    public interface IDataServiceSync<T>
    {
        void Sync(IAriesSession target, T item);
    }
}
