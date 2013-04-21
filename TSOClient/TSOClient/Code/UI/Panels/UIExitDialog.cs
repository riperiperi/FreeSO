using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Controls;

namespace TSOClient.Code.UI.Panels
{
    public class UIExitDialog : UIDialog
    {
        public UIExitDialog() : base(UIDialogStyle.Standard, true)
        {
            this.RenderScript("exitdialog.uis");
        }
    }
}
