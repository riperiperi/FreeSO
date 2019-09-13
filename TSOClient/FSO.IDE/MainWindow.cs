using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.IDE.Common;
using FSO.IDE.ContentEditors;
using FSO.IDE.Managers;
using FSO.SimAntics;
using FSO.SimAntics.JIT.Translation.CSharp;
using FSO.SimAntics.NetPlay.Model.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE
{
    public partial class MainWindow : Form
    {
        public static MainWindow Instance;

        public VM HookedVM;
        public IffEditManager IffManager = new IffEditManager();
        public BHAVEditManager BHAVManager = new BHAVEditManager();

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
        }
        public void Test(VM vm)
        {
            ObjectRegistry.Init();
            this.HookedVM = vm;
            entityInspector1.ChangeVM(vm);
            Browser.RefreshTree();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (Browser.SelectedFile == null) return;
            uint GUID;
            if (Browser.SelectedObj == null)
                GUID = ObjectRegistry.MastersByFilename[Browser.SelectedFile][0].GUID;
            else
                GUID = Browser.SelectedObj.GUID;

            IffManager.OpenResourceWindow(Content.Content.Get().WorldObjects.Get(GUID));
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            if (Browser.SelectedFile == null) return;
            uint GUID;
            if (Browser.SelectedObj == null)
                GUID = ObjectRegistry.MastersByFilename[Browser.SelectedFile][0].GUID;
            else
                GUID = Browser.SelectedObj.GUID;

            HookedVM.SendCommand(new VMNetBuyObjectCmd
            {
                Verified = true,
                GUID = GUID,
                dir = LotView.Model.Direction.NORTH,
                level = HookedVM.Context.World.State.Level,
                x = (short)(((short)Math.Floor(HookedVM.Context.World.State.CenterTile.X) << 4) + 8),
                y = (short)(((short)Math.Floor(HookedVM.Context.World.State.CenterTile.Y) << 4) + 8),
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private Dictionary<ToolStripItem, Form> WindowButtons;

        private bool AddForms(bool lastHad, IEnumerable<object> forms)
        {
            var items = windowToolStripMenuItem.DropDownItems;
            if (lastHad && forms.Count() > 0) {
                items.Add(new ToolStripSeparator());
                lastHad = false;
            }

            foreach (var res in forms)
            {
                var form = (Form)res;
                var item = new ToolStripMenuItem(form.Text);
                WindowButtons.Add(item, form);
                item.Click += WindowItem_Click;
                items.Add(item);
            }

            return (lastHad || forms.Count() > 0);
        }

        private void windowToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var items = windowToolStripMenuItem.DropDownItems;
            while (items.Count > 2)
                items.RemoveAt(items.Count - 1);

            WindowButtons = new Dictionary<ToolStripItem, Form>();

            bool prevHadAny = AddForms(false, IffManager.ResourceWindow.Values);
            prevHadAny = AddForms(prevHadAny, BHAVManager.Editors.Values);
            prevHadAny = AddForms(prevHadAny, BHAVManager.Tracers.Values);
        }

        private void WindowItem_Click(object sender, EventArgs e)
        {
            Form form = null;
            WindowButtons.TryGetValue((ToolStripItem)sender, out form);
            if (form != null)
            {
                if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
                form.Activate();
            }
        }

        private void hideAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Form form in IffManager.ResourceWindow.Values) form.WindowState = FormWindowState.Minimized;
            foreach (Form form in BHAVManager.Editors.Values) form.WindowState = FormWindowState.Minimized;
            foreach (Form form in BHAVManager.Tracers.Values) form.WindowState = FormWindowState.Minimized;
        }

        public void RefreshResourceView()
        {
            //refresh resource view
            ChangesView.Nodes.Clear();
            SelectedChunks.Clear();
            SelectedIffs.Clear();
            ResNodes.Clear();
            UpdateSelectedRes();
            var mod = Content.Content.Get().Changes;
            var changes = mod.GetChangeList();

            int chunkChange = 0;

            foreach (var iff in changes)
            {
                var node = new ResChangeNode(iff);
                foreach (var chunk in iff.ListAll())
                {
                    if (chunk.RuntimeInfo == ChunkRuntimeState.Modified
                     || chunk.RuntimeInfo == ChunkRuntimeState.Delete)
                    {
                        var cnode = new ResChangeNode(chunk);
                        node.Nodes.Add(cnode);
                        ResNodes.Add(cnode);
                        chunkChange++;
                    }
                }
                ResNodes.Add(node);
                ChangesView.Nodes.Add(node);
            }

            ChangesView.ExpandAll();

            if (changes.Count == 0) ChangesLabel.Text = "No changes detected.";
            else ChangesLabel.Text = "Changed " + chunkChange + " chunks across " + changes.Count + " files.";

            OverviewTab.Text = "Resources" + ((chunkChange > 0) ? (" (" + chunkChange + ")") : "");

            UpdateSelectedRes();
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            RefreshResourceView();
            entityInspector1.RefreshView();
            Browser.ObjectsModified();
        }

        private void SaveAll_Click(object sender, EventArgs e)
        {
            var mod = Content.Content.Get().Changes;
            var changes = mod.GetChangeList();
            Action<IEnumerable<IffFile>> func = mod.SaveChanges;
            mod.Invoke(func, changes);
            RefreshResourceView();
        }

        private void DiscardAll_Click(object sender, EventArgs e)
        {
            var mod = Content.Content.Get().Changes;
            var changes = mod.GetChangeList();
            Action<IEnumerable<IffFile>> func = mod.DiscardChanges;
            mod.Invoke(func, changes);
            RefreshResourceView();
        }

        private HashSet<IffFile> SelectedIffs = new HashSet<IffFile>();
        private HashSet<IffChunk> SelectedChunks = new HashSet<IffChunk>();
        private List<ResChangeNode> ResNodes = new List<ResChangeNode>();

        private bool OwnSelMod;

        private void ChangesView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (OwnSelMod) return;
            var node = (ResChangeNode)e.Node;

            if (node.Resource is IffFile)
            {
                OwnSelMod = true;
                foreach (ResChangeNode n in node.Nodes) n.Checked = node.Checked;
                OwnSelMod = false;
            }
            else
            {
                var parent = node.Parent;
                bool check = true;
                foreach (ResChangeNode n in parent.Nodes)
                {
                    if (!n.Checked)
                    {
                        check = false;
                        break;
                    }
                }
                OwnSelMod = true;
                parent.Checked = check;
                OwnSelMod = false;
            }

            UpdateSelectedRes();
        }

        private void UpdateSelectedRes()
        {
            SelectedIffs.Clear();
            SelectedChunks.Clear();

            foreach (var node in ResNodes)
            {
                if (node.Checked)
                {
                    if (node.Resource is IffFile) SelectedIffs.Add((IffFile)node.Resource);
                    else if (node.Resource is IffChunk)
                    {
                        SelectedChunks.Add((IffChunk)node.Resource);
                        SelectedIffs.Add(((IffChunk)node.Resource).ChunkParent);
                    }
                }
            }

            IffSelection.Text = SelectedIffs.Count + " files selected.";
            ChunkSelection.Text = SelectedChunks.Count + " in selection.";

            IffSave.Enabled = SelectedIffs.Count > 0;
            IffDiscard.Enabled = SelectedIffs.Count > 0;

            ChunkDiscard.Enabled = SelectedChunks.Count > 0;
        }

        private void IffSave_Click(object sender, EventArgs e)
        {
            var changes = Content.Content.Get().Changes;
            Action<IEnumerable<IffFile>> func = changes.SaveChanges;
            changes.Invoke(func, new List<IffFile>(SelectedIffs));
            RefreshResourceView();
        }

        private void IffDiscard_Click(object sender, EventArgs e)
        {
            var changes = Content.Content.Get().Changes;
            Action<IEnumerable<IffFile>> func = changes.DiscardChanges;
            changes.Invoke(func, new List<IffFile>(SelectedIffs));
            RefreshResourceView();
        }

        private void ChunkDiscard_Click(object sender, EventArgs e)
        {
            var changes = Content.Content.Get().Changes;
            Action<IEnumerable<IffChunk>> func = changes.DiscardChanges;
            changes.Invoke(func, new List<IffChunk>(SelectedChunks));
            RefreshResourceView();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var about = new AboutWindow();
            about.ShowDialog();
        }

        private void NewOBJButton_Click(object sender, EventArgs e)
        {
            var iffDialog = new NewIffDialog();
            iffDialog.ShowDialog();
            if (iffDialog.DialogResult == DialogResult.OK)
            {
                var iff = iffDialog.InitIff;
                var objDialog = new NewObjectDialog(iff, true);
                objDialog.ShowDialog();
                if (objDialog.DialogResult == DialogResult.OK)
                {
                    Browser.ObjectsModified();
                    IffManager.OpenResourceWindow(Content.Content.Get().WorldObjects.Get(objDialog.ResultGUID));
                }
                else
                    MessageBox.Show("Object creation cancelled! Iff will not be created.");
            }
        }

        private void dataServiceEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ds = new TSODataDefinitionEditor();
            ds.Show();
        }

        private void saveGlobalscsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var trans = new AOTGenerator();
            trans.Show();

            /*
            var translator = new CSTranslator();
            var globalRes = Content.Content.Get().WorldObjectGlobals.Get("global").Resource;

            var iff = globalRes.MainIff;
            iff.Filename = "global.iff";
            translator.Context.GlobalRes = globalRes;
            var globalText = translator.TranslateIff(iff);
            using (var file = System.IO.File.Open(@"C:\Users\Rhys\Desktop\AOT\global.cs", System.IO.FileMode.Create))
            {
                using (var writer = new System.IO.StreamWriter(file))
                {
                    writer.Write(globalText);
                }
            }

            var globalContext = (CSTranslationContext)translator.Context;

            var compiledSG = new Dictionary<GameGlobalResource, CSTranslationContext>();
            var objs = Content.Content.Get().WorldObjects.Entries.ToList();
            var fileComplete = new HashSet<string>();
            foreach (var obj in objs)
            {
                var r = obj.Value;
                if (!fileComplete.Contains(r.FileName))
                {
                    fileComplete.Add(r.FileName);
                    var objRes = r.Get();

                    CSTranslationContext sg = null;
                    if (objRes.Resource.SemiGlobal != null)
                    {
                        if (!compiledSG.TryGetValue(objRes.Resource.SemiGlobal, out sg))
                        {
                            //compile semiglobals
                            translator = new CSTranslator();
                            var sgIff = objRes.Resource.SemiGlobal.MainIff;
                            translator.Context.ObjectRes = objRes.Resource; //pass this in as occasionally *local* tuning constants are used in *semiglobal* functions.
                            translator.Context.GlobalRes = globalRes;
                            translator.Context.SemiGlobalRes = objRes.Resource.SemiGlobal;
                            translator.Context.GlobalContext = globalContext;
                            var semiglobalText = translator.TranslateIff(sgIff);
                            using (var file = System.IO.File.Open(@"C:\Users\Rhys\Desktop\AOT\" + sgIff.Filename.ToString().Replace(".iff", ".cs"), System.IO.FileMode.Create))
                            {
                                using (var writer = new System.IO.StreamWriter(file))
                                {
                                    writer.Write(semiglobalText);
                                }
                            }
                            sg = (CSTranslationContext)translator.Context;
                            compiledSG[objRes.Resource.SemiGlobal] = sg;
                        }
                    }

                    translator = new CSTranslator();
                    var objIff = objRes.Resource.MainIff;
                    translator.Context.GlobalRes = globalRes;
                    translator.Context.SemiGlobalRes = objRes.Resource.SemiGlobal;
                    translator.Context.ObjectRes = objRes.Resource;
                    translator.Context.GlobalContext = globalContext;
                    translator.Context.SemiGlobalContext = sg;
                    var objText = translator.TranslateIff(objIff);
                    using (var file = System.IO.File.Open(@"C:\Users\Rhys\Desktop\AOT\" + objIff.Filename.ToString().Replace(".iff", ".cs"), System.IO.FileMode.Create))
                    {
                        using (var writer = new System.IO.StreamWriter(file))
                        {
                            writer.Write(objText);
                        }
                    }
                }
            }
            */
        }

        private void avatarToolToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var avatarTool = new AvatarTool();
            avatarTool.Show();
        }
    }

    public class ResChangeNode : TreeNode
    {
        public object Resource;
        public ResChangeNode(object res)
        {
            Resource = res;
            Text = ToString();
        }

        public override string ToString()
        {
            if (Resource is IffChunk) {
                var chunk = (IffChunk)Resource;
                return "(" + chunk.ChunkType + " #" + chunk.ChunkID + ") " + chunk.ChunkLabel;
            }
            if (Resource is IffFile) return ((IffFile)Resource).Filename;
            return base.ToString();
        }
    }
}
