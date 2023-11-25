using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Windows;

namespace FSO.IDE.ResourceBrowser
{
    public partial class XMLEntryEditor : UserControl
    {
        public GameObject ActiveObj;
        private Dictionary<string, int> CategoryIds = new Dictionary<string, int>()
        {
            // Buy Mode
            { "Seating", 12 },
            { "Surfaces", 13 },
            { "Appliances", 14 },
            { "Entertainment", 15 },
            { "Skill and Job Objects", 16 },
            { "Decorative", 17 },
            { "Miscellaneous", 18 },
            { "Lighting", 19 },
            { "Pets and Pet Objects", 20 },

            // Build Mode
            { "Door Tool", 0 },
            { "Window Tool", 1 },
            { "Stair Tool", 2 },
            { "Plant Tool", 3 },
            { "Fireplace Tool", 4 },
            { "Water Tool", 5 },
            { "Wall & Fence Tool", 7 },
            { "Debug Menu", 29 },
        };

        public XMLEntryEditor()
        {
            InitializeComponent();
        }

        public void SetActiveObject(GameObject obj)
        {
            ActiveObj = obj;

            // Workaround to guarantee a monospaced font
            XMLEntryTextBox.Font = new Font(FontFamily.GenericMonospace, XMLEntryTextBox.Font.Size);

            IFFFilenameTextBox.Text = ActiveObj.Resource.Iff.Filename;
            CommentCheckbox.Checked = false;

            GUIDTextBox.Text = ActiveObj.OBJ.GUID.ToString("X8");

            CategoryComboBox.DisplayMember = "Key";
            CategoryComboBox.ValueMember = "Value";
            CategoryComboBox.DataSource = new BindingSource(CategoryIds, null);
            CategoryComboBox.SelectedValue = 29;

            SalePriceUpDown.Value = ActiveObj.OBJ.SalePrice;
            CTSS ctss = ActiveObj.Resource.Get<CTSS>(ActiveObj.OBJ.CatalogStringsID);
            NameTextBox.Text = ctss == null ? ActiveObj.OBJ.ChunkLabel : ctss.GetString(0);

            CopiedLabel.Text = "";

            XMLEntryTextBox.Text = GenerateXML();
        }

        private string GenerateXML()
        {
            XDocument fullEntry = new XDocument();

            if (CommentCheckbox.Checked && IFFFilenameTextBox.Text.Length > 0)
            {
                XComment comment = new XComment($" {IFFFilenameTextBox.Text} ");
                fullEntry.Add(comment);
            }

            XElement entry = new XElement("P");
            entry.SetAttributeValue("g", GUIDTextBox.Text);
            entry.SetAttributeValue("s", CategoryComboBox.SelectedValue);
            entry.SetAttributeValue("p", SalePriceUpDown.Value);
            entry.SetAttributeValue("n", NameTextBox.Text);

            fullEntry.Add(entry);

            return fullEntry.ToString();
        }

        private void UpdateXMLTextBox(object sender, EventArgs e)
        {
            XMLEntryTextBox.Text = GenerateXML();
            CopiedLabel.Text = "";
        }

        private void CopyXML()
        {
            WinFormsClipboard clipboard = new WinFormsClipboard();
            clipboard.Set(XMLEntryTextBox.Text);
            CopiedLabel.Text = "XML Copied!";
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            DialogResult result = new DialogResult();
            if (((int) CategoryComboBox.SelectedValue != CategoryIds["Debug Menu"]) && SalePriceUpDown.Value == 0)
            {
                string message = $"This object is free and being placed in {((KeyValuePair<string, int>) CategoryComboBox.SelectedItem).Key}. Are you sure you want to copy this entry?";
                string caption = "Free Object Outside of Debug Menu";
                result = MessageBox.Show(message, caption, MessageBoxButtons.YesNo);
            }

            if (result == System.Windows.Forms.DialogResult.None || result == System.Windows.Forms.DialogResult.Yes)
            {
                CopyXML();
            }
        }

        private void CommentCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (CommentCheckbox.Checked)
            {
                IFFFilenameTextBox.Enabled = true;
            } else
            {
                IFFFilenameTextBox.Enabled = false;
            }

            UpdateXMLTextBox(sender, e);
        }
    }
}
