using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Controls;
using TSOClient.LUI;
using TSOClient.Code.UI.Framework;

namespace TSOClient.Code.UI.Panels
{
    public class UILoginDialog : UIDialog
    {
        private UITextBox m_TxtAccName, m_TxtPass;

        public UILoginDialog() : base(UIDialogStyle.Standard, true)
        {
            this.Caption = GameFacade.Strings.GetString("UIText", "209", "1");

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
                Caption = GameFacade.Strings.GetString("UIText", "209", "2")
            };
            this.Add(loginBtn);
            loginBtn.OnButtonClick += new ButtonClickDelegate(loginBtn_OnButtonClick);

            var exitBtn = new UIButton
            {
                X = 226,
                Y = 170,
                Width = 100,
                ID = "ExitButton",
                Caption = GameFacade.Strings.GetString("UIText", "209", "3")
            };
            this.Add(exitBtn);
            exitBtn.OnButtonClick += new ButtonClickDelegate(exitBtn_OnButtonClick);


            this.Add(new UILabel
            {
                Caption = GameFacade.Strings.GetString("UIText", "209", "4"),
                X = 24,
                Y = 50
            });

            this.Add(new UILabel
            {
                Caption = GameFacade.Strings.GetString("UIText", "209", "5"),
                X = 24,
                Y = 106
            });

        }



        void loginBtn_OnButtonClick(UIElement button)
        {
            GameFacade.Controller.ShowPersonSelection();
        }

        void exitBtn_OnButtonClick(UIElement button)
        {
            //var exitDialog = new UIExitDialog();
            //Parent.Add(exitDialog);
        }
    }
}
