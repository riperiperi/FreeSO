/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using FSO.SimAntics.Engine.Utils;
using FSO.SimAntics.Primitives;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.SimAntics.Engine
{
    public static class VMDialogHandler
    {
        //should use a Trie for this in future, for performance reasons
        private static string[] valid = {
            "Object", "Me", "TempXL:", "Temp:", "$", "Attribute:", "DynamicStringLocal:", "Local:", "TimeLocal:", "NameLocal:",
            "FixedLocal:", "DynamicObjectName", "MoneyXL:", "JobOffer:", "Job:", "JobDesc:", "Param:", "Neighbor", "\r\n", "ListObject",
            "CatalogLocal:", "DateLocal:", "ObjectLocal:", "FSOCatalogName", "\\n"
        };

        public static void ShowDialog(VMStackFrame context, VMDialogOperand operand, STR source)
        {
            VMDialogInfo info = new VMDialogInfo
            {
                Block = (operand.Flags & VMDialogFlags.Continue) == 0,
                Caller = context.Caller,
                Icon = context.StackObject,
                Operand = operand,
                Message = ParseDialogString(context, source.GetString(Math.Max(0, operand.MessageStringID - 1)), source),
                Title = (operand.TitleStringID == 0) ? "" : ParseDialogString(context, source.GetString(operand.TitleStringID - 1), source),
                IconName = (operand.IconNameStringID == 0) ? "" : ParseDialogString(context, source.GetString(operand.IconNameStringID - 1), source),

                Yes = (operand.YesStringID == 0) ? null : ParseDialogString(context, source.GetString(operand.YesStringID - 1), source),
                No = (operand.NoStringID == 0) ? null : ParseDialogString(context, source.GetString(operand.NoStringID - 1), source),
                Cancel = (operand.CancelStringID == 0) ? null : ParseDialogString(context, source.GetString(operand.CancelStringID - 1), source),
                DialogID = (context.CodeOwner.GUID << 32) | ((ulong)context.Routine.ID << 16) | context.InstructionPointer
            };
            context.VM.SignalDialog(info);
        }

        private static bool CommandSubstrValid(string command)
        {
            for (int i = 0; i < valid.Length; i++)
            {
                if (command.Length <= valid[i].Length && command.Equals(valid[i].Substring(0, command.Length))) return true;
            }
            return false;
        }

        public static string ParseDialogString(VMStackFrame context, string input, STR source)
        {
            return ParseDialogString(context, input, source, 0);
        }

        public static string ParseDialogString(VMStackFrame context, string input, STR source, int depth)
        {
            if (depth > 10) return input;
            int state = 0;
            StringBuilder command = new StringBuilder();
            StringBuilder output = new StringBuilder();

            if (input == null) return "Missing String!!!";

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
                    var invalid = !CommandSubstrValid(command.ToString());
                    if (i == input.Length - 1 || invalid)
                    {
                        if (invalid || char.IsDigit(input[i]))
                        {
                            command.Remove(command.Length - 1, 1);
                            i--;
                        }

                        var cmdString = command.ToString();
                        short[] values = new short[3];
                        if (cmdString.Length > 1 && cmdString[cmdString.Length - 1] == ':')
                        {
                            try
                            {
                                if (cmdString == "DynamicStringLocal:" || cmdString == "TimeLocal:" || cmdString == "JobOffer:" || cmdString == "Job:" || cmdString == "JobDesc:" || cmdString == "DateLocal:")
                                {
                                    values[1] = -1;
                                    values[2] = -1;
                                    for (int j=0; j<3; j++)
                                    {
                                        char next = input[++i];
                                        string num = "";
                                        while (char.IsDigit(next))
                                        {
                                            num += next;
                                            next = (++i == input.Length) ? '!': input[i];
                                        }
                                        if (num == "")
                                        {
                                            values[j] = -1;
                                            if (j == 1) values[2] = -1;
                                            break;
                                        }
                                        values[j] = short.Parse(num);
                                        if (i == input.Length || next != ':') break;
                                    }
                                }
                                else
                                {
                                    char next = input[++i];
                                    string num = "";
                                    while (char.IsDigit(next))
                                    {
                                        num += next;
                                        next = (++i == input.Length) ? '!' : input[i];
                                    }
                                    values[0] = short.Parse(num);
                                }
                                i--;
                            }
                            catch (FormatException)
                            {

                            }
                        }
                        try
                        {
                            switch (cmdString)
                            {
                                case "FSOCatalogName":
                                    var fsoCatObj = context.StackObject.MasterDefinition ?? context.StackObject.Object.OBJ;
                                    var fsoCatName = context.StackObject.Object.Resource.Get<CTSS>(fsoCatObj.CatalogStringsID)?.GetString(0);
                                    output.Append(fsoCatName ?? context.StackObject.ToString());
                                    break;
                                case "Object":
                                case "DynamicObjectName":
                                    //hack: if stack object doesn't exist and should contain owner's id,
                                    //try output the callee's owner id instead for tip jar.
                                    //special id for this is -1.
                                    if (context.StackObjectID == -1 && !context.VM.TS1)
                                    {
                                        //StackObjectOwnerID call sets the id to -1 if no owner found. (null is usually 0)
                                        output.Append(context.VM.TSOState.Names.GetNameForID(
                                            context.VM, 
                                            (context.Callee.TSOState as VMTSOObjectState)?.OwnerID ?? 0
                                            ));
                                    } else
                                    {
                                        output.Append(context.StackObject.ToString());
                                    }
                                    break;
                                case "Me":
                                    output.Append(context.Caller.ToString()); break;
                                case "TempXL:":
                                    output.Append(VMMemory.GetBigVariable(context, Scopes.VMVariableScope.TempXL, values[0]).ToString()); break;
                                case "MoneyXL:":
                                    output.Append("$" + VMMemory.GetBigVariable(context, Scopes.VMVariableScope.TempXL, values[0]).ToString("##,#0")); break;
                                case "Temp:":
                                    output.Append(VMMemory.GetBigVariable(context, Scopes.VMVariableScope.Temps, values[0]).ToString()); break;
                                case "$":
                                    output.Append("$"); i--; break;
                                case "Attribute:":
                                    output.Append(VMMemory.GetBigVariable(context, Scopes.VMVariableScope.StackObjectAttributes, values[0]).ToString()); break;
                                case "DynamicStringLocal:":
                                    STR res = null;
                                    if (values[2] != -1 && values[1] != -1)
                                    {
                                        VMEntity obj = context.VM.GetObjectById((short)context.Locals[values[2]]);
                                        if (obj == null) break;
                                        ushort tableID = (ushort)context.Locals[values[1]];

                                        {//local
                                            if (obj.SemiGlobal != null) res = obj.SemiGlobal.Get<STR>(tableID);
                                            if (res == null) res = obj.Object.Resource.Get<STR>(tableID);
                                            if (res == null) res = context.Global.Resource.Get<STR>(tableID);
                                        }
                                    } else if (values[1] != -1)
                                    {
                                        //global table
                                        ushort tableID = (ushort)context.Locals[values[1]];
                                        res = context.Global.Resource.Get<STR>(tableID);

                                    } else
                                    {
                                        res = source;
                                    }

                                    ushort index = (ushort)context.Locals[values[0]];
                                    if (res != null)
                                    {
                                        var str = res.GetString(index);
                                        output.Append(ParseDialogString(context, str, res, depth++)); // recursive command parsing!
                                        // this is needed for the crafting table.
                                        // though it is also, completely insane?
                                    }
                                    break;
                                case "Local:":
                                    output.Append(VMMemory.GetBigVariable(context, Scopes.VMVariableScope.Local, values[0]).ToString()); break;
                                case "FixedLocal:":
                                    output.Append((VMMemory.GetBigVariable(context, Scopes.VMVariableScope.Local, values[0])/100f).ToString("F2")); break;
                                case "TimeLocal:":
                                    var hours = VMMemory.GetBigVariable(context, Scopes.VMVariableScope.Local, values[0]);
                                    var mins = (values[1] == -1)?0:VMMemory.GetBigVariable(context, Scopes.VMVariableScope.Local, values[1]);
                                    var suffix = (hours > 11) ? "pm" : "am";
                                    if (hours > 12) hours -= 12;
                                    output.Append(hours.ToString());
                                    output.Append(":");
                                    output.Append(mins.ToString().PadLeft(2, '0'));
                                    output.Append(suffix);
                                    break;
                                case "ObjectLocal:":
                                    output.Append(context.VM.GetObjectById(VMMemory.GetVariable(context, Scopes.VMVariableScope.Local, values[0]))?.ToString() ?? ""); break;
                                case "JobOffer:":
                                    output.Append(Content.Content.Get().Jobs.JobOffer(
                                        (short)VMMemory.GetBigVariable(context, Scopes.VMVariableScope.Local, values[0]),
                                        VMMemory.GetBigVariable(context, Scopes.VMVariableScope.Local, values[1])));
                                    break;
                                case "Job:":
                                case "JobDesc:":
                                    var level = VMMemory.GetBigVariable(context, Scopes.VMVariableScope.Local, values[1]);
                                    var jobStr = Content.Content.Get().Jobs.JobStrings(
                                        (short)VMMemory.GetBigVariable(context, Scopes.VMVariableScope.Local, values[0]));
                                    if (jobStr != null) output.Append(jobStr.GetString(level*3+((cmdString=="JobDesc:")?3:4)));
                                    break;
                                case "Param:":
                                    output.Append(VMMemory.GetBigVariable(context, Scopes.VMVariableScope.Parameters, values[0]).ToString()); break;
                                case "NameLocal:":
                                    output.Append(context.VM.GetObjectById(VMMemory.GetVariable(context, Scopes.VMVariableScope.Local, values[0])).ToString()); break;
                                case "Neighbor":
                                    //neighbour in stack object id
                                    if (!context.VM.TS1) break;
                                    var guid = Content.Content.Get().Neighborhood.GetNeighborByID(context.StackObjectID)?.GUID ?? 0;
                                    var gobj = Content.Content.Get().WorldObjects.Get(guid);
                                    if (gobj == null) output.Append("Unknown");
                                    else output.Append(gobj.Resource.Get<FSO.Files.Formats.IFF.Chunks.CTSS>(gobj.OBJ.CatalogStringsID)?.GetString(0) ?? "Unknown");
                                    break;
                                case "ListObject":
                                    output.Append(new string(context.StackObject.MyList.Select(x => (char)x).ToArray()));
                                    break;
                                case "CatalogLocal:":
                                    var catObj = context.VM.GetObjectById(VMMemory.GetVariable(context, Scopes.VMVariableScope.Local, values[0]));
                                    var cat = catObj.Object.Resource.Get<CTSS>(catObj.Object.OBJ.CatalogStringsID)?.GetString(1);
                                    output.Append(cat ?? "");
                                    break;
                                case "DateLocal:":
                                    var date = new DateTime(context.Locals[values[2]], context.Locals[values[1]], context.Locals[values[0]]);
                                    output.Append(date.ToLongDateString());
                                    break;
                                case "\\n":
                                    output.Append("\n");
                                    break;
                                default:
                                    output.Append(cmdString);
                                    break;
                            }
                        } catch (Exception)
                        {
                            //something went wrong. just skip command
                        }
                        state = 0;
                    }
                }
            }
            if (context.Thread != null) output.Replace("\r\n", "\r\n\r\n");
            return output.ToString();
        }
    }
}
