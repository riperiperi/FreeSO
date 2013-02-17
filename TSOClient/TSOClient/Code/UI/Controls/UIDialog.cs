using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;

namespace TSOClient.Code.UI.Controls
{
    public enum UIDialogStyle
    {
        Standard,
        StandardTall
    }

    /// <summary>
    /// Generic dialog component
    /// </summary>
    public class UIDialog : UIContainer
    {
        private UIImage Background;

        public UIDialog(UIDialogStyle style)
        {
            switch (style)
            {
                case UIDialogStyle.Standard:
                    Background = new UIImage(GetTexture(0xE500000002))
                                    .With9Slice(41, 41, 60, 40);
                    break;

                case UIDialogStyle.StandardTall:
                    Background = new UIImage(GetTexture(0x15700000002))
                                    .With9Slice(41, 41, 66, 40);
                    break;
            }

            this.Add(Background);
        }


        /// <summary>
        /// Set the size of the dialog
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetSize(int width, int height)
        {
            Background.SetSize(width, height);
        }

    }
}
