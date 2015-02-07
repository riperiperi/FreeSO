using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using TSO.Simantics.engine.utils;
using TSO.Simantics.primitives;
using TSO.Files.formats.iff.chunks;

namespace TSO.Simantics.engine
{
    public static class VMDialogHandler
    {
        //should use a Trie for this in future, for performance reasons
        private static string[] valid = {
            "Object", "Me", "TempXL:", "Temp:", "$", "Attribute:", "DynamicStringLocal:", "Local:", "NameLocal:"
        };

        public static void ShowDialog(VMStackFrame context, VMDialogStringsOperand operand, STR source)
        {
            string MessageBody = ParseDialogString(context, source.GetString(operand.MessageStringID - 1));
            System.Diagnostics.Debug.Print(MessageBody);
        }

        private static bool CommandSubstrValid(string command)
        {
            for (int i = 0; i < valid.Length; i++)
            {
                if (command.Length <= valid[i].Length && command.Equals(valid[i].Substring(0, command.Length))) return true;
            }
            return false;
        }

        public static string ParseDialogString(VMStackFrame context, string input)
        {
            int state = 0;
            StringBuilder command = new StringBuilder();
            StringBuilder output = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                if (state == 0)
                {
                    if (input[i] == '$')
                    {
                        state = 1; //start parsing string
                        command.Clear();
                    } else {
                        output.Append(input[i]);
                    }
                }
                else
                {
                    command.Append(input[i]);
                    if (i == input.Length - 1 || !CommandSubstrValid(command.ToString()))
                    {
                        if (i != input.Length - 1)
                        {
                            command.Remove(command.Length - 1, 1);
                            i--;
                        }

                        var cmdString = command.ToString();
                        ushort value = 0;
                        if (cmdString.Length > 1 && cmdString[cmdString.Length - 1] == ':')
                        {
                            try
                            {
                                value = ushort.Parse(new string(new char[] { input[++i] }));
                            }
                            catch (FormatException)
                            {

                            }
                        }
                        switch (cmdString)
                        {
                            case "Object":
                                output.Append(context.StackObject.ToString()); break;
                            case "Me":
                                output.Append(context.Caller.ToString()); break;
                            case "TempXL:":
                                output.Append(VMMemory.GetBigVariable(context, scopes.VMVariableScope.TempXL, value).ToString()); break;
                            case "Temp:":
                                output.Append(VMMemory.GetBigVariable(context, scopes.VMVariableScope.Temps, value).ToString()); break;
                            case "$":
                                output.Append("$"); i--; break;
                            case "Attribute:":
                                output.Append(VMMemory.GetBigVariable(context, scopes.VMVariableScope.MyObjectAttributes, value).ToString()); break;
                            case "DynamicStringLocal:":
                                output.Append("(DynamicStringLocal)"); break;
                            case "Local:": 
                                output.Append(VMMemory.GetBigVariable(context, scopes.VMVariableScope.Local, value).ToString()); break;
                            case "NameLocal:":
                                output.Append("(NameLocal)"); break;
                            default:
                                output.Append(cmdString);
                                break;
                        }
                        state = 0;
                    }
                }
            }

            return output.ToString();
        }
    }
}
