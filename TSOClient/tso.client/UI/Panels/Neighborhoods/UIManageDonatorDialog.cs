using FSO.Client.Controllers;
using FSO.Client.Controllers.Panels;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.Client.Utils;
using FSO.Common.Rendering.Framework.Model;
using FSO.Server.Protocol.Electron.Model;
using FSO.SimAntics;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Model.Commands;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIManageDonatorDialog : UIDialog
    {
        public UIListBox RoommateListBox { get; set; }
        public UIListBoxTextStyle RoommateListBoxColors { get; set; }
        private UIInboxDropdown Dropdown;

        public UISlider RoommateListSlider { get; set; }
        public UIButton RoommateListScrollUpButton { get; set; }
        public UIButton RoommateScrollDownButton { get; set; }
        public UIImage BuildIcon { get; set; }

        public UILabel DonatorsLabel { get; set; }
        public UILotControl LotControl;

        private HashSet<uint> LastRoommates;
        private HashSet<uint> LastBuildRoommates;

        public bool Community;

        //listbox
        //smallThumb | avatarName | buildCheckbox | deleteButton

        public UIManageDonatorDialog(UILotControl lotControl) : base(UIDialogStyle.Standard | UIDialogStyle.OK, true)
        {
            this.LotControl = lotControl;
            Community = lotControl.vm.TSOState.CommunityLot;

            BuildIcon = new UIImage();
            var ui = RenderScript("fsodonatorlist.uis");
            var listBg = ui.Create<UIImage>("ListBoxBackground");
            AddAt(4, listBg);
            listBg.With9Slice(25, 25, 25, 25);
            listBg.Width += 110;
            listBg.Height += 50;

            Dropdown = ui.Create<UIInboxDropdown>("PullDownMenuSetup");
            Dropdown.OnSearch += (query) =>
            {
                FindController<GenericSearchController>()?.Search(query, false, (results) =>
                {
                    Dropdown.SetResults(results);
                });
            };
            Dropdown.OnSelect += AddDonator;
            Add(Dropdown);

            RoommateListSlider.AttachButtons(RoommateListScrollUpButton, RoommateScrollDownButton, 1);
            RoommateListBox.AttachSlider(RoommateListSlider);
            RoommateListBox.Columns[1].Alignment = Framework.TextAlignment.Left | Framework.TextAlignment.Middle;

            Caption = GameFacade.Strings.GetString("f114", (Community)?"6":"12");
            SetSize(405, 400);

            DonatorsLabel.CaptionStyle = DonatorsLabel.CaptionStyle.Clone();
            DonatorsLabel.CaptionStyle.Shadow = true;
            DonatorsLabel.Caption = GameFacade.Strings.GetString("f114", (Community) ? "5" : "13");
            AddAt(5, BuildIcon);

            var ctr = ControllerUtils.BindController<GenericSearchController>(this);
            UpdateDonatorList();
            OKButton.OnButtonClick += (btn) =>
            {
                UIScreen.RemoveDialog(this);
            };
        }

        private void AddDonator(uint donator, string name)
        {
            LotControl.vm.TSOState.Names.Precache(LotControl.vm, donator);

            if (Community)
            {
                UIAlert alert = null;
                alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Message = GameFacade.Strings.GetString("f114", "7", new string[] { name }),
                    Buttons = UIAlertButton.YesNo(
                        (btn) =>
                        {
                            LotControl.vm.SendCommand(new VMChangePermissionsCmd
                            {
                                TargetUID = donator,
                                Level = VMTSOAvatarPermissions.Roommate,
                            });
                            UIScreen.RemoveDialog(alert);
                        },
                        (btn) => { UIScreen.RemoveDialog(alert); }
                        )
                }, true);
            }
            else
            {
                UIAlert alert = null;
                alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Title = GameFacade.Strings.GetString("208", "5"),
                    Message = GameFacade.Strings.GetString("208", "6"),
                    Buttons = new UIAlertButton[] {
                        new UIAlertButton(UIAlertButtonType.Yes, (btn) => {
                            var screen = UIScreen.Current as CoreGameScreen;
                            if (screen != null)
                            {
                                screen.PersonPage.FindController<PersonPageController>().ChangeRoommate(
                                    ChangeRoommateType.INVITE,
                                    donator,
                                    screen.FindController<CoreGameScreenController>().GetCurrentLotID());
                            }

                            UIScreen.RemoveDialog(alert);
                            }),
                        new UIAlertButton(UIAlertButtonType.No, (btn) => UIScreen.RemoveDialog(alert))
                        },
                }, true);
            }
        }

        public override void Update(UpdateState state)
        {
            Invalidate();
            base.Update(state);

            if (HasListChanged())
            {
                var lastPos = RoommateListBox.ScrollOffset;
                UpdateDonatorList();
                RoommateListBox.ScrollOffset = lastPos;
            }
        }

        private bool HasListChanged()
        {
            if (LastRoommates == null) return true;

            var roomies = new HashSet<uint>(LotControl.vm.TSOState.Roommates);
            var buildRoomies = new HashSet<uint>(LotControl.vm.TSOState.BuildRoommates);
            if (LastRoommates.Count != roomies.Count || LastBuildRoommates.Count != buildRoomies.Count)
                return true;

            buildRoomies.IntersectWith(LastBuildRoommates);
            if (LastBuildRoommates.Count != buildRoomies.Count) return true; //at least one of the entries is different

            roomies.IntersectWith(LastRoommates);
            if (LastRoommates.Count != roomies.Count) return true; //at least one of the entries is different

            return false; //couldn't find a difference.
        }

        public void UpdateDonatorList()
        {
            var roomies = LotControl.vm.TSOState.Roommates;
            var roomiesNoOwner = new HashSet<uint>(roomies);
            roomiesNoOwner.Remove(LotControl.vm.TSOState.OwnerID);

            var canChange = (LotControl.ActiveEntity as VMAvatar)?.AvatarState?.Permissions >= VMTSOAvatarPermissions.Owner;

            var ui = Content.Content.Get().CustomUI;
            var btnTex = ui.Get("chat_cat.png").Get(GameFacade.GraphicsDevice);
            var btnCaption = TextStyle.DefaultLabel.Clone();
            var checkTex = GetTexture(0x0000083600000001);
            btnCaption.Size = 8;
            btnCaption.Shadow = true;

            Dropdown.DropDownButton.Disabled = !canChange;
            Dropdown.MenuTextEdit.Mode = canChange ? UITextEditMode.Editor : UITextEditMode.ReadOnly;
            RoommateListBox.Items = roomiesNoOwner.Select(x => {
                var check = new UIButton(checkTex);
                check.Selected = LotControl.vm.TSOState.BuildRoommates.Contains(x);
                check.Disabled = !canChange;
                check.OnButtonClick += (btn) =>
                {
                    check.Selected = !check.Selected;
                    LotControl.vm.SendCommand(new VMChangePermissionsCmd
                    {
                        TargetUID = x,
                        Level = (check.Selected) ? VMTSOAvatarPermissions.BuildBuyRoommate : VMTSOAvatarPermissions.Roommate,
                    });
                };
                var deleteBtn = new UIButton(btnTex) { Caption = "Delete", CaptionStyle = btnCaption };
                deleteBtn.OnButtonClick += (btn) =>
                {
                    LotControl.vm.SendCommand(new VMChangePermissionsCmd
                    {
                        TargetUID = x,
                        Level = VMTSOAvatarPermissions.Visitor,
                    });
                    UpdateDonatorList();
                };
                var personBtn = new UIPersonButton()
                {
                    AvatarId = x,
                    FrameSize = UIPersonButtonSize.SMALL
                };
                personBtn.LogicalParent = this;
                return new UIListBoxItem(
                    x,
                    personBtn,
                    LotControl.vm.TSOState.Names.GetNameForID(LotControl.vm, x),
                    check,
                    deleteBtn
                    );
                }).ToList();

            LastRoommates = new HashSet<uint>(roomies);
            LastBuildRoommates = new HashSet<uint>(LotControl.vm.TSOState.BuildRoommates);
        }
    }
}
