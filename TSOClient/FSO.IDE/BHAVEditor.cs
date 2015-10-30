using FSO.Client;
using FSO.Client.UI.Framework;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent;
using FSO.IDE.EditorComponent.Model;
using FSO.IDE.EditorComponent.OperandForms;
using FSO.IDE.EditorComponent.UI;
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

        private PrimitiveBox ActivePrim;

        private bool HasGameThread;

        public UIExternalContainer EditorLock { get { return EditorControl.FSOUI; } }
        public UIBHAVEditor Editor { get { return EditorControl.Editor; } }
        public BHAVContainer EditorCont { get { return EditorControl.Cont; } }

        public BHAVEditor(BHAV bhav, EditorScope scope)
        {
            Scope = scope;
            this.bhav = bhav;
            InitializeComponent();

            Text = scope.GetFilename(scope.GetScopeFromID(bhav.ChunkID))+"::"+bhav.ChunkLabel.Trim('\0');
            EditorControl.InitBHAV(bhav, scope);

            PrimitiveList.Items.AddRange(scope.GetAllSubroutines(ScopeSource.Private).ToArray());

            /*PrimitiveList.Items.Add("Generic Sims Online Call");
            PrimitiveList.Items.Add("Sleep");
            PrimitiveList.Items.Add("Idle for Input");
            PrimitiveList.Items.Add("Notify Stack Object out of Idle");
            PrimitiveList.Items.Add("Push Interaction");
            PrimitiveList.Items.Add("Find Best Object For Function");
            PrimitiveList.Items.Add("Run Functional Tree");
            PrimitiveList.Items.Add("Run Tree By Name");
            PrimitiveList.Items.Add("Add / Change Action String");*/

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

            PrimGroupChange(AllBtn, null);
            EditorCont.OnSelectedChanged += SelectionChanged;
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
                if (prim == null || prim.Descriptor == null || ActivePrim == prim) return;
                ActivePrim = prim;
                var panel = OperandEditTable;
                panel.Controls.Clear();
                panel.RowCount = 0;
                panel.RowStyles.Clear();
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
    }
}
