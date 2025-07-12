using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveLandingDialog : UIDialog
    {
        public UIButton CreateButton;
        public UIButton JoinButton;
        public UIButton QuickStartButton;

        public UIArchiveLandingDialog() : base(UIDialogStyle.Standard, true)
        {
            Caption = "Welcome to FreeSO";

            var vbox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Center };

            vbox.Add(new UILabel()
            {
                Caption = "Create or join a server to get started."
            });

            var createJoinHbox = new UIHBoxContainer();

            createJoinHbox.Add(CreateButton = new UIButton()
            {
                Caption = "Create Server"
            });

            createJoinHbox.Add(JoinButton = new UIButton()
            {
                Caption = "Join Server"
            });

            createJoinHbox.AutoSize();

            vbox.Add(createJoinHbox);

            vbox.Add(QuickStartButton = new UIButton()
            {
                Caption = "Quick Start (offline)"
            });

            Add(vbox);

            vbox.AutoSize();
            vbox.Position = new Vector2(20, 45);

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 70);

            CreateButton.OnButtonClick += Create;
            JoinButton.OnButtonClick += Join;
            QuickStartButton.OnButtonClick += QuickStart;
        }

        private void QuickStart(Framework.UIElement button)
        {
            throw new NotImplementedException();
        }

        private void Join(Framework.UIElement button)
        {
            FindController<ConnectArchiveController>().SwitchMode(ConnectArchiveMode.Join);
        }

        private void Create(Framework.UIElement button)
        {
            FindController<ConnectArchiveController>().SwitchMode(ConnectArchiveMode.Create);
        }
    }
}
