using FSO.Files.Formats.IFF;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace FSO.IDE.ResourceBrowser
{
    public partial class ExternalIffSelectorDialog : Form
    {
        public IffFile Iff { get; private set; }
        public IffFile Spf { get; private set; }
        /// <summary>
        /// 1 = Open OBJD Editor 2 = Open Iff Editor
        /// </summary>
        public int Selection { get; private set; } = -1;

        public ExternalIffSelectorDialog()
        {
            InitializeComponent();
        }

        internal static IffFile OpenExternalIff(out string FilePath, bool SPFFile = false)
        {
            FilePath = null;
            bool SaveFile(FileDialog Dialog)
            {
                // ༼ つ ◕_◕ ༽つ IMPEACH STAThread ༼ つ ◕_◕ ༽つ
                var wait = new AutoResetEvent(false);
                DialogResult result = DialogResult.None;
                var thread = new Thread(() => {
                    result = Dialog.ShowDialog();
                    wait.Set();
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                wait.WaitOne();
                return result == DialogResult.OK;
            }

            var dialog = new OpenFileDialog();
            string noun = SPFFile ? "SPF" : "IFF";
            dialog.Title = $"Select an {noun} file. (*.{noun})";
            if (!SaveFile(dialog)) return null;
            try
            {
                var iff = new IffFile(dialog.FileName);
                iff.TSBO = true;
                FilePath = dialog.FileName;
                return iff;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return null;
        }

        private void IffLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Iff = OpenExternalIff(out var fileName);
            if (Iff == null) return; // User cancelled
            IffLinkLabel.Text = fileName;
            string spfFileName = fileName.Remove(fileName.Length - 3) + "spf";
            if (!File.Exists(spfFileName)) return;
            if (MessageBox.Show($"Select {Path.GetFileName(spfFileName)}?", "Autoselect", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            SelectSpfFile(ref spfFileName);
        }

        void SelectSpfFile(ref string SpfFileName)
        {
            IffFile spf = default;
            if (string.IsNullOrWhiteSpace(SpfFileName) || !File.Exists(SpfFileName))            
                spf = OpenExternalIff(out SpfFileName, true);
            else
            {
                try
                {
                    spf = new IffFile(SpfFileName);
                    spf.TSBO = true;
                }
                catch (Exception e) { MessageBox.Show(e.Message); }
            }
            if (spf == null) return;
            SPFLinkLabel.Text = SpfFileName;
            Spf = spf;
        }

        private void SPFLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string filename = "";
            SelectSpfFile(ref filename);
        }

        private void IffEditorButton_Click(object sender, EventArgs e)
        {
            if (Iff == null && Spf == null) return;
            Selection = 2;
            Close();
        }

        private void OBJDEditorButton_Click(object sender, EventArgs e)
        {
            if (Iff == null && Spf == null) return;
            Selection = 1;
            Close();
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            Selection = -1;
            Close();
        }
    }
}
