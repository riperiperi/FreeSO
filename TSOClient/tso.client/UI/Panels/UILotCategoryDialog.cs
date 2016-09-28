using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    public class UILotCategoryDialog : UIDialog
    {
        public Texture2D HouseCategoryBackgroundImage { get; set; }
        public UILotCategoryDialog() : base(UIDialogStyle.Close, true)
        {
            var script = RenderScript("lotcategoryselectiondialog.uis");
            SetSize(201, 180);

            for (int i=1; i<12; i++)
            {
                var bg = script.Create<UIImage>("BackgroundThumbImage"+i);
                bg.Texture = HouseCategoryBackgroundImage;
                AddAt(3, bg);
            }

            CloseButton.OnButtonClick += Close;
        }

        private void Close(Framework.UIElement button)
        {
            UIScreen.RemoveDialog(this);
        }
    }
}
