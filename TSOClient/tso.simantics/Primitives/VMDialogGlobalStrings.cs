using FSO.SimAntics.Engine;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Primitives
{
    public class VMDialogGlobalStrings : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            return VMDialogPrivateStrings.ExecuteGeneric(context, args, context.Global.Resource.Get<STR>(301));
        }
    }
}
