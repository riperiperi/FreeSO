using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using FSO.Files.Formats.tsodata;

namespace FSO.IDE.ContentEditors
{
    public partial class TSODataDefinitionEditor : Form
    {
        public TSODataDefinition Data;
        private object _CurrentSelection;
        public object CurrentSelection
        {
            get
            {
                return _CurrentSelection;
            }
            set
            {
                if (value != null)
                {
                    if (value is ListEntryEntry)
                    {
                        if (DataViewTabs.SelectedTab == StructDTab)
                            PropGrid.SelectedObject = new TSODataMaskWrapper((ListEntryEntry)value);
                        else
                            PropGrid.SelectedObject = new TSODataStructWrapper((ListEntryEntry)value);
                    }
                    else
                        PropGrid.SelectedObject = new TSODataDefinitionWrapper((ListEntry)value);
                }
                _CurrentSelection = value;

                Delete.Enabled = (value != null);
                NewChild.Enabled = (value != null);
            }
        }

        public Dictionary<TreeNode, object> TreeToObject = new Dictionary<TreeNode, object>();

        public TSODataDefinitionEditor()
        {
            InitializeComponent();
        }

        private void TSODataDefinitionEditor_Load(object sender, EventArgs e)
        {
            Reload();
        }

        private string TypeName(ListEntryEntry child)
        {
            var typeName = Data.GetString(child.TypeStringID);
            switch ((StructFieldClassification)child.TypeClass)
            {
                case StructFieldClassification.List:
                    return typeName + "[]";
                case StructFieldClassification.Map:
                    return "Map<uint, " + typeName + ">";
                default:
                    return typeName;
            }
        }

        private TreeNode RenderListEntry(ListEntry entry, string fieldName, string typeName, bool addTTO)
        {
            //let's get our children
            var children = new List<TreeNode>();
            foreach (var child in entry.Entries)
            {
                var type = Data.GetStringType(child.TypeStringID);
                TreeNode node;
                if (type == StringTableType.Field)
                {
                    node = new TreeNode(Data.GetString(child.NameStringID) + " (" + ((DerivedStructFieldMaskType)child.TypeClass).ToString() + ")");
                }
                else if (type == StringTableType.Primitive || type == StringTableType.Field)
                {
                    node = new TreeNode(Data.GetString(child.NameStringID) + " ("+TypeName(child)+")");
                }
                else
                {
                    var lentry = Data.List1.FirstOrDefault(x => x.NameStringID == child.TypeStringID);
                    if (entry == null)
                        lentry = Data.List2.FirstOrDefault(x => x.NameStringID == child.TypeStringID);

                    node = RenderListEntry(lentry, Data.GetString(child.NameStringID), TypeName(child), addTTO);
                }
                if (addTTO) TreeToObject.Add(node, child);
                children.Add(node);
            }
            if (fieldName == null)
            {
                var name = Data.GetString(entry.NameStringID);
                if (entry.ParentStringID != 0) name += " [0x"+entry.NameStringID.ToString("X8")+"] (" + Data.GetString(entry.ParentStringID) + ")";
                return new TreeNode(name, children.ToArray());
            }
            else
                return new TreeNode(fieldName + " (" + typeName + ")", children.ToArray());
        }

        private void Reload()
        {
            Data = Content.Content.Get().DataDefinition;

            //populate the tree views
            TreeToObject.Clear();

            ReloadTree(TreeView1S, Data.List1);
            ReloadTree(TreeView2S, Data.List2);
            ReloadTree(TreeViewDS, Data.List3);
        }

        private void ReloadTree(TreeView tree, List<ListEntry> list)
        {
            //roughly clear TreeToObject. this will leak child nodes, though I don't think it's too much 
            //of a problem for a niche editor like this.
            foreach (var node in tree.Nodes)
            {
                TreeToObject.Remove((TreeNode)node);
            }
            tree.Nodes.Clear();

            foreach (var elem in list)
            {
                var node = RenderListEntry(elem, null, null, true);
                TreeToObject.Add(node, elem);
                tree.Nodes.Add(node);
            }
        }

        private void SoftReloadTree(TreeView tree, List<ListEntry> list)
        {
            //only reload names. currently hijacks the full regen
            var current = new List<TreeNode>();
            foreach (TreeNode node in tree.Nodes)
                current.Add(node);
            var compare = list.Select(x => RenderListEntry(x, null, null, false)).ToList();

            RecursiveTextCopy(compare, current);
        }

        private void RecursiveTextCopy(List<TreeNode> src, List<TreeNode> dest)
        {
            var min = Math.Min(src.Count, dest.Count);
            for (int i=0; i<min; i++)
            {
                dest[i].Text = src[i].Text;
                var current = new List<TreeNode>();
                foreach (TreeNode node in dest[i].Nodes) current.Add(node);
                var compare = new List<TreeNode>();
                foreach (TreeNode node in src[i].Nodes) compare.Add(node);

                RecursiveTextCopy(compare, current);
            }
        }


        private TreeNode TreeWhere(List<TreeNode> src, Func<TreeNode, bool> predicate)
        {
            for (int i = 0; i < src.Count; i++)
            {
                if (predicate(src[i])) return src[i];

                var current = new List<TreeNode>();
                foreach (TreeNode node in src[i].Nodes) current.Add(node);

                var childMatch = TreeWhere(current, predicate);
                if (childMatch != null) return childMatch;
            }
            return null;
        }

        private void TreeView1S_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (TreeView1S.SelectedNode == null) return;
            var obj = TreeToObject[TreeView1S.SelectedNode];
            CurrentSelection = obj;

            TreeView2S.SelectedNode = null;
            TreeViewDS.SelectedNode = null;
        }

        private void TreeView2S_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (TreeView2S.SelectedNode == null) return;
            var obj = TreeToObject[TreeView2S.SelectedNode];
            CurrentSelection = obj;

            TreeView1S.SelectedNode = null;
            TreeViewDS.SelectedNode = null;
        }

        private void TreeViewDS_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (TreeViewDS.SelectedNode == null) return;
            var obj = TreeToObject[TreeViewDS.SelectedNode];
            CurrentSelection = obj;

            TreeView1S.SelectedNode = null;
            TreeView2S.SelectedNode = null;
        }

        private uint GetNewNameID(StringTableType type)
        {
            var rand = new Random();
            var id = (uint)rand.Next();
            while (Data.Strings.Any(x => x.ID == id))
            {
                //no dupes
                id = (uint)rand.Next();
            }
            Data.Strings.Add(new StringTableEntry()
            {
                Category = type,
                ID = id,
                Value = "NewDefinition"
            });
            return id;
        }

        private void NewRoot_Click(object sender, EventArgs e)
        {
            var treeT = GetCurrentTree();
            var tree = treeT.Item1;
            var list = treeT.Item2;
            var table = treeT.Item3;

            var entry = new ListEntry()
            {
                NameStringID = GetNewNameID(table),
                ParentStringID = 0,
                Entries = new List<ListEntryEntry>(),
                Parent = Data
            };
            list.Add(entry);
            ReloadTree(tree, list);

            var current = new List<TreeNode>();
            foreach (TreeNode node in tree.Nodes)
                current.Add(node);

            tree.SelectedNode = TreeWhere(current, x => TreeToObject[x] == entry);
        }

        private void NewChild_Click(object sender, EventArgs e)
        {
            var treeT = GetCurrentTree();
            var tree = treeT.Item1;
            var list = treeT.Item2;
            var table = treeT.Item3;

            var item = TreeToObject[tree.SelectedNode];
            bool reload1 = false;
            ListEntry listItem;
            if (item is ListEntry)
                listItem = (ListEntry)item;
            else
            {
                var entry2 = (ListEntryEntry)item;
                //find the real class for this type
                listItem = Data.List1.FirstOrDefault(x => x.NameStringID == entry2.TypeStringID);

                //really should not allow this under normal circumstances, but we assume the user knows what they are doing.
                if (listItem == null) 
                    listItem = Data.List2.FirstOrDefault(x => x.NameStringID == entry2.TypeStringID);

                if (listItem == null)
                    return; //this is likely a primitive type. can't add child fields to a primitive type.
                reload1 = true;
            }
            
            var entry = new ListEntryEntry()
            {
                NameStringID = (table == StringTableType.Derived)?0:GetNewNameID(StringTableType.Field),
                Parent = listItem,
            };
            listItem.Entries.Add(entry);
            ReloadTree(tree, list);
            ReloadTree(TreeView1S, Data.List1);

            var current = new List<TreeNode>();
            foreach (TreeNode node in tree.Nodes)
                current.Add(node);

            tree.SelectedNode = TreeWhere(current, x => TreeToObject[x] == entry);
        }

        private int NodeDepth(TreeNode node)
        {
            int depth = 0;
            while (node != null)
            {
                depth++;
                node = node.Parent;
            }
            return depth;
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            if (CurrentSelection == null) return;

            var treeT = GetCurrentTree();
            var tree = treeT.Item1;
            var list = treeT.Item2;
            var table = treeT.Item3;

            if (CurrentSelection is ListEntryEntry)
            {
                var ee = (ListEntryEntry)CurrentSelection;
                ee.Parent.Parent.RemoveString(ee.NameStringID);
                ee.Parent.Entries.Remove(ee);
            } else
            {
                var entry = (ListEntry)CurrentSelection;
                entry.Parent.RemoveString(entry.NameStringID);
                list.Remove(entry);
            }

            ReloadTree(tree, list);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "This will save over the default FreeSO data service definitions. " +
                "(Content/FSODataDefinition.dat) " +
                "These won't be automatically activated in this instance of FreeSO, " +
                "but they will be when the game restarts. \r\n\r\n" +
                "Are you sure you want to save here?", "Warning", MessageBoxButtons.YesNo);
            if (result == DialogResult.No) return;

            try
            {
                using (var str = new FileStream("Content/FSODataDefinition.dat", FileMode.Create, FileAccess.Write))
                    Data.Write(str);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Couldn't save the file! Exception: " + ex.Message);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "Saving Data Service Definitions...";
            dialog.FileName = "FSODataDefinition.dat";
            SaveFile(dialog);

            try
            {
                Stream str;
                if ((str = dialog.OpenFile()) != null)
                {
                    Data.Write(str);
                    str.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Couldn't save the file! Exception: " + ex.Message);
            }
        }

        private void SaveFile(FileDialog dialog)
        {
            // ༼ つ ◕_◕ ༽つ IMPEACH STAThread ༼ つ ◕_◕ ༽つ
            var wait = new AutoResetEvent(false);
            var thread = new Thread(() => {
                dialog.ShowDialog();
                wait.Set();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            wait.WaitOne();
            return;
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Select a Data Service Definition file. (dat)";
            SaveFile(dialog);
            try
            {
                Stream str;
                if ((str = dialog.OpenFile()) != null)
                {
                    var tsoData = new TSODataDefinition();
                    tsoData.Read(str);

                    Content.Content.Get().DataDefinition = tsoData;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Couldn't load the file! Exception: " + ex.Message);
            }
            Reload();
        }
        
        private void activateIngameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Data.Activate();
            MessageBox.Show("New definitions activated!");
        }

        private Tuple<TreeView, List<ListEntry>, StringTableType> GetCurrentTree()
        {
            if (DataViewTabs.SelectedTab == Struct1Tab)
                return new Tuple<TreeView, List<ListEntry>, StringTableType>(TreeView1S, Data.List1, StringTableType.Level1);
            else if (DataViewTabs.SelectedTab == Struct2Tab)
                return new Tuple<TreeView, List<ListEntry>, StringTableType>(TreeView2S, Data.List2, StringTableType.Level2);
            else if (DataViewTabs.SelectedTab == StructDTab)
                return new Tuple<TreeView, List<ListEntry>, StringTableType>(TreeViewDS, Data.List3, StringTableType.Derived);
            return null;
        }

        private void PropGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            //redraw the relevant property in the grid
            var tree = GetCurrentTree();
            SoftReloadTree(tree.Item1, tree.Item2);
        }
    }
}
