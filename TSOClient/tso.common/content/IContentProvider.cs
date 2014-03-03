using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Common.content
{
    public interface IContentProvider <T>
    {
        T Get(ulong id);
        T Get(uint type, uint fileID);
        List<IContentReference<T>> List();
    }
}
