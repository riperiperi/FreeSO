/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;

namespace FSO.Client.UI.Panels
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
            FSOFacade.Controller.Disconnect(true);
        }
    }
}