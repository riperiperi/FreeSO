using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TSOClient.Code.Debug
{
    public partial class TSOClientTools : Form
    {
        private TSOClientUIInspector uiInspetor;
        private TSOSceneInspector sceneInspector;

        public TSOClientTools()
        {
            InitializeComponent();

            /**
             * UI Inspector
             */
            uiInspetor = new TSOClientUIInspector();
            uiInspetor.Show();

            sceneInspector = new TSOSceneInspector();
            sceneInspector.Show();


        }

        public void PositionAroundGame(Form gameWindow)
        {
            this.Location = new Point(gameWindow.Location.X - this.Width - 10, gameWindow.Location.Y);
            uiInspetor.Location = new Point(
                gameWindow.Location.X - uiInspetor.Width - 10,
                gameWindow.Location.Y + this.Height + 10
            );
            sceneInspector.Location = new Point(
                gameWindow.Location.X + gameWindow.Width + 10,
                gameWindow.Location.Y
            );
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            var window = new TSOClientFindAssetSearch();
            window.StartSearch(txtFindAsset.Text);
            window.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //var window = new TSOEdith();
            //window.Show();
        }
    }
}
