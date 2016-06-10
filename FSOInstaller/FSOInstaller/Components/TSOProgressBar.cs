using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSOInstaller.Components
{
    public class TSOProgressBar : Control
    {
        private Bitmap bgimage;
        private Bitmap pgLeft;
        private Bitmap pgRight;
        private Bitmap pgSeg;

        private int _Percent= 50;

        public TSOProgressBar()
        {
            BackColor = Color.FromArgb(80, 119, 163);
            //BackColor = Color.Transparent;
        }
        private void Init()
        {
            var value = "Packed/Setup/Resources/Plugin/Res/8345a779_Graphics_SetupUI/";
            if (DesignMode) value = "C:/Users/Rhys/Documents/GitHub/FreeSO/FSOInstaller/FSOInstaller/" + value;
            try
            {
                bgimage = Paloma.TargaImage.LoadTargaImage(value + "10000001_ProgressGaugeBackground.tga");
                bgimage.MakeTransparent(Color.Magenta);
                pgLeft = Paloma.TargaImage.LoadTargaImage(value + "10000002_ProgressGaugeLeft.tga");
                pgLeft.MakeTransparent(Color.Magenta);
                pgRight = Paloma.TargaImage.LoadTargaImage(value + "10000003_ProgressGaugeRight.tga");
                pgRight.MakeTransparent(Color.Magenta);
                pgSeg = Paloma.TargaImage.LoadTargaImage(value + "10000004_ProgressGaugeSegment.tga");
            }
            catch (Exception)
            {

            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (bgimage == null) Init();
            if (Height != bgimage.Height) Height = bgimage.Height;

            e.Graphics.DrawImage(bgimage, new Rectangle(0, 0, bgimage.Height, bgimage.Height), new Rectangle(0, 0, bgimage.Height, bgimage.Height), GraphicsUnit.Pixel);
            e.Graphics.DrawImage(bgimage, new Rectangle(bgimage.Height, 0, Width-bgimage.Height*2, bgimage.Height), new Rectangle(bgimage.Height, 0, bgimage.Width-bgimage.Height*2, bgimage.Height), GraphicsUnit.Pixel);
            e.Graphics.DrawImage(bgimage, new Rectangle(Width-bgimage.Height, 0, bgimage.Height, bgimage.Height), new Rectangle(bgimage.Width-bgimage.Height, 0, bgimage.Height, bgimage.Height), GraphicsUnit.Pixel);

            if (_Percent > 0)
            {
                var innerWidth = (_Percent * (Width - (6 + pgLeft.Width + pgRight.Width))) / 100;
                e.Graphics.DrawImage(pgLeft, new Point(3, 3));
                e.Graphics.DrawImage(pgSeg, new Rectangle(3 + pgLeft.Width, 3, innerWidth, pgSeg.Height), new Rectangle(0,0,1,pgSeg.Height), GraphicsUnit.Pixel);
                e.Graphics.DrawImage(pgRight, new Point(3 + pgLeft.Width + innerWidth, 3));
            }
            
        }
    }


}
