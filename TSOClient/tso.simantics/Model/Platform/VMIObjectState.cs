using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model.Platform
{
    public interface VMIObjectState
    {
        ushort Wear { get; set; }
        void ProcessQTRDay(VM vm, VMEntity owner);
    }
}
