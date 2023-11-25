using FSO.SimAntics.Engine.Scopes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace FSO.IDE.EditorComponent
{
    public partial class VarScopeSelect : Form
    {
        public static string[] Categories =
        {
            "My",
            "Stack Object's",
            "World",
            "Unused"
        };

        public static ScopeDefinition[] Definitions =
        {
            new ScopeDefinition(7, "Literal", ScopeGroup.Other,
                "Literal number. Read-only."),
            new ScopeDefinition(8, "Temps", ScopeGroup.Other,
                "Accesses the thread's temporary variables."),
            new ScopeDefinition(11, "Temps[temp]", ScopeGroup.Other,
                "Accesses the temp indexed by the value in another temp."),
            new ScopeDefinition(9, "Parameters", ScopeGroup.Other,
                "Accesses the parameters of this stack frame (subroutine)."),
            new ScopeDefinition(25, "Locals", ScopeGroup.Other,
                "Accesses the local variables of this stack frame (subroutine)."),
            new ScopeDefinition(40, "Locals[temp]", ScopeGroup.Other,
                "Accesses the local variables of this stack frame, using the specified temp as an index."),
            new ScopeDefinition(42, "TempXL", ScopeGroup.Other,
                "Accesses the thread's 32-bit temporary variables. Can be used for 32-bit calculation, mostly for money values."),
            new ScopeDefinition(26, "Tuning", ScopeGroup.Other,
                "Accesses the tuning constants relative to this BHAV. Temporary System."),

            new ScopeDefinition(0, "Attributes", ScopeGroup.My,
                "Accesses the Attributes of the caller object. Attributes are object specific state."),
            new ScopeDefinition(3, "Object Data", ScopeGroup.My,
                "Accesses the Object Data of the caller object. Fields are the same for all objects."),
            new ScopeDefinition(18, "Person Data", ScopeGroup.My,
                "Accesses the Person Data of the caller avatar. Avatars only."),
            new ScopeDefinition(30, "Person Data[temp]", ScopeGroup.My,
                "Accesses the Person Data of the caller avatar, using the specified temp as an index."),
            new ScopeDefinition(14, "Motives", ScopeGroup.My,
                "Accesses the motives of the caller avatar. Avatars only."),
            new ScopeDefinition(20, "Slot", ScopeGroup.My,
                "Accesses the contained ID of each slot on the caller object. 0 for empty."),
            new ScopeDefinition(36, "Type Attributes", ScopeGroup.My,
                "No description as of right now."),
            new ScopeDefinition(46, "List", ScopeGroup.My,
                "Accesses the Linked List of the caller object."),
            new ScopeDefinition(49, "Lead Tile Attributes", ScopeGroup.My,
                "Accesses the Attributes of the caller object's Lead Tile."),
            new ScopeDefinition(51, "Lead Tile Object Data", ScopeGroup.My,
                "Accesses the Object Data of the caller object's Lead Tile."),
            new ScopeDefinition(48, "Money Over Head (32-bit)", ScopeGroup.My,
                "Accesses a 32-bit store for the money display over the caller avatar's head."),
            new ScopeDefinition(59, "Avatar ID", ScopeGroup.My,
                "Accesses the Persistent ID of the caller avatar."),
            

            new ScopeDefinition(10, "ID", ScopeGroup.StackObject,
                "Accesses the ID of the current Stack Object."),
            new ScopeDefinition(1, "Attributes", ScopeGroup.StackObject,
                "Accesses the Attributes of the stack object. Attributes are object specific state."),
            new ScopeDefinition(41, "Attributes[temp]", ScopeGroup.StackObject,
                "Accesses the Attributes of the stack object, using the specified temp as an index."),
            new ScopeDefinition(22, "Attributes[param]", ScopeGroup.StackObject,
                "Accesses the Attribute of the Stack Object specified by the given parameter."),
            new ScopeDefinition(4, "Object Data", ScopeGroup.StackObject,
                "Accesses the Object Data of the stack object. Fields are the same for all objects."),
            new ScopeDefinition(19, "Person Data", ScopeGroup.StackObject,
                "Accesses the Person Data of the avatar in the stack object."),
            new ScopeDefinition(31, "Person Data[temp]", ScopeGroup.StackObject,
                "Accesses the Person Data of the stack avatar, using the specified temp as an index."),
            new ScopeDefinition(15, "Motives", ScopeGroup.StackObject,
                "Accesses the motives of the avatar in the stack object. Avatars only."),
            new ScopeDefinition(17, "Motives[temp]", ScopeGroup.StackObject,
                "Accesses the motive of the avatar in the stack object indexed by a temp."),
            new ScopeDefinition(16, "Slot", ScopeGroup.StackObject,
                "Accesses the contained ID of each slot on the stack object. 0 for empty."),
            new ScopeDefinition(37, "Type Attributes", ScopeGroup.StackObject,
                "No description as of right now."),
            new ScopeDefinition(47, "List", ScopeGroup.StackObject,
                "Accesses the Linked List of the stack object."),
            new ScopeDefinition(50, "Lead Tile Attributes", ScopeGroup.StackObject,
                "Accesses the Attributes of the stack object's Lead Tile."),
            new ScopeDefinition(52, "Lead Tile Object Data", ScopeGroup.StackObject,
                "Accesses the Object Data of the stack object's Lead Tile."),
            new ScopeDefinition(21, "Definition", ScopeGroup.StackObject,
                "Accesses fields from the OBJD of the stack object. Read-only."),
            new ScopeDefinition(53, "Master Definition", ScopeGroup.StackObject,
                "Accesses fields from the Master OBJD of the stack object. Same as Definition if object is not Multi-tile. Read-only."),
            new ScopeDefinition(27, "Dynamic Sprite Flag[temp 0]", ScopeGroup.StackObject,
                "Dynamic sprite flag of the stack object indexed by temp 0. Toggles a DRGP layer, 1/0."), 
            new ScopeDefinition(35, "Function", ScopeGroup.StackObject,
                "No description as of right now."),
            new ScopeDefinition(13, "Temps", ScopeGroup.StackObject,
                "Accesses the temporary varaibles of the stack object's thread."),

            new ScopeDefinition(6, "Globals", ScopeGroup.World,
                "Accesses global simulation state values, generally unused."),
            new ScopeDefinition(23, "Room[temp 0]", ScopeGroup.World,
                "Accesses information relating to the room indexed by the value of temp 0."),
            new ScopeDefinition(33, "Job Data[temp 0, 1]", ScopeGroup.World,
                "No description as of right now."),
            new ScopeDefinition(34, "Neighborhood Data", ScopeGroup.World,
                "No description as of right now."),
            new ScopeDefinition(43, "City Time", ScopeGroup.World,
                "Accesses the ingame time. Read-only."),
            new ScopeDefinition(44, "TSO Standard Time", ScopeGroup.World,
                "Accesses the real world time as defined by the city server."),
            new ScopeDefinition(45, "Game Time", ScopeGroup.World,
                "Accesses the ingame time. Read-only. (compatibility alias for City Time)"),
            new ScopeDefinition(54, "Feature Enable Level", ScopeGroup.World,
                "Accesses information on which features are currently enabled. Read-only."),

            new ScopeDefinition(2, "Target Object Attributes", ScopeGroup.Unused,
                "Would access the Attributes of the target object, likely the callee."),
            new ScopeDefinition(5, "Target Object Data", ScopeGroup.Unused,
                "Would access the Object Data of the target object, likely the callee."),
            new ScopeDefinition(12, "Check Tree Ad Range", ScopeGroup.Unused,
                "No description as of right now."),
            new ScopeDefinition(24, "Neighbor in Stack Object", ScopeGroup.Unused,
                "No description as of right now."),
            new ScopeDefinition(28, "Check Tree Ad Personality", ScopeGroup.Unused,
                "No description as of right now."),
            new ScopeDefinition(29, "Check Tree Ad Min", ScopeGroup.Unused,
                "No description as of right now."),
            new ScopeDefinition(32, "Neighbor's Person Data", ScopeGroup.Unused,
                "No description as of right now."),
            new ScopeDefinition(38, "Unused 38", ScopeGroup.Unused,
                "No description as of right now."),
            new ScopeDefinition(39, "Unused 39", ScopeGroup.Unused,
                "No description as of right now."),
        };


        public List<List<ScopeDefinition>> CategorisedScopes;
        public Dictionary<int, ScopeDefinition> SourceIDToDef;

        public Dictionary<TreeNode, ScopeDefinition> SourceNodeToDef;

        public List<TreeNode> VisibleNodes;

        public TreeNode OldSelect;
        public ScopeDefinition SelectedDef;
        public byte SelectedSource
        {
            get
            {
                return SelectedDef.ID;
            }
        }
        public short SelectedData {
            set { DataValue.Value = value; }
            get { return (short)DataValue.Value; }
        }

        private EditorScope Scope;

        private bool ManDataListChange;
        private bool ManDataValChange;

        public VarScopeSelect()
        {
            InitializeComponent();
            SourceSearch.Focus();
        }

        public VarScopeSelect(EditorScope scope, byte source, short data) : this()
        {
            Scope = scope;
            
            CategorisedScopes = new List<List<ScopeDefinition>>();
            for (int i = 0; i < 5; i++)
            {
                CategorisedScopes.Add(new List<ScopeDefinition>());
            }
            SourceIDToDef = new Dictionary<int, ScopeDefinition>();
            foreach (var def in Definitions)
            {
                if (def.ID == source) SelectedDef = def;
                SourceIDToDef.Add(def.ID, def);
                CategorisedScopes[(int)(def.Group + 1)].Add(def);
            }

            RefreshScopeTree();
            SelectedData = data;
        }

        public void RefreshScopeTree()
        {
            VisibleNodes = new List<TreeNode>();
            string[] searchTerms = (SourceSearch.Text == "") ? null : SourceSearch.Text.ToLowerInvariant().Split(' ');
            SourceNodeToDef = new Dictionary<TreeNode, ScopeDefinition>();
            SourceTree.Nodes.Clear();
            TreeNode firstNode = null;

            var nodes = new List<TreeNode>();
            foreach (var def in CategorisedScopes[0])
            {
                if (searchTerms == null || def.SearchMatch(searchTerms))
                {
                    var node = new TreeNode(def.Name);
                    if (firstNode == null) firstNode = node;
                    SourceNodeToDef.Add(node, def);
                    VisibleNodes.Add(node);
                    nodes.Add(node);
                    if (def == SelectedDef) SourceTree.SelectedNode = node;
                }
            }
            SourceTree.Nodes.AddRange(nodes.OrderByDescending(node => SourceNodeToDef[node].LastSearchScore).ToArray());

            for (int i=0; i<4; i++)
            {
                var parent = new TreeNode(Categories[i]);
                nodes = new List<TreeNode>();
                foreach (var def in CategorisedScopes[i+1])
                {
                    if (searchTerms == null || def.SearchMatch(searchTerms))
                    {
                        var node = new TreeNode(def.Name);
                        if (firstNode == null) firstNode = node;
                        SourceNodeToDef.Add(node, def);
                        VisibleNodes.Add(node);
                        nodes.Add(node);
                        if (def == SelectedDef) SourceTree.SelectedNode = node;
                    }
                }
                parent.Nodes.AddRange(nodes.OrderByDescending(node => SourceNodeToDef[node].LastSearchScore).ToArray());

                if (parent.Nodes.Count > 0) SourceTree.Nodes.Add(parent);
            }
            SourceTree.ExpandAll();

            if (SourceTree.SelectedNode == null) SourceTree.SelectedNode = firstNode;
            if (SourceTree.Nodes.Count > 0) SourceTree.Nodes[0].EnsureVisible();
        }

        public void RefreshDataList(bool skipSearch)
        {
            string[] searchTerms = (skipSearch || DataSearch.Text == "") ? null : DataSearch.Text.ToLowerInvariant().Split(' ');

            DataList.Items.Clear();
            if (SelectedDef == null)
            {
                DataList.Visible = false;
                DataValue.Enabled = false;
                DataDesc.Text = "";
            }
            else
            {
                var defs = Scope.GetVarScopeDataNames((VMVariableScope)SelectedDef.ID);

                DataValue.Enabled = true;
                if (defs == null)
                {
                    DataDesc.Text = "";
                    DataList.Visible = false;
                }
                else
                {
                    DataList.Visible = true;
                    foreach (var def in defs)
                    {
                        if (searchTerms == null || def.SearchMatch(searchTerms))
                        {
                            DataList.Items.Add(def);
                            if (def.Value == SelectedData) DataList.SelectedItem = def;
                        }
                    }
                    if (DataList.SelectedIndex == -1 && DataList.Items.Count>0) DataList.SelectedIndex = 0;
                }
            }
        }

        private void SourceSearch_TextChanged(object sender, EventArgs e)
        {
            RefreshScopeTree();
        }

        private void SourceTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;
            if (!SourceNodeToDef.ContainsKey(e.Node))
            {
                SourceTree.SelectedNode = OldSelect;
            }
            else
            {
                OldSelect = e.Node;
                var def = SourceNodeToDef[e.Node];
                SourceDesc.Text = def.Description;
                SelectedDef = def;
            }
            DataSearch.Text = "";
            RefreshDataList(true);
        }

        private void DataValue_ValueChanged(object sender, EventArgs e)
        {
            if (ManDataValChange) return;
            ManDataListChange = true;
            foreach (ScopeDataDefinition def in DataList.Items)
            {
                if (def.Value == SelectedData)
                {
                    DataList.SelectedItem = def;
                    ManDataListChange = false; return;
                }
            }
            DataList.SelectedItem = null;
            ManDataListChange = false;
        }

        private void DataList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (DataList.SelectedItem == null)
            {
                DataDesc.Text = "";
                return;
            }
            var def = ((ScopeDataDefinition)DataList.SelectedItem);
            DataDesc.Text = def.Description;
            if (ManDataListChange) return;
            ManDataValChange = true;
            SelectedData = def.Value;
            ManDataValChange = false;
        }

        private void DataSearch_TextChanged(object sender, EventArgs e)
        {
            if (DataSearch.Text != "")
            {
                short data;
                bool parsed = short.TryParse(DataSearch.Text, out data);
                if (parsed)
                {
                    SelectedData = data;
                    RefreshDataList(true);
                } else RefreshDataList(false);
            } else
            {
                RefreshDataList(true);
            }
        }

        private void SourceSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                ScrollTree(1);
            }
            else if (e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                ScrollTree(-1);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                DataSearch.Focus();
            }
        }

        private void ScrollTree(int diff)
        {
            int index = VisibleNodes.IndexOf(SourceTree.SelectedNode);
            index = Math.Min(VisibleNodes.Count - 1, Math.Max(0, index + diff));
            SourceTree.SelectedNode = VisibleNodes[index];
        }

        private void DataSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (DataList.SelectedIndex < DataList.Items.Count-1) DataList.SelectedIndex++;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (DataList.SelectedIndex > 0) DataList.SelectedIndex--;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                Accept();
            }
        }

        public void Accept()
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void SourceTree_Enter(object sender, EventArgs e)
        {
        }

        private void DataList_Enter(object sender, EventArgs e)
        {
        }

        private void SourceTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                ScrollTree(1);
            }
            else if (e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                ScrollTree(-1);
            }
            else if (e.KeyCode == Keys.Enter)
            {
                DataSearch.Focus();
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            Accept();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

    public class ScopeDefinition
    {
        public byte ID;
        public string Name;
        public ScopeGroup Group;
        public string Description;

        public int LastSearchScore;
        private string Total;

        public ScopeDefinition(byte id, string name, ScopeGroup group, string desc)
        {
            ID = id;
            Name = name;
            Group = group;
            Description = desc;
            Total = (((Group == ScopeGroup.Other) ? "" : VarScopeSelect.Categories[(int)Group] + " ") + Name).ToLowerInvariant();
        }

        public override string ToString()
        {
            return Name;
        }

        public bool SearchMatch(string[] words)
        {
            LastSearchScore = 0;
            foreach (var word in words)
            {
                if (Total.Contains(word)) LastSearchScore++;
            }
            return (LastSearchScore>=words.Length);
        }
    }

    public enum ScopeGroup : int
    {
        Other = -1,
        My = 0,
        StackObject = 1,
        World = 2,
        Unused = 3
    }
}
