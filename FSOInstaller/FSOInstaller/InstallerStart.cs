using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSOInstaller
{
    public partial class InstallerStart : Form
    {
        public static string[] LanguageSetNames =
{
            "English (US)",
            "English (UK)",
            "French",
            "German",
            "Italian",
            "Spanish",
            "Dutch",
            "Danish",
            "Swedish",
            "Norwegian",
            "Finish",
            "Hebrew",
            "Russian",
            "Portuguese",
            "Japanese",
            "Polish",
            "Simplified Chinese",
            "Traditional Chinese",
            "Thai",
            "Korean"
        };

        public InstallerStart()
        {
            InitializeComponent();
            LanguageCombo.Items.AddRange(LanguageSetNames);
            LanguageCombo.SelectedIndex = 0;
        }

        private void InstallButton_Click(object sender, EventArgs e)
        {
            var installer = new InstallerComplete();
            this.Hide();
            installer.Closed += (s, args) => this.Close();
            installer.Show();
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
        }
    }
}
