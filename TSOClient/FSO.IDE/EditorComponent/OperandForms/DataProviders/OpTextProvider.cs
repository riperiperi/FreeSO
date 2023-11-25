using FSO.SimAntics.Engine;

namespace FSO.IDE.EditorComponent.OperandForms.DataProviders
{
    public abstract class OpTextProvider : OpDataProvider
    {
        public abstract string GetText(EditorScope scope, VMPrimitiveOperand op);
    }

    public class OpStaticTextProvider : OpTextProvider
    {
        private string Text;
        public OpStaticTextProvider(string text)
        {
            Text = text;
        }

        public override string GetText(EditorScope scope, VMPrimitiveOperand op)
        {
            return Text;
        }
    }
}
