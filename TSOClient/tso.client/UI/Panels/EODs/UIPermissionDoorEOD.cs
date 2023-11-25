using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.SimAntics.NetPlay.EODs.Handlers;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIPermissionDoorEOD : UIEOD
    {
        public VMEODPermissionDoorMode Mode;
        public int MaxFee;
        public int PermissionState;
        public int CurDoorFee;
        public int Flags;

        public UIImage SubpanelBackground;
        public UIImage EntryField;

        public UITextEdit CodeTextEntry { get; set; }

        //normal mode
        public UIButton LotMembersCheckButton { get; set; }
        public UIButton Friends2CheckButton { get; set; }
        public UIButton Employees2CheckButton { get; set; }
        public UIButton VisitorsCheckButton { get; set; }

        public UILabel PermissionDoorDirections { get; set; }
        public UILabel LotMembers { get; set; }
        public UILabel Visitors { get; set; }
        public UILabel Friends2 { get; set; }
        public UILabel Employees2 { get; set; }

        //pay mode

        public UIButton FriendsCheckButton { get; set; }
        public UIButton EmployeesCheckButton { get; set; }

        public UILabel Exempt { get; set; }
        public UILabel Friends { get; set; }
        public UILabel Employees { get; set; }
        public UILabel SimoleanSymbol { get; set; }
        public UILabel DoorFee { get; set; }

        //code mode

        public UILabel CodeDoorDirections { get; set; }


        //state choice
        public UIButton CodeDoorButton { get; set; }
        public UIButton PayDoorButton { get; set; }
        public UIButton PermissionDoorButton { get; set; }

        public UILabel Loading { get; set; }

        public string CodeText = "";

        public UIPermissionDoorEOD(UIEODController controller) : base(controller)
        {
            var script = this.RenderScript("dooreod.uis");

            Loading.Visible = false;
            //SubpanelBackground = script.Create<UIImage>("SubpanelBackground");
            //AddAt(0, SubpanelBackground);

            EntryField = script.Create<UIImage>("EntryField");
            AddAt(1, EntryField);

            PermissionDoorButton.OnButtonClick += (btn) => { UploadState(0); };
            CodeDoorButton.OnButtonClick += (btn) => { UploadState(1); };
            PayDoorButton.OnButtonClick += (btn) => { UploadState(2); };

            LotMembersCheckButton.OnButtonClick += (btn) => { TriggerFlag(VMEODPermissionDoorFlags.AllowRoommate); };
            Friends2CheckButton.OnButtonClick += (btn) => { TriggerFlag(VMEODPermissionDoorFlags.AllowFriend); };
            Employees2CheckButton.OnButtonClick += (btn) => { TriggerFlag(VMEODPermissionDoorFlags.AllowEmployee); };
            VisitorsCheckButton.OnButtonClick += (btn) => { TriggerFlag(VMEODPermissionDoorFlags.AllowVisitor); };

            FriendsCheckButton.OnButtonClick += (btn) => { TriggerFlag(VMEODPermissionDoorFlags.MoneyExemptFriend); };
            EmployeesCheckButton.OnButtonClick += (btn) => { TriggerFlag(VMEODPermissionDoorFlags.MoneyExemptEmployee); };

            CodeTextEntry.MaxChars = 9;
            CodeTextEntry.X -= 14;
            CodeTextEntry.OnChange += CodeTextEntry_OnChange;
            CodeTextEntry.Alignment = Framework.TextAlignment.Center;
            CodeDoorDirections.Wrapped = true;

            PlaintextHandlers["door_init"] = P_Init;
            PlaintextHandlers["door_code"] = P_Code;
        }

        private bool IgnoreEntryChange;
        private void CodeTextEntry_OnChange(Framework.UIElement element)
        {
            if (IgnoreEntryChange) return;
            var newText = CodeTextEntry.CurrentText;

            int result = 0;
            int.TryParse(newText, out result);
            var max = (PermissionState == 2) ? MaxFee : 999999999;
            if (result > max)
            {
                result = max;
                var lastEntry = IgnoreEntryChange;
                IgnoreEntryChange = true;
                CodeTextEntry.CurrentText = max.ToString();
                IgnoreEntryChange = lastEntry;
            }

            if (PermissionState == 2)
            {
                CurDoorFee = result;
                Send("set_fee", CurDoorFee.ToString());
            }
            else if (PermissionState == 1)
            {
                CodeText = result.ToString();
                Send("set_code", CodeText);
            }
        }

        public override void OnClose()
        {
            Send("close", "");
            base.OnClose();
        }

        public void UploadState(int state)
        {
            if (Mode != VMEODPermissionDoorMode.Edit) return;
            Send("set_state", state.ToString());
            PermissionState = state;
            UpdateState();
        }

        public void TriggerFlag(VMEODPermissionDoorFlags flag)
        {
            if (Mode != VMEODPermissionDoorMode.Edit) return;
            Flags ^= (int)flag;
            Send("set_flags", Flags.ToString());
            UpdateFlags();
        }

        public void UpdateFlags()
        {
            var flags = (VMEODPermissionDoorFlags)Flags;
            LotMembersCheckButton.Selected = flags.HasFlag(VMEODPermissionDoorFlags.AllowRoommate);
            Friends2CheckButton.Selected = flags.HasFlag(VMEODPermissionDoorFlags.AllowFriend);
            Employees2CheckButton.Selected = flags.HasFlag(VMEODPermissionDoorFlags.AllowEmployee);
            VisitorsCheckButton.Selected = flags.HasFlag(VMEODPermissionDoorFlags.AllowVisitor);

            FriendsCheckButton.Selected = flags.HasFlag(VMEODPermissionDoorFlags.MoneyExemptFriend);
            EmployeesCheckButton.Selected = flags.HasFlag(VMEODPermissionDoorFlags.MoneyExemptEmployee);
        }

        public void UpdateState()
        {
            var normal = PermissionState == 0;
            var code = PermissionState == 1;
            var pay = PermissionState == 2;

            LotMembersCheckButton.Visible = normal;
            Friends2CheckButton.Visible = normal;
            Employees2CheckButton.Visible = normal;
            VisitorsCheckButton.Visible = normal;

            PermissionDoorDirections.Visible = normal;
            LotMembers.Visible = normal;
            Visitors.Visible = normal;
            Friends2.Visible = normal;
            Employees2.Visible = normal;

            FriendsCheckButton.Visible = pay;
            EmployeesCheckButton.Visible = pay;
            Exempt.Visible = pay;
            Friends.Visible = pay;
            Employees.Visible = pay;
            SimoleanSymbol.Visible = pay;
            DoorFee.Visible = pay;

            CodeDoorDirections.Visible = code;

            CodeTextEntry.Visible = code || pay;
            EntryField.Visible = code || pay;

            PermissionDoorButton.Selected = normal;
            CodeDoorButton.Selected = code;
            PayDoorButton.Selected = pay;

            IgnoreEntryChange = true;
            CodeTextEntry.CurrentText = (code) ? CodeText : CurDoorFee.ToString();
            CodeTextEntry.Mode = (Mode != VMEODPermissionDoorMode.Edit) ? UITextEditMode.ReadOnly : UITextEditMode.Editor;
            IgnoreEntryChange = false;
        }

        public void P_Init(string evt, string text)
        {
            var split = text.Split('\n');
            Mode = (VMEODPermissionDoorMode)(int.Parse(split[0]));
            MaxFee = int.Parse(split[1]);
            PermissionState = int.Parse(split[2]);
            CurDoorFee = int.Parse(split[3]);
            Flags = int.Parse(split[4]);

            if (Mode == VMEODPermissionDoorMode.CodeInput)
            {
                //don't open, just show a dialog.
                UIAlert alert = null;
                alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Title = GameFacade.Strings.GetString("192", "11"),
                    Message = GameFacade.Strings.GetString("192", "1"),
                    Buttons = UIAlertButton.Ok((btn) =>
                    {
                        Send("try_code", alert.ResponseText.Trim());
                        UIScreen.RemoveDialog(alert);
                    }),
                    TextEntry = true,
                    MaxChars = 9,
                }, true);
            }
            else
            {
                EODController.ShowEODMode(new EODLiveModeOpt
                {
                    Buttons = 0,
                    Expandable = false,
                    Height = EODHeight.Normal,
                    Length = EODLength.Short,
                    Timer = EODTimer.None,
                    Tips = EODTextTips.None
                });
            }

            UpdateState();
            UpdateFlags();
            if (Mode == VMEODPermissionDoorMode.Edit)
            {
                Send("set_state", PermissionState.ToString());
                Send("set_flags", Flags.ToString());
                Send("set_fee", CurDoorFee.ToString());
            }
        }

        public void P_Code(string evt, string text)
        {
            CodeText = text;
            UpdateState();
        }
    }
}
