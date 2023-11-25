using FSO.SimAntics.Marshals;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.Model
{
    public class VMRuntimeHeadline
    {
        public VMSetBalloonHeadlineOperand Operand;
        public VMEntity Target;
        public VMEntity IconTarget;
        public sbyte Index;
        public int Duration;
        public int Anim;

        public VMRuntimeHeadline(VMSetBalloonHeadlineOperand op, VMEntity targ, VMEntity icon, sbyte index)
        {
            Operand = op;
            Target = targ;
            IconTarget = icon;
            Index = index;
            Duration = (op.DurationInLoops && op.Duration != -1) ? op.Duration * 15 : op.Duration;
        }

        public VMRuntimeHeadline(VMRuntimeHeadlineMarshal input, VMContext context)
        {
            Operand = input.Operand;
            Target = context.VM.GetObjectById(input.Target);
            IconTarget = context.VM.GetObjectById(input.IconTarget);
            Index = input.Index;
            Duration = input.Duration;
            Anim = input.Anim;
        }

        public VMRuntimeHeadlineMarshal Save()
        {
            var result = new VMRuntimeHeadlineMarshal();
            result.Operand = Operand;
            result.Target = (Target == null) ? (short)0 : Target.ObjectID;
            result.IconTarget = (IconTarget == null) ? (short)0 : IconTarget.ObjectID;
            result.Index = Index;
            result.Duration = Duration;
            result.Anim = Anim;
            return result;
        }
    }
}
