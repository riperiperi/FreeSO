using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Utils
{
    public interface ITimedCachable
    {
        void Rereferenced(bool save);
    }
}
