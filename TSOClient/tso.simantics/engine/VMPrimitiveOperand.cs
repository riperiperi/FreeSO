using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Simantics.engine
{
    public interface VMPrimitiveOperand
    {
        void Read(byte[] bytes);
    }
}
