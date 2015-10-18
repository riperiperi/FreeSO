using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSO.Content.Model;

namespace FSO.Debug.Content.Preview
{
    public partial class TexturePreview : UserControl, IContentPreview
    {
        public TexturePreview()
        {
            InitializeComponent();
        }

        public bool CanPreview(object value)
        {
            return value is ITextureRef;
        }

        public void Preview(object value)
        {
            ITextureRef texture = (ITextureRef)value;
            this.pictureBox1.Image = texture.GetImage();
        }
    }
}
