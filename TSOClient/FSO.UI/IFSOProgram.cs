using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.UI
{
    public interface IFSOProgram
    {
        bool InitWithArguments(string[] args);
        bool UseDX { get; set; }
    }
}
