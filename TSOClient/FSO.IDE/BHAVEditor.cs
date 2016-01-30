using FSO.Client;
using FSO.Client.UI.Framework;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent;
using FSO.IDE.EditorComponent.DataView;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.UI;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE
{
    public partial class BHAVEditor : Form
    {
        private Dictionary<Button, PrimitiveGroup> ButtonGroups;
        private Dictionary<PrimitiveGroup, Color> ButtonColors;
        private Dictionary<PrimitiveGroup, Color> ButtonSelectedText = new Dictionary<PrimitiveGroup, Color>()
            {
                {PrimitiveGroup.Subroutine, Color.White},
                {PrimitiveGroup.Control, Color.FromArgb(0x22,0x22,0x22)},
                {PrimitiveGroup.Debug, Color.FromArgb(0x40,0x00,0x00)},
                {PrimitiveGroup.Math, Color.FromArgb(0x00,0x66,0x33)},
                {PrimitiveGroup.Sim, Color.FromArgb(0x4C,0x00,0x66)},
                {PrimitiveGroup.Object, Color.FromArgb(0x4C,0x00,0x66)},
                {PrimitiveGroup.Looks, Color.FromArgb(0x00,0x33,0x66)},
                {PrimitiveGroup.Position, Color.FromArgb(0x00,0x20,0x40)},
                {PrimitiveGroup.TSO, Color.FromArgb(0x3F,0x00,0x00)},
            };
        private PrimitiveGroup SelectedGroup = PrimitiveGroup.Control;

        private List<InstructionIDNamePair> CurrentFullList;
        private EditorScope Scope;
        private BHAV bhav;
        private IDETester Owner;

        private PrimitiveBox ActivePrim;

        private bool HasGameThread;

        public bool DebugMode;
        private VMEntity DebugEntity;

        public UIExternalContainer EditorLock { get { return EditorControl.FSOUI; } }
        public UIBHAVEditor Editor { get { return EditorControl.Editor; } }
        public BHAVContainer EditorCont { get { return EditorControl.Cont; } }

        public BHAVEditor()
        {
            InitializeComponent();
            ButtonGroups = new Dictionary<Button, PrimitiveGroup>()
            {
                {SubroutineBtn, PrimitiveGroup.Subroutine},
                {ControlBtn, PrimitiveGroup.Control},
                {DebugBtn, PrimitiveGroup.Debug},
                {MathBtn, PrimitiveGroup.Math},
                {SimBtn, PrimitiveGroup.Sim},
                {ObjectBtn, PrimitiveGroup.Object},
                {LooksBtn, PrimitiveGroup.Looks},
                {PositionBtn, PrimitiveGroup.Position},
                {TSOBtn, PrimitiveGroup.TSO},
                {AllBtn, PrimitiveGroup.All }
            };

            ButtonColors = new Dictionary<PrimitiveGroup, Color>();
            foreach (var btn in ButtonGroups)
            {
                ButtonColors.Add(btn.Value, btn.Key.BackColor);
                btn.Key.Click += PrimGroupChange;
            }
        }

        public BHAVEditor(BHAV bhav, EditorScope scope, IDETester owner) : this()
        {
            DebugMode = false;
            MainTable.ColumnStyles[2].SizeType = SizeType.Absolute;
            MainTable.ColumnStyles[2].Width = 0;

            Scope = scope;
            this.bhav = bhav;
            Owner = owner;

            Text = scope.GetFilename(scope.GetScopeFromID(bhav.ChunkID))+"::"+bhav.ChunkLabel;
            EditorControl.InitBHAV(bhav, scope, null, null, SelectionChanged);
            Editor.DisableDebugger += DisableDebugger;

            PrimGroupChange(AllBtn, null);
        }

        public BHAVEditor(VM vm, VMEntity entity, IDETester owner) : this()
        {
            DebugMode = true;
            DebugEntity = entity;
            Owner = owner;
            Text = "Tracer - " + entity.ToString() + " (ID " + entity.ObjectID + ")";
            
            UpdateStack();
            Editor.DisableDebugger += DisableDebugger;
        }

        private void UpdateStack()
        {
            var stack = DebugEntity.Thread.Stack;
            StackView.Items.Clear();
            int lastFrame = -1;
            for (int i = 0; i < stack.Count; i++)
            {
                var item = new ListViewItem(
                    new string[] { (stack[i] is VMRoutingFrame)?"<Routing Frame>":stack[i].Routine.Rti.Name, "filename"
                    });
                if (stack[i] is VMRoutingFrame)
                {
                    item.Tag = "route";
                    item.ForeColor = Color.Gray;
                }
                else lastFrame = i;
                StackView.Items.Add(item);
            }

            if (lastFrame != -1)
            {
                StackView.Items[lastFrame].Selected = true;
                SelectStackFrame(lastFrame);
            }
        }

        private void SelectStackFrame(int forceFrame)
        {
            if (forceFrame == -1 && StackView.SelectedItems.Count == 0) return;
            var frame = DebugEntity.Thread.Stack[(forceFrame != -1) ? forceFrame:StackView.Items.IndexOf(StackView.SelectedItems[0])];

            if (bhav != null && bhav.ChunkID == frame.Routine.Chunk.ChunkID && frame == Editor.DebugFrame) return;
            SetActivePrimitive(null);
            this.bhav = frame.Routine.Chunk;         
            Scope = new EditorScope(frame.CodeOwner, frame.Routine.Chunk);
            Scope.CallerObject = DebugEntity.Object;
            Scope.StackObject = (frame.StackObject == null)?null:frame.StackObject.Object;


            EditorControl.InitBHAV(bhav, Scope, DebugEntity, frame, SelectionChanged);

            ObjectDataGrid.SelectedObject = new PropGridVMData(Scope, DebugEntity, frame, Editor);
            ObjectDataGrid.Refresh();

            PrimGroupChange(AllBtn, null);
        }

        delegate void UpdateDebuggerDelegate();
        public void UpdateDebugger()
        {
            if (InvokeRequired)
            {
                var del = new UpdateDebuggerDelegate(UpdateDebugger);
                Invoke(del, null);
            }
            else
            {
                //does not need to be thread safe as this is invoked from UI thread.
                UpdateStack();
                Editor.NewBreak(Editor.DebugFrame);
                StackView.Enabled = true;
                ObjectDataGrid.Enabled = true;
            }
        }

        public delegate void DisableDebuggerDelegate();
        public void DisableDebugger()
        {
            if (InvokeRequired)
            {
                new Thread(() =>
                {
                    var del = new DisableDebuggerDelegate(DisableDebugger);
                    Invoke(del, null);
                }).Start();
            }
            else
            {
                StackView.Enabled = false;
                ObjectDataGrid.Enabled = false;
            }
        }

        private void PrimGroupChange(object sender, EventArgs e)
        {
            Button btn = (Button)sender;

            foreach (var cbtn in ButtonGroups)
            {
                var col = ButtonColors[cbtn.Value];
                if (cbtn.Key == btn) cbtn.Key.BackColor = Color.FromArgb((col.R * 128) / 255 + 127, (col.G * 128) / 255 + 127, (col.B * 128) / 255 + 127);
                else cbtn.Key.BackColor = col;
            }

            SelectedGroup = ButtonGroups[btn];

            //set primitive list to reflect the group

            CurrentFullList = new List<InstructionIDNamePair>();

            if (SelectedGroup == PrimitiveGroup.All)
            {
                for (ushort id = 0; id < 68; id++)
                {
                    var primName = EditorScope.Behaviour.Get<STR>(139).GetString(id);
                    CurrentFullList.Add(new InstructionIDNamePair((primName == null) ? "Primitive #" + id : primName, id));
                }
            }

            if (SelectedGroup == PrimitiveGroup.Subroutine || SelectedGroup == PrimitiveGroup.All)
            {
                if (bhav.ChunkID > 4095)
                {
                    if (bhav.ChunkID < 8192) CurrentFullList.AddRange(Scope.GetAllSubroutines(ScopeSource.Private));
                    CurrentFullList.AddRange(Scope.GetAllSubroutines(ScopeSource.SemiGlobal));
                }
                CurrentFullList.AddRange(Scope.GetAllSubroutines(ScopeSource.Global));
            }
            else
            {
                var prims = PrimitiveRegistry.PrimitiveGroups[SelectedGroup];
                foreach (var id in prims)
                {
                    var primName = EditorScope.Behaviour.Get<STR>(139).GetString(id);
                    CurrentFullList.Add(new InstructionIDNamePair((primName == null) ? "Primitive #" + id : primName, id));
                }
            }

            SearchBox.Text = "";
            UpdatePrimitiveList();
        }

        private void UpdatePrimitiveList()
        {
            var searchString = new Regex(".*" + SearchBox.Text.ToLower() + ".*");
            PrimitiveList.ClearSelected();
            lock (EditorLock) Editor.ClearPlacement();
            PrimitiveList.Items.Clear();

            foreach (var prim in CurrentFullList)
            {
                if (searchString.IsMatch(prim.ToString().ToLower())) PrimitiveList.Items.Add(prim);
            }
        }

        public void SelectionChanged(List<PrimitiveBox> sel)
        {
            if (sel.Count > 0)
            {
                SetActivePrimitive(sel[0]);
            }
        }

        delegate void SetActiveDelegate(PrimitiveBox prim);
        public void SetActivePrimitive(PrimitiveBox prim)
        {
            if (InvokeRequired)
            {
                
                //HasGameThread = true;

                new Thread(() =>
                {
                    var del = new SetActiveDelegate(SetActivePrimitive);
                    Invoke(del, new object[] { prim });
                }).Start();
                //HasGameThread = false;
            }
            else
            {
                var panel = OperandEditTable;
                panel.Controls.Clear();
                panel.RowCount = 0;
                panel.RowStyles.Clear();
                if (prim == null || prim.Descriptor == null || ActivePrim == prim) return;
                ActivePrim = prim;
                for (int i=0; i<10; i++) panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                prim.Descriptor.PopulateOperandView(this, EditorCont.Scope, panel);
            }
        }

        public void SignalOperandUpdate()
        {
            var panel = OperandEditTable;
            foreach (IOpControl cont in panel.Controls)
            {
                cont.OperandUpdated();
            }

            if (HasGameThread) Editor.UpdateOperand(ActivePrim);
            else
            {
                lock (EditorLock) Editor.UpdateOperand(ActivePrim);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PrimitiveList.SelectedItem == null) return;
            lock (EditorLock) Editor.SetPlacement(((InstructionIDNamePair)PrimitiveList.SelectedItem).ID);
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            UpdatePrimitiveList();
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void OperandScroller_Resize(object sender, EventArgs e)
        {
            OperandScroller.HorizontalScroll.Enabled = false;
            OperandScroller.HorizontalScroll.Visible = false;
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lock (EditorLock)
            {
                Editor.UndoRedoDir++;
            }
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lock (EditorLock)
            {
                Editor.UndoRedoDir--;
            }
        }

        private void StackView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (StackView.SelectedItems.Count > 0 && StackView.SelectedItems[0].Tag == null) SelectStackFrame(-1);
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void BHAVEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DebugMode)
            {
                //resume thread, does this need to be thread safe?
                DebugEntity.Thread.ThreadBreak = SimAntics.Engine.VMThreadBreakMode.Active;
                Owner.UnregisterDebugger(DebugEntity);
            }
        }
    }
}
