using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent.Model
{
    public enum PrimitiveGroup : int
    {
        All = -1,
        Subroutine = 0,
        Control,
        Debug,
        Math,
        Sim,
        Object,
        Looks,
        Position,
        TSO,
        Unknown,
        Placement
    }
}
