using FSO.IDE.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.AvatarUtils
{
    public partial class AddAppearanceWindow : Form
    {

        private string OutfitErrorMessage = "Outfits require materials for each skin color on at least one of the meshes, ending in '-<skin>', eg. lgt/med/drk.";
        private string HandgroupErrorMessage = "Handgroups require 6 meshes ending in '-<side>-<type>', eg left/right and idle/fist/point, as well as 3 materials for each hand style per skin color (9).";
        private string HandgroupUnsupported = "Importing Handgroups is not yet supported.";

        private bool InternalChange;
        private List<ImportMeshGroup> Meshes;
        private List<ImportMaterialGroup> Materials;
        public AddAppearanceImportMode Mode;

        public AddAppearanceWindow(List<ImportMeshGroup> meshes, List<ImportMeshGroup> materials)
        {
            InitializeComponent();

            Meshes = meshes;
            NameEntry.Text = GuessName();
            EvaluateImportPossibilities();
            SetMode(AddAppearanceImportMode.Appearance);
        }

        private string GuessName()
        {
            if (Meshes.Count == 0) return "empty";
            var strings = Meshes.Select(x => x.Name).ToArray();
            var maxLength = strings.Max(x => x.Length);
            var baseName = strings[0];

            int i = 0;
            bool end = false;
            for (i = 0; i < maxLength; i++)
            {
                foreach (var str in strings) {
                    if (baseName[i] != str[i])
                    {
                        end = true;
                        break;
                    }
                }
                if (end) break;
            }

            var guess = baseName.Substring(0, i).TrimEnd('-', '_', ' ');
            if (guess.Length == 0) return "mesh";
            return guess;
        }

        private void EvaluateImportPossibilities()
        {
            AppearanceRadio.Enabled = true;

            //outfit export is enabled if we have at least one mesh ending in '-<skin>'
            /*
            var skinColoredMeshes = Meshes.Where(mesh =>
            {
                
            });
            */

            OutfitRadio.Enabled = false;
            HeadRadio.Enabled = false;
            HandgroupRadio.Enabled = false;
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            var importer = new AppearanceGenerator();
            switch (Mode)
            {
                case AddAppearanceImportMode.Appearance:
                    importer.GenerateAppearanceTSO(Meshes, NameEntry.Text, false);
                    break;
            }
            Close();
        }

        private void UpdateSummary()
        {
            SummaryText.Text = NameEntry.Text + ".apr\r\n" + String.Join("\r\n", Meshes.Select(x => "  " + x.Name));
        }

        private void SetMode(AddAppearanceImportMode mode)
        {
            InternalChange = true;
            Mode = mode;
            AppearanceRadio.Checked = mode == AddAppearanceImportMode.Appearance;
            OutfitRadio.Checked = mode == AddAppearanceImportMode.Outfit;
            HeadRadio.Checked = mode == AddAppearanceImportMode.Head;
            HandgroupRadio.Checked = mode == AddAppearanceImportMode.Handgroup;

            HandgroupLabel.Visible = mode == AddAppearanceImportMode.Outfit;
            HandgroupCombo.Visible = mode == AddAppearanceImportMode.Outfit;

            UpdateSummary();

            InternalChange = false;
        }

        private void AppearanceRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (!InternalChange) SetMode(AddAppearanceImportMode.Appearance);
        }

        private void OutfitRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (!InternalChange) SetMode(AddAppearanceImportMode.Outfit);
        }

        private void HandgroupRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (!InternalChange) SetMode(AddAppearanceImportMode.Handgroup);
        }

        private void HeadRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (!InternalChange) SetMode(AddAppearanceImportMode.Head);
        }

        private void NameEntry_TextChanged(object sender, EventArgs e)
        {
            UpdateSummary();
        }
    }

    public enum AddAppearanceImportMode
    {
        Appearance,
        Outfit,
        Head,
        Handgroup
    }
}
