using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Simantics.engine
{
    public class VMPrimitiveRegistration
    {
        private VMPrimitiveHandler Handler;
        public VMPrimitiveRegistration(VMPrimitiveHandler handler)
        {
            this.Handler = handler;
        }

        public Type OperandModel;
        public string Name;
        public ushort Opcode;

        public VMPrimitiveHandler GetHandler(){
            return Handler;
        }
    }
}
