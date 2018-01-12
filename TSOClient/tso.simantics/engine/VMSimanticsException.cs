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

        public string GetStackTrace() {
            if (context == null) return "No Stack Info.";

            var stack = context.Thread.Stack;
            return GetStackTrace(stack);
        }

        public static string GetStackTrace(List<VMStackFrame> stack)
        {
            StringBuilder output = new StringBuilder();
            string prevEE = "";
            string prevER = "";

            for (int i = stack.Count-1; i>=0; i--)
            {
                if (i == 9 && i <= stack.Count - 8) {
                    output.Append("...");
                    output.AppendLine();
                }
                if (i > 8 && i <= stack.Count - 8) continue;
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

                if (frame is VMRoutingFrame)
                {
                    output.Append("VMRoutingFrame with state: ");
                    output.Append(((VMRoutingFrame)frame).State.ToString());
                }
                else 
                {
                    output.Append(frame.Routine.Rti.Name.TrimEnd('\0'));
                    output.Append(':');
                    output.Append(frame.InstructionPointer);
                    output.Append(" (");
                    var opcode = frame.GetCurrentInstruction().Opcode;
                    var primitive = (opcode > 255)?null:frame.VM.Context.Primitives[opcode];
                    output.Append((primitive == null)?opcode.ToString():primitive.Name);
                    output.Append(")");
                }
                output.AppendLine();
            }

            return output.ToString();
        }
    }
}
