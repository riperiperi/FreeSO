using FSO.Content;
using FSO.SimAntics.Engine;

namespace FSO.SimAntics.JIT.Runtime
{
    public interface IBHAV
    {
        VMPrimitiveExitCode Execute(VMStackFrame context, ref byte instruction);
    }

    public abstract class IInlineBHAV
    {
        public abstract bool Execute(VMStackFrame context, ref byte instruction, params short[] args);

        public virtual int ArgCount => 4;

        public GameObject CodeOwner;
        public bool Execute(VMStackFrame context, params short[] args)
        {
            byte instruction = 0;
            var stackObj = context.StackObject;
            var stackObjID = context.StackObjectID;
            var oldArgs = context.Args;
            var oldLocals = context.Locals;
            context.Args = args;
            var result = Execute(context, ref instruction, args);
            context.Args = oldArgs;
            context.Locals = oldLocals;
            if (stackObjID != context.StackObjectID)
            {
                context.StackObject = stackObj;
                context._StackObjectID = stackObjID;
            }
            return result;
        }

        public bool ExecuteExternal(VMStackFrame context, params short[] args)
        {
            //we need to set the code owner to the correct object.
            //var oldCodeOwner = context.CodeOwner;

            //context.CodeOwner = CodeOwner;
            var result = Execute(context, args);

            //context.CodeOwner = oldCodeOwner;
            return result;
        }
    }
}
