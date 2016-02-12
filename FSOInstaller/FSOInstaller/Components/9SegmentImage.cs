using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Paloma;
using System.IO;

namespace FSOInstaller.Components
{
    public partial class _9SegmentImage : Control
    {
        public Bitmap image;


        private int _Margin;
        [Description("Margin to use from the edges."), Category("Appearance")]
        public int SegMargin
        {
            get { return _Margin; }
            set { _Margin = value; }
        }

        private string _ImagePath;
        [Description("TGA displayed by this 9seg"), Category("Appearance")]
        public string ImagePath {
            get { return _ImagePath; }
            set {
                _ImagePath = value;
                if (DesignMode) value = "C:/Users/Rhys/Documents/GitHub/FreeSO/FSOInstaller/FSOInstaller/" + value;
                try
                {
                    image = Paloma.TargaImage.LoadTargaImage(value);
                } catch (Exception)
                {

                }
            }
        }

        public _9SegmentImage()
        {
            InitializeComponent();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = (-1);

            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (image != null)
            {
                var cornerSize = new Size(_Margin, _Margin);
                var middleSize = new Size(image.Width - 2 * _Margin, image.Height - 2 * _Margin);
                //tl corner
                e.Graphics.DrawImage(image, new Rectangle(new Point(), cornerSize), new Rectangle(new Point(),cornerSize), GraphicsUnit.Pixel);
                //tr corner
                e.Graphics.DrawImage(image, new Rectangle(new Point(Width-_Margin, 0), cornerSize), new Rectangle(new Point(image.Width - _Margin, 0), cornerSize), GraphicsUnit.Pixel);
                //br corner
                e.Graphics.DrawImage(image, new Rectangle(new Point(Width - _Margin, Height - _Margin), cornerSize), new Rectangle(new Point(image.Width - _Margin, image.Height - _Margin), cornerSize), GraphicsUnit.Pixel);
                //bl corner
                e.Graphics.DrawImage(image, new Rectangle(new Point(0, Height - _Margin), cornerSize), new Rectangle(new Point(0, image.Height - _Margin), cornerSize), GraphicsUnit.Pixel);

                //top
                e.Graphics.DrawImage(image, new Rectangle(new Point(_Margin, 0), new Size(Width-2*_Margin, _Margin)), new Rectangle(new Point(_Margin, 0), new Size(middleSize.Width, _Margin)), GraphicsUnit.Pixel);
                //bottom
                e.Graphics.DrawImage(image, new Rectangle(new Point(_Margin, Height-_Margin), new Size(Width - 2 * _Margin, _Margin)), new Rectangle(new Point(_Margin, image.Height-_Margin), new Size(middleSize.Width, _Margin)), GraphicsUnit.Pixel);
                //left
                e.Graphics.DrawImage(image, new Rectangle(new Point(0, _Margin), new Size(_Margin, Height - 2 * _Margin)), new Rectangle(new Point(0, _Margin), new Size(_Margin, middleSize.Height)), GraphicsUnit.Pixel);
                //right
                e.Graphics.DrawImage(image, new Rectangle(new Point(Width-_Margin, _Margin), new Size(_Margin, Height - 2 * _Margin)), new Rectangle(new Point(image.Width-_Margin, _Margin), new Size(_Margin, middleSize.Height)), GraphicsUnit.Pixel);

                //middle
                e.Graphics.DrawImage(image, new Rectangle(new Point(_Margin, _Margin), new Size(Width - 2*_Margin, Height - 2 * _Margin)), new Rectangle(new Point(_Margin, _Margin), middleSize), GraphicsUnit.Pixel);

            }
        }
    }
}
