using FSO.SimAntics.Engine;

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
