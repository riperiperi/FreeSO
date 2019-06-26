using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Electron.Model
{
    public interface IActionRequest
    {
        object OType { get; }
        bool NeedsValidation { get; }
    }
}
