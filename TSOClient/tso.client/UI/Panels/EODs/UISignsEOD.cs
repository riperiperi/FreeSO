using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public class UISignsEOD : UIEOD
    {
        public VMEODSignsMode InitialMode;
        public VMEODSignsMode Mode;
        public bool OwnerMode;
        public int MaxChars;

        public UIImage OwnerWriteTextSubpanel;
        public UIImage WriteTextSubpanel;
        public UIImage ReadTextSubpanel;
        public UIImage OwnerPermissionsSubpanel;

        public UIButton OwnerPermissionsButton { get; set; }
        public UIButton OwnerWriteTextButton { get; set; }

        public UIButton RoommateReadCheckButton { get; set; }
        public UIButton FriendReadCheckButton { get; set; }
        public UIButton VisitorReadCheckButton { get; set; }

        public UIButton RoommateWriteCheckButton { get; set; }
        public UIButton FriendWriteCheckButton { get; set; }
        public UIButton VisitorWriteCheckButton { get; set; }

        public UILabel TextRead { get; set; }
        public UILabel TextRRoommates { get; set; }
        public UILabel TextRFriends { get; set; }
        public UILabel TextRVisitors { get; set; }
        public UILabel TextWrite { get; set; }
        public UILabel TextWRoommates { get; set; }
        public UILabel TextWFriends { get; set; }
        public UILabel TextWVisitors { get; set; }


        public UITextEdit OwnerWriteTextBox { get; set; }
        public UITextEdit WriteTextBox { get; set; }
        public UITextEdit ReadTextBox { get; set; }

        public VMEODSignsData Data;

        public UISignsEOD(UIEODController controller) : base(controller)
        {
            var script = this.RenderScript("signseod.uis");

            OwnerWriteTextSubpanel = script.Create<UIImage>("OwnerWriteTextSubpanel");
            AddAt(0, OwnerWriteTextSubpanel);
            WriteTextSubpanel = script.Create<UIImage>("WriteTextSubpanel");
            AddAt(0, WriteTextSubpanel);
            ReadTextSubpanel = script.Create<UIImage>("ReadTextSubpanel");
            AddAt(0, ReadTextSubpanel);
            OwnerPermissionsSubpanel = script.Create<UIImage>("OwnerPermissionsSubpanel");
            AddAt(0, OwnerPermissionsSubpanel);

            PlaintextHandlers["signs_init"] = P_Init;
            BinaryHandlers["signs_show"] = B_Show;

            OwnerPermissionsButton.OnButtonClick += OwnerPermissionsButton_OnButtonClick;
            OwnerWriteTextButton.OnButtonClick += OwnerWriteTextButton_OnButtonClick;

            RoommateReadCheckButton.OnButtonClick += TogglePermission;
            FriendReadCheckButton.OnButtonClick += TogglePermission;
            VisitorReadCheckButton.OnButtonClick += TogglePermission;

            RoommateWriteCheckButton.OnButtonClick += TogglePermission;
            FriendWriteCheckButton.OnButtonClick += TogglePermission;
            VisitorWriteCheckButton.OnButtonClick += TogglePermission;

            WriteTextSubpanel.Position = ReadTextSubpanel.Position; //it's wrong normally?
            WriteTextBox.Position = ReadTextBox.Position;

            OwnerWriteTextBox.InitDefaultSlider();
            WriteTextBox.InitDefaultSlider();
            ReadTextBox.InitDefaultSlider();
        }

        private void OwnerWriteTextButton_OnButtonClick(UIElement button)
        {
            SetMode(VMEODSignsMode.OwnerWrite);
        }

        private void OwnerPermissionsButton_OnButtonClick(UIElement button)
        {
            SetMode(VMEODSignsMode.OwnerPermissions);
        }

        public override void OnClose()
        {
            string replaceText = "";
            switch (Mode)
            {
                case VMEODSignsMode.OwnerPermissions:
                case VMEODSignsMode.OwnerWrite:
                    replaceText = OwnerWriteTextBox.CurrentText;
                    break;
                case VMEODSignsMode.Read:
                    replaceText = "";
                    break;
                case VMEODSignsMode.Write:
                    replaceText = WriteTextBox.CurrentText;
                    break;
            }
            Data.Text = replaceText;
            Send("set_message", Data.Save());
            Send("close", "");
            base.OnClose();
        }

        public void P_Init(string evt, string text)
        {
            var split = text.Split('\n');
            Mode = (VMEODSignsMode)(int.Parse(split[0]));
            InitialMode = Mode;
            MaxChars = int.Parse(split[0]);

            OwnerMode = (Mode == VMEODSignsMode.OwnerPermissions) || (Mode == VMEODSignsMode.OwnerWrite);
        }

        public void B_Show(string evt, byte[] data)
        {
            Data = new VMEODSignsData(data);
            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Expandable = false,
                Height = EODHeight.Tall,
                Length = (OwnerMode)?EODLength.Medium:EODLength.Full,
                Timer = EODTimer.None,
                Tips = EODTextTips.None
            });
            SetMode(Mode);
            OwnerWriteTextBox.CurrentText = Data.Text;
            WriteTextBox.CurrentText = Data.Text;
            ReadTextBox.CurrentText = Data.Text;
            UpdatePermissions();
        }

        public void TogglePermission(UIElement elem)
        {
            VMEODSignPermissionFlags flag = 0;
            if (elem == RoommateReadCheckButton) flag = VMEODSignPermissionFlags.RoomieRead;
            else if (elem == FriendReadCheckButton) flag = VMEODSignPermissionFlags.FriendRead;
            else if (elem == VisitorReadCheckButton) flag = VMEODSignPermissionFlags.VisitorRead;
            else if (elem == RoommateWriteCheckButton) flag = VMEODSignPermissionFlags.RoomieWrite;
            else if (elem == FriendWriteCheckButton) flag = VMEODSignPermissionFlags.FriendWrite;
            else if (elem == VisitorWriteCheckButton) flag = VMEODSignPermissionFlags.VisitorWrite;

            Data.Flags ^= (ushort)flag;
            UpdatePermissions();
        }

        public void UpdatePermissions()
        {
            var flags = (VMEODSignPermissionFlags)Data.Flags;
            RoommateReadCheckButton.Selected = (flags & VMEODSignPermissionFlags.RoomieRead) > 0;
            FriendReadCheckButton.Selected = (flags & VMEODSignPermissionFlags.FriendRead) > 0;
            VisitorReadCheckButton.Selected = (flags & VMEODSignPermissionFlags.VisitorRead) > 0;

            RoommateWriteCheckButton.Selected = (flags & VMEODSignPermissionFlags.RoomieWrite) > 0;
            FriendWriteCheckButton.Selected = (flags & VMEODSignPermissionFlags.FriendWrite) > 0;
            VisitorWriteCheckButton.Selected = (flags & VMEODSignPermissionFlags.VisitorWrite) > 0;
        }

        public void SetMode(VMEODSignsMode mode)
        {
            Mode = mode;

            OwnerPermissionsButton.Visible = OwnerMode;
            OwnerWriteTextButton.Visible = OwnerMode;

            var perms = mode == VMEODSignsMode.OwnerPermissions;

            OwnerPermissionsButton.Visible = OwnerMode;
            OwnerWriteTextButton.Visible = OwnerMode;

            RoommateReadCheckButton.Visible = perms;
            FriendReadCheckButton.Visible = perms;
            VisitorReadCheckButton.Visible = perms;

            RoommateWriteCheckButton.Visible = perms;
            FriendWriteCheckButton.Visible = perms;
            VisitorWriteCheckButton.Visible = perms;

            TextRead.Visible = perms;
            TextRRoommates.Visible = perms;
            TextRFriends.Visible = perms;
            TextRVisitors.Visible = perms;
            TextWrite.Visible = perms;
            TextWRoommates.Visible = perms;
            TextWFriends.Visible = perms;
            TextWVisitors.Visible = perms;

            OwnerWriteTextSubpanel.Visible = mode == VMEODSignsMode.OwnerWrite;
            OwnerWriteTextBox.Visible = mode == VMEODSignsMode.OwnerWrite;
            WriteTextSubpanel.Visible = mode == VMEODSignsMode.Write;
            WriteTextBox.Visible = mode == VMEODSignsMode.Write;
            ReadTextSubpanel.Visible = mode == VMEODSignsMode.Read;
            ReadTextBox.Visible = mode == VMEODSignsMode.Read;
            OwnerPermissionsSubpanel.Visible = perms;
        }
    }
}
