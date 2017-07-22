using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.UI
{
    public interface IGameStartProxy
    {
        void Start(bool useDX);
        void SetPath(string path);
    }
}
