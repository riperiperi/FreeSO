using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent;
using FSO.IDE.EditorComponent.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.ResourceBrowser
{
    public partial class SelectTreeDialog : Form
    {
        GameIffResource Resource;
        public List<InstructionIDNamePair> CurrentFullList;
        public ushort ResultID = 0;

        public SelectTreeDialog(GameIffResource res)
        {
            InitializeComponent();
            Resource = res;

            CurrentFullList = new List<InstructionIDNamePair>();
            CurrentFullList.AddRange(GetAllSubroutines(ScopeSource.Private));
            CurrentFullList.AddRange(GetAllSubroutines(ScopeSource.SemiGlobal));
            CurrentFullList.AddRange(GetAllSubroutines(ScopeSource.Global));

            RenderList();
        }

        public List<T> GetAllResource<T>(ScopeSource source)
        {
            switch (source)
            {
                case ScopeSource.Private:
                    return (Resource is GameGlobalResource)? new List<T>():Resource.List<T>();
                case ScopeSource.SemiGlobal:
                    return (Resource.SemiGlobal == null) ? new List<T>():Resource.SemiGlobal.List<T>();
                case ScopeSource.Global:
                    return EditorScope.Globals.Resource.List<T>();
                default:
                    return new List<T>();
            }
        }

        public List<InstructionIDNamePair> GetAllSubroutines(ScopeSource source)
        {
            var bhavs = GetAllResource<BHAV>(source);
            if (source == ScopeSource.SemiGlobal && Resource is GameGlobalResource) bhavs = Resource.List<BHAV>();
            var output = new List<InstructionIDNamePair>();
            if (bhavs == null) return output;
            foreach (var bhav in bhavs)
            {
                output.Add(new InstructionIDNamePair(bhav.ChunkLabel, bhav.ChunkID));
            }

            output = output.OrderBy(o => o.Name).ToList();
            return output;
        }

        private void RenderList()
        {
            var searchString = new Regex(".*" + SearchBox.Text.ToLowerInvariant() + ".*");
            PrimitiveList.ClearSelected();
            PrimitiveList.Items.Clear();

            foreach (var prim in CurrentFullList)
            {
                if (searchString.IsMatch(prim.ToString().ToLowerInvariant())) PrimitiveList.Items.Add(prim);
            }
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            RenderList();
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void PrimitiveList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResultID = (PrimitiveList.SelectedItem == null)?(ushort)0:((InstructionIDNamePair)PrimitiveList.SelectedItem).ID;
            SelectButton.Text = "Select " + ((PrimitiveList.SelectedItem == null) ? 
                "None" : ((InstructionIDNamePair)PrimitiveList.SelectedItem).Name);
        }
    }
}
