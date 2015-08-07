/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Engine
{
    public class VMSimanticsException : Exception
    {
        private string message;
        private VMStackFrame context;
        public VMSimanticsException(string message, VMStackFrame context) : base(message)
        {
            this.context = context;
            this.message = message;
        }

        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.Append(message);
            output.AppendLine();
            output.AppendLine();
            output.Append(GetStackTrace());
            return output.ToString();
        }

        public string GetStackTrace()
        {
            if (context == null) return "No Stack Info.";
            StringBuilder output = new StringBuilder();
            
            var stack = context.Thread.Stack;

            string prevEE = "";
            string prevER = "";

            for (int i = stack.Count-1; i>=0; i--)
            {
                var frame = stack[i];
                //run in tree:76

                string callerStr = frame.Caller.ToString();
                string calleeStr = frame.Callee.ToString();

                if (callerStr != prevER || calleeStr != prevEE)
                {
                    output.Append('(');
                    output.Append(callerStr);
                    output.Append(':');
                    output.Append(calleeStr);
                    output.Append(") ");
                    output.AppendLine();
                    prevEE = calleeStr;
                    prevER = callerStr;
                }

                output.Append(" > ");

                if (frame is VMPathFinder)
                {
                    output.Append("VMPathFinder to: ");
                    output.Append(((VMPathFinder)frame).CurRoute.Position.ToString());
                }
                else 
                {
                    output.Append(frame.Routine.Rti.Name.TrimEnd('\0'));
                    output.Append(':');
                    output.Append(frame.InstructionPointer);
                    output.Append(" (");
                    var opcode = frame.GetCurrentInstruction().Opcode;
                    var primitive = context.VM.Context.Primitives[opcode];
                    output.Append((primitive == null)?opcode.ToString():primitive.Name);
                    output.Append(")");
                }
                output.AppendLine();
            }

            return output.ToString();
        }
    }
}
