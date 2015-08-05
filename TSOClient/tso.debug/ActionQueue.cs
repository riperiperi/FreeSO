using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using FSO.SimAntics;

namespace FSO.Debug
{
    public partial class ActionQueue : Form
    {
        private ImageList imgList;
        public VMEntity target;

        public ActionQueue()
        {
            InitializeComponent();
            imgList = new ImageList();
            imgList.ImageSize = new Size(45, 45);
            imgList.ColorDepth = ColorDepth.Depth24Bit;
            imgList.Images.Add(FSO.Debug.Properties.Resources.iconsel);
            imgList.Images.Add(FSO.Debug.Properties.Resources.icondsel);

            actionView.LargeImageList = imgList;

            ListView_SetSpacing(actionView, 53, 49);
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public int MakeLong(short lowPart, short highPart)
        {
            return (int)(((ushort)lowPart) | (uint)(highPart << 16));
        }

        public void ListView_SetSpacing(ListView listview, short cx, short cy)
        {
            const int LVM_FIRST = 0x1000;
            const int LVM_SETICONSPACING = LVM_FIRST + 53;

            SendMessage(listview.Handle, LVM_SETICONSPACING,
            IntPtr.Zero, (IntPtr)MakeLong(cx, cy));
        }

        public void DrawActionsOf(VMEntity obj)
        {
            objNameLabel.Text = "Active Object: " + obj.ToString();

            actionView.Items.Clear();
            for (int i = 0; i < obj.Thread.Queue.Count; i++)
            {
                actionView.Items.Add(new ListViewItem(obj.Thread.Queue[i].Name, (i == 0) ? 0 : 1));
            }
        }

        private void interactionUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (target != null)
            {
                lock (target)
                {
                    DrawActionsOf(target);
                }
            }
        }
    }
}
