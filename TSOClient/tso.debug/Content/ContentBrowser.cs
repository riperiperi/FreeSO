using FSO.Content.Framework;
using FSO.Content.Model;
using FSO.Debug.Content.Preview;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.Debug.Content
{
    public partial class ContentBrowser : Form
    {
        private TexturePreview TexturePreview;
        private IContentPreview[] Previews;

        public ContentBrowser()
        {
            InitializeComponent();
            TexturePreview = new TexturePreview();
            Previews = new IContentPreview[]
            {
                TexturePreview
            };

            RefreshItems();
        }

        private void RefreshItems(){
            var content = FSO.Content.Content.Get();

            List<ContentReference> items = new List<ContentReference>();

            if(menuGraphics.Checked)
            {
                items.AddRange(content.UIGraphics.List().Select(x =>
                {
                    Far3ProviderEntry<ITextureRef> actual = (Far3ProviderEntry<ITextureRef>)x;
                    return new ContentReference {
                        Value = actual.Get(),
                        ItemID = actual.ID,
                        ItemName = actual.FarEntry.Filename
                    };
                }));
            }

            if(searchQuery.Text.Length > 0){
                items = items.Where(x => x.ItemName.IndexOf(searchQuery.Text) != -1).ToList();
            }

            this.items.DataSource = items;
        }

        private void items_SelectionChanged(object sender, EventArgs e)
        {
            splitContainer1.Panel2.Controls.Clear();

            if (this.items.SelectedRows.Count != 1) { return; }
            var item = this.items.SelectedRows[0].DataBoundItem as ContentReference;
            if (item == null) { return; }

            foreach(var preview in Previews)
            {
                if (preview.CanPreview(item.Value))
                {
                    preview.Preview(item.Value);
                    splitContainer1.Panel2.Controls.Add((Control)preview);
                    break;
                }
            }
        }

        private void searchBtn_Click_1(object sender, EventArgs e)
        {
            RefreshItems();
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void searchQuery_TextChanged(object sender, EventArgs e)
        {
            RefreshItems();
        }
    }


    class ContentReference
    {
        public ulong ItemID { get; set; }
        public string ItemName { get; set; }
        public object Value;
    }
}
