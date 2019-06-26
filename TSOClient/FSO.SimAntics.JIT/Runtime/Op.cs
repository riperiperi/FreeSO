using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.JIT.Runtime
{
    public static class Op
    {
        public static T Read<T>(byte[] data) where T : VMPrimitiveOperand, new()
        {
            var result = new T();
            result.Read(data);
            return result;
        }
    }
}
