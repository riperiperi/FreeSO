using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FSO.SimAntics;
using System.Collections;
using FSO.SimAntics.Engine;

namespace FSO.Debug.Controls
{
    public partial class VMRoutineDisplay : UserControl
    {
        public VMRoutineDisplay()
        {
            InitializeComponent();
        }

        private void InvalidateRoutine()
        {
            IList<VMInstructionDisplay> items = new List<VMInstructionDisplay>();
            if (_Routine != null)
            {
                foreach (var inst in _Routine.Instructions){
                    items.Add(new VMInstructionDisplay(inst));
                }
            }
            grid.DataSource = items;
        }

        private VMRoutine _Routine;
        public VMRoutine Routine
        {
            get{
                return _Routine;
            }
            set{
                _Routine = value;
                InvalidateRoutine();
            }
        }
    }

    public class VMInstructionDisplay
    {
        private VMInstruction Instruction;
        private VMPrimitiveRegistration Primitive;
        public VMInstructionDisplay(VMInstruction inst){

            this.Primitive = inst.Function.VM.Context.GetPrimitive(inst.Opcode);
            //vm.Context.GetPrimitive(inst.Opcode)
            this.Instruction = inst;
        }


        public string Index {
            get{
                return Instruction.Index.ToString();
            }
        }

        public object Margin
        {
            get
            {
                return null;
            }
        }

        public string Opcode
        {
            get {
                if (this.Primitive != null)
                {
                    return this.Primitive.Name;
                }
                return this.Instruction.Opcode.ToString();
            }
        }


        public string Operand
        {
            get
            {
                if (this.Instruction.Operand != null){
                    return this.Instruction.Operand.ToString();
                }
                return "";
            }
        }
    }
}
