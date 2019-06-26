using FSO.SimAntics.Engine.Utils;
using FSO.SimAntics.Model;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Engine
{
    class JitIdeas
    {
        //the point:
        //a lot of execution time is spent on if statements and switches that check the operand
        //a way to remove that entirely would be to write and compile c# functions with those *static* operands in mind.
        //the compiler can also optimise multiple consecutive expression primitives. This can lead to orders of magnitude speedups for simantics.
        //(also a point of interest for generating readable simantics code... and perhaps writing our own text language!)

        //on platforms without JIT, we can still AOT simantics scripts that we expect to be there, then swap to interpreter when
        //hash functions do not match. we usually do one cs file per iff, and one assembly per iff - but with the AOT setup you would include ALL cs files
        //in the same fully inclusive assembly. (eg. TS1.Scripts or FSO.Scripts)

        //building the code
        // - we build separate inst
        // - anything that uses a temp variable must be enclosed in a scope (see refresh)

        //partial implementation and fallbacks
        // - instructions fall back on their interpreter primitives, pulling from private static operands baked into the class.
        // - scopes fall back on VMMemory

        //function modes:
        // - async function: sets up full vm stack frame system, can yield. exits this function to enter the next, essentially yielding.
        // - inline function: function is small and guaranteed not to yield (or change). however i would like to extend this to yielding functions somehow, specifically Idle.
        // - sync function: function is guaranteed to not yield. as a separate function.

        //structure detection:
        // - structure types: if/else, switch, loop
        // - a structure is broken when: structure from a non-nested struct jumps back into a branch, a instruction in the structure yields
        //   - broken structures only fully kill the loop primitive - for if/else and switch they will just return to the global switch statement.
        //   - loop primitives will have to break out eventually regardless - the breaking point is code that jumps *in*.

        public JitReturnData main(VMStackFrame context, byte instruction)
        {
            VMPrimitiveExitCode eResult;
            while (true)
            {
                switch (instruction)
                {
                    case 0:
                        //0: expression
                        context.Thread.TempRegisters[0] = 1;
                        //1: set to next
                        eResult = new VMSetToNext().Execute(context, context.Routine.Instructions[1].Operand);
                        instruction = (byte)((eResult == VMPrimitiveExitCode.GOTO_TRUE) ? 2 : 4);
                        break;
                    case 2:
                        //2: expression
                        context.StackObject.SetValue(VMStackObjectVariable.BirthDay, VMMemory.GetVariable(context, Scopes.VMVariableScope.StackObjectList, 2));
                        //3: refresh
                        {
                            VMEntity target = context.Caller;
                            if (target is VMGameObject)
                            {
                                var TargObj = (VMGameObject)target;
                                TargObj.RefreshGraphic();
                            }
                        }
                        break;
                }
            }
        }
    }

    class JitReturnData
    {

    }
}
