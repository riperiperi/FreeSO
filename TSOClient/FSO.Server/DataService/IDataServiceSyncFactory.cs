using FSO.Server.Framework.Aries;

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
