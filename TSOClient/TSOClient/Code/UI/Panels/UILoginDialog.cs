using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Controls;

namespace TSOClient.Code.UI.Panels
{
    public class UILoginDialog : UIDialog
    {
        public UILoginDialog() : base(UIDialogStyle.Standard)
        {
            SetSize(350, 225);
        }
    }
}
