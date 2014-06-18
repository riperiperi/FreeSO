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
