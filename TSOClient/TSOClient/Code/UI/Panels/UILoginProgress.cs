/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
using TSOClient.Code.UI.Model;

namespace TSOClient.Code.UI.Panels
{
    public class UILoginProgress : UIDialog
    {
        private UIProgressBar m_ProgressBar;
        private UILabel m_ProgressLabel;

        public UILoginProgress() : base(UIDialogStyle.Standard, false)
        {
            this.SetSize(400, 180);
            this.Caption = GameFacade.Strings.GetString("210", "1");


            /**
             * Label background
             */
            var bgImg = new UIImage(UITextBox.StandardBackground)
            {
                X = 20,
                Y = 120
            };
            bgImg.SetSize(360, 27);
            this.Add(bgImg);


            m_ProgressBar = new UIProgressBar() {
                X = 20,
                Y = 66,
                Value = 0
            };
            m_ProgressBar.SetSize(360, 27);
            this.Add(m_ProgressBar);

            this.Add(new UILabel
            {
                Caption = GameFacade.Strings.GetString("210", "2"),
                X = 20,
                Y = 44
            });

            this.Add(new UILabel
            {
                Caption = GameFacade.Strings.GetString("210", "3"),
                X = 20,
                Y = 97
            });

            m_ProgressLabel = new UILabel{
                Caption = GameFacade.Strings.GetString("210", "4"),
                X = 31,
                Y = 122
            };
            this.Add(m_ProgressLabel);
        }


        public float Progress
        {
            set
            {
                m_ProgressBar.Value = value;
            }
        }

        public string ProgressCaption
        {
            set
            {
                m_ProgressLabel.Caption = value;
            }
        }
    }
}
