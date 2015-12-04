using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.Common;
using FSO.IDE.EditorComponent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE
{
    public partial class ObjectWindow : Form
    {
        public GameObject ActiveObj;

        public ObjectWindow()
        {
            InitializeComponent();
        }

        public ObjectWindow(List<ObjectRegistryEntry> df, uint GUID) : this()
        {
            ChangeActiveObject(Content.Content.Get().WorldObjects.Get(GUID));
        }

        public void ChangeActiveObject(GameObject obj)
        {
            ActiveObj = obj;

            var bhavs = obj.Resource.List<BHAV>();
            TreeList.Items.Clear();
            TreeList.Items.AddRange(GetResList(bhavs.ToArray()).ToArray());
        }

        public List<ObjectResourceEntry> GetResList(IffChunk[] list)
        {
            var result = new List<ObjectResourceEntry>();
            foreach (var item in list)
            {
                var chunk = (IffChunk)item;
                result.Add(new ObjectResourceEntry(chunk.ChunkLabel.TrimEnd('\0'), chunk.ChunkID));
            }
            return result;
        }

        private void TreeList_DoubleClick(object sender, EventArgs e)
        {
            if (TreeList.SelectedItem != null)
            {
                var item = (ObjectResourceEntry)TreeList.SelectedItem;
                new Thread(() =>
                {
                    var bhav = ActiveObj.Resource.Get<BHAV>(item.ID);
                    var editor = new BHAVEditor(bhav, new EditorScope(ActiveObj, bhav), null);
                    Application.Run(editor);
                }).Start();
            }
        }
    }

    public class ObjectResourceEntry
    {
        public string Name;
        public ushort ID;

        public ObjectResourceEntry(string name, ushort id)
        {
            Name = name;
            ID = id;
        }

        public override string ToString()
        {
            return Name + " ("+ID+")";
        }
    }
}
