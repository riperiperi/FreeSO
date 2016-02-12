using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Formats.IFF;
using FSO.Content;

namespace FSO.IDE.ResourceBrowser
{
    public partial class OBJDSelectorControl : UserControl
    {
        public OBJDSelectorControl()
        {
            InitializeComponent();
        }

        private OBJDSelector[] Selectors;
        private OBJD Definition;
        private IffChunk Active;
        private bool OwnChange;

        public void SetSelectors(OBJD objd, IffChunk active, OBJDSelector[] selectors)
        {
            Active = active;
            Definition = objd;
            SelectButton.Dock = DockStyle.Fill;
            SelectCombo.Dock = DockStyle.Fill;
            SelectButton.Visible = selectors.Length == 1;
            SelectCombo.Visible = selectors.Length > 1;
            Selectors = selectors;
            if (selectors.Length > 1)
            {
                SelectCombo.Items.Clear();
                SelectCombo.Items.Add("-- Not Selected --");
                int i = 1;
                OwnChange = true;
                SelectCombo.SelectedIndex = 0;
                foreach (var sel in selectors)
                {
                    SelectCombo.Items.Add(sel);
                    if (sel.FieldName != null && objd.GetPropertyByName<ushort>(sel.FieldName) == active.ChunkID)
                        SelectCombo.SelectedIndex = i;
                    i++;
                }
                OwnChange = false;
            } else
            {
                var sel = selectors[0];
                if (sel.Callback != null || (sel.FieldName != null && objd.GetPropertyByName<ushort>(sel.FieldName) == active.ChunkID))
                {
                    SelectButton.Text = "Selected as " + sel.Name;
                    SelectButton.Enabled = false;
                } else
                {
                    SelectButton.Text = "Select as " + sel.Name;
                    SelectButton.Enabled = true;
                }
            }
        }

        private void SelectCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelectCombo.SelectedIndex == -1 || OwnChange) return;

            var id = Active.ChunkID;
            var def = Definition;

            if (SelectCombo.SelectedIndex == 0)
            {
                foreach (var sel2 in Selectors)
                {
                    if (def.GetPropertyByName<ushort>(sel2.FieldName) == id)
                    {
                        var prop = sel2.FieldName;

                        Content.Content.Get().BlockingResMod(new ResAction(() =>
                        {
                            def.SetPropertyByName(prop, 0);
                        }, def));
                    }
                }
            }
            else
            {
                var sel = Selectors[SelectCombo.SelectedIndex - 1];
                if (sel.FieldName != null)
                {
                    var prop = sel.FieldName;

                    Content.Content.Get().BlockingResMod(new ResAction(() =>
                    {
                        def.SetPropertyByName(prop, id);
                    }, def));
                }
            }
        }

        private void SelectButton_Click(object sender, EventArgs e)
        {
            var sel = Selectors[0];

            if (sel.Callback != null) sel.Callback(Active);
            else if (sel.FieldName != null)
            {
                var prop = sel.FieldName;
                var id = Active.ChunkID;
                var def = Definition;
                Content.Content.Get().BlockingResMod(new ResAction(() =>
                {
                    def.SetPropertyByName(prop, id);
                }, def));
            }
        }
    }

    public class OBJDSelector
    {
        public delegate void OBJDSelectorCallback(IffChunk chunk);

        public string Name;
        public string FieldName;
        public OBJDSelectorCallback Callback;

        public OBJDSelector(string name, string field) : this(name, field, null) { }
        public OBJDSelector(string name, string field, OBJDSelectorCallback callback)
        {
            Name = name;
            FieldName = field;
            Callback = callback;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
