/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Framework;
using TSOClient.LUI;

namespace TSOClient.Code.UI.Panels
{
    public class UIExitDialog : UIDialog
    {
        /// <summary>
        /// Exit buttons
        /// </summary>
        public UIButton ReLoginButton { get; set; }
        public UIButton ExitButton { get; set; }
        public UIButton CancelButton { get; set; }

        public UIExitDialog()
            : base(UIDialogStyle.Standard, true)
        {
            this.RenderScript("exitdialog.uis");
            this.SetSize(380, 180);

            ExitButton.OnButtonClick += new ButtonClickDelegate(ExitButton_OnButtonClick);
            CancelButton.OnButtonClick += new ButtonClickDelegate(CancelButton_OnButtonClick);
            ReLoginButton.OnButtonClick += new ButtonClickDelegate(ReLoginButton_OnButtonClick);
        }

        private void ExitButton_OnButtonClick(UIElement button)
        {
            GameFacade.Kill();
        }

        private void CancelButton_OnButtonClick(UIElement button)
        {
            UIScreen.RemoveDialog(this);
        }

        private void ReLoginButton_OnButtonClick(UIElement button)
        {
            var alert = UIScreen.ShowAlert(new UIAlertOptions { Title = "Not Implemented", Message = "This feature is not implemented yet!" }, true);
        }
    }
}