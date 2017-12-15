using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.OperandForms.DataProviders;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Engine.Scopes;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent.Primitives
{
    public class SubroutineDescriptor : PrimitiveDescriptor
    {
        public override PrimitiveGroup Group { get { return PrimitiveGroup.Subroutine; } }
        public override PrimitiveReturnTypes Returns { get { return PrimitiveReturnTypes.TrueFalse; } }
        public override Type OperandType { get { return typeof(VMSubRoutineOperand); } }

        public override string GetTitle(EditorScope scope)
        {
            var op = (VMSubRoutineOperand)Operand;
            var name = scope.GetSubroutineName(PrimID);
            return name;
        }

        public override string GetBody(EditorScope scope)
        {
            var op = (VMSubRoutineOperand)Operand;
            var result = new StringBuilder();

            var paramNames = new string[4];
            for (var i = 0; i < 4; i++)
            {
                paramNames[i] = "arg" + i;
            }

            var bhav = scope.GetBHAV(PrimID);
            if (bhav != null)
            {
                //There might be param names
                var labels = scope.GetLabels(bhav.ChunkID);
                if (labels?.ParamNames != null)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        if (i < labels.ParamNames.Length)
                        {
                            paramNames[i] = labels.ParamNames[i];
                        }
                    }
                }
            }

            //Add parameter names and values
            for (var i = 0; i < bhav.Args; i++)
            {
                if (paramNames.Length <= i) continue;
                if (i > 0)
                {
                    result.Append(", ");
                }

                result.Append(paramNames[i] + "=");

                short argValue = op.Arguments[i];
                string argValueString = argValue + "";

                if (argValue == -1)
                {
                    argValueString = scope.GetVarName(VMVariableScope.Temps, (short)i);
                }

                result.Append(argValueString);
            }

            return result.ToString();
        }

        public override void PopulateOperandView(BHAVEditor master, EditorScope escope, TableLayoutPanel panel)
        {
            panel.Controls.Add(new OpLabelControl(master, escope, Operand, new OpStaticTextProvider("Specify the parameters passed to the subroutine. -1 passes the corresponding Temp as the parameter.")));
            var provider = new OpStaticValueBoundsProvider(-32767, 32768);
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Argument 1:", "Arg0", provider));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Argument 2:", "Arg1", provider));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Argument 3:", "Arg2", provider));
            panel.Controls.Add(new OpValueControl(master, escope, Operand, "Argument 4:", "Arg3", provider));
        }
    }
}
