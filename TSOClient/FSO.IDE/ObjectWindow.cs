using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.Common;
using FSO.IDE.EditorComponent;
using FSO.IDE.ResourceBrowser;
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
        public ObjectRegistryEntry ActiveObjTable;
        public GameObject ActiveObj;

        public ObjectWindow()
        {
            InitializeComponent();
        }

        public ObjectWindow(List<ObjectRegistryEntry> df, uint GUID) : this()
        {
            if (df.Count == 0)
            {
                MessageBox.Show("Not a valid object!");
                this.Close();
                return;
            }

            //populate object selected box with options
            ObjCombo.Items.Clear();
            int i = 0;
            foreach (var master in df)
            {
                ObjCombo.Items.Add(master);
                if (master.GUID == GUID) ObjCombo.SelectedIndex = i;
                i++;
                foreach (var child in master.Children)
                {
                    ObjCombo.Items.Add(child);
                    if (child.GUID == GUID) ObjCombo.SelectedIndex = i;
                    i++;
                }
            }
            if (ObjCombo.SelectedIndex == -1) ObjCombo.SelectedIndex = 0;

            Text = "Edit Object - " + ActiveObjTable.Filename;


        }

        public void ChangeActiveObject(ObjectRegistryEntry obj)
        {
            ActiveObjTable = obj;
            ActiveObj = Content.Content.Get().WorldObjects.Get(obj.GUID);

            if (IffResView.ActiveIff == null) IffResView.ChangeIffSource(ActiveObj.Resource);
            IffResView.ChangeActiveObject(ActiveObj);

            //update top var

            ObjNameLabel.Text = obj.Name;
            ObjDescLabel.Text = "§----";
            if (obj.Group == 0)
            {
                ObjMultitileLabel.Text = "Single-tile object.";
            }
            else if (obj.SubIndex < 0)
            {
                ObjMultitileLabel.Text = "Multitile master object.";
            }
            else
            {
                ObjMultitileLabel.Text = "Multitile part. (" + (obj.SubIndex >> 8) + ", " + (obj.SubIndex & 0xFF) + ")";
            }
        }

        private void ObjCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            ChangeActiveObject((ObjectRegistryEntry)ObjCombo.SelectedItem);
        }
    }
}
