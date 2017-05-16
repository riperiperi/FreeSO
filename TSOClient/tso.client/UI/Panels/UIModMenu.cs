using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.Debug.Content;
using FSO.Server.Protocol.Electron.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    public class UIModMenu : UIDialog
    {
        private UIImage Background;
        private UIButton IPBanButton;

        public uint AvatarID;

        public UIModMenu() : base(UIDialogStyle.Tall | UIDialogStyle.Close, true)
        {
            SetSize(380, 300);
            Caption = "Do what to this user?";

            Position = new Microsoft.Xna.Framework.Vector2(
                (GlobalSettings.Default.GraphicsWidth / 2.0f) - (480/2),
                (GlobalSettings.Default.GraphicsHeight / 2.0f) - 150
            );

            IPBanButton = new UIButton();
            IPBanButton.Caption = "IP Ban";
            IPBanButton.Position = new Microsoft.Xna.Framework.Vector2(40, 50);
            IPBanButton.Width = 300;
            IPBanButton.OnButtonClick += x =>
            {
                var controller = FindController<FSO.Client.Controllers.CoreGameScreenController>();
                if (controller != null)
                    controller.ModRequest(AvatarID, ModerationRequestType.IPBAN_USER);
                UIScreen.RemoveDialog(this);
            };
            Add(IPBanButton);

            var BanButton = new UIButton();
            BanButton.Caption = "Ban User";
            BanButton.Position = new Microsoft.Xna.Framework.Vector2(40, 90);
            BanButton.Width = 300;
            BanButton.OnButtonClick += x =>
            {
                var controller = FindController<FSO.Client.Controllers.CoreGameScreenController>();
                if (controller != null)
                    controller.ModRequest(AvatarID, ModerationRequestType.BAN_USER);
                UIScreen.RemoveDialog(this);
            };
            Add(BanButton);

            var kickButton = new UIButton();
            kickButton.Caption = "Kick Avatar";
            kickButton.Position = new Microsoft.Xna.Framework.Vector2(40, 130);
            kickButton.Width = 300;
            kickButton.OnButtonClick += x =>
            {
                var controller = FindController<FSO.Client.Controllers.CoreGameScreenController>();
                if (controller != null)
                    controller.ModRequest(AvatarID, ModerationRequestType.KICK_USER);
                UIScreen.RemoveDialog(this);
            };
            Add(kickButton);

            CloseButton.OnButtonClick += CloseButton_OnButtonClick;
        }

        private void CloseButton_OnButtonClick(UIElement button)
        {
            UIScreen.RemoveDialog(this);
        }
    }
}
