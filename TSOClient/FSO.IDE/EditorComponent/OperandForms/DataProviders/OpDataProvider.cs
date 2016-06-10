using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.EditorComponent.OperandForms.DataProviders
{
    public abstract class OpDataProvider
    {
        public virtual bool IsEnabled(EditorScope scope, VMPrimitiveOperand op)
        {
            return true;
        }
    }
}
