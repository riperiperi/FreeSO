using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Controls;
using TSOClient.LUI;

namespace TSOClient.Code.UI.Panels
{
    public class UILoginDialog : UIDialog
    {
        private UITextBox m_TxtAccName, m_TxtPass;

        public UILoginDialog() : base(UIDialogStyle.Standard, true)
        {
            SetSize(350, 225);


            m_TxtAccName = new UITextBox
            {
                X = 20,
                Y = 72,
                Opacity = 0.8f
            };
            m_TxtAccName.SetSize(310, 27);
            this.Add(m_TxtAccName);


            m_TxtPass = new UITextBox
            {
                X = 20,
                Y = 128,
                Opacity = 0.8f
            };
            m_TxtPass.SetSize(310, 27);
            this.Add(m_TxtPass);


            /** Login button **/
            var loginBtn = new UIButton {
                X = 116,
                Y = 170,
                Width = 100,
                ID = "LoginButton",
                Caption = "Login"
            };
            this.Add(loginBtn);


            var exitBtn = new UIButton
            {
                X = 226,
                Y = 170,
                Width = 100,
                ID = "ExitButton",
                Caption = "Exit"
            };
            this.Add(exitBtn);
        }
    }
}
