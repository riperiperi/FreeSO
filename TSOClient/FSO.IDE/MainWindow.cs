using FSO.IDE.Common;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.Model.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE
{
    public partial class MainWindow : Form
    {
        public VM HookedVM;

        public MainWindow()
        {
            InitializeComponent();
        }
        public void Test(VM vm)
        {
            ObjectRegistry.Init();
            this.HookedVM = vm;
            Browser.RefreshTree();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (Browser.SelectedFile == null) return;
            uint GUID;
            if (Browser.SelectedObj == null)
                GUID = ObjectRegistry.MastersByFilename[Browser.SelectedFile][0].GUID;
            else
                GUID = Browser.SelectedObj.GUID;

            var objWindow = new ObjectWindow(ObjectRegistry.MastersByFilename[Browser.SelectedFile], GUID);
            objWindow.Show();
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            if (Browser.SelectedFile == null) return;
            uint GUID;
            if (Browser.SelectedObj == null)
                GUID = ObjectRegistry.MastersByFilename[Browser.SelectedFile][0].GUID;
            else
                GUID = Browser.SelectedObj.GUID;

            HookedVM.SendCommand(new VMNetBuyObjectCmd
            {
                GUID = GUID,
                dir = LotView.Model.Direction.NORTH,
                level = HookedVM.Context.World.State.Level,
                x = (short)(((short)Math.Floor(HookedVM.Context.World.State.CenterTile.X) << 4) + 8),
                y = (short)(((short)Math.Floor(HookedVM.Context.World.State.CenterTile.Y) << 4) + 8)
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
