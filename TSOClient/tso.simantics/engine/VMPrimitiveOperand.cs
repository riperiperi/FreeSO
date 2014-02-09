using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tso.simantics.engine
{
    public interface VMPrimitiveOperand
    {
        void Read(byte[] bytes);
    }
}
