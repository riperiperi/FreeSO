using System.Collections.Generic;

namespace FSO.Common.Content
{
    public interface IContentProvider <T>
    {
        T Get(ulong id);
        T Get(string name);
        T Get(uint type, uint fileID);
        T Get(ContentID id);
        List<IContentReference<T>> List();
    }
}
