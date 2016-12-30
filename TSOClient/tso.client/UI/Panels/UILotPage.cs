using FSO.Client.Controllers;
using FSO.Client.Controllers.Panels;
using FSO.Client.Model;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common.DataService.Model;
using FSO.Common.Enum;
using FSO.Common.Utils;
using FSO.Files;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    public class UILotPage : UIContainer
    {
        private UIImage BackgroundDescriptionImage;
        private UIImage BackgroundDescriptionEditImage;
        private UIImage BackgroundHouseLeaderThumbImage;
        private UIImage BackgroundHouseCategoryThumbImage;
        private UIImage BackgroundNumOccupantsImage;

        private UIImage BackgroundContractedImage;
        private UIImage BackgroundExpandedImage;

        public Texture2D ContractedBackgroundImage { get; set; }
        public Texture2D ExpandedBackgroundImage { get; set; }

        public UIButton ContractButton { get; set; }
        public UIButton ExpandButton { get; set; }
        public UIButton ExpandedCloseButton { get; set; }
        public UIButton ContractedCloseButton { get; set; }

        public UITextEdit HouseDescriptionTextEdit { get; set; }
        public UISlider HouseDescriptionSlider { get; set; }
        public UIButton HouseDescriptionScrollUpButton { get; set; }
        public UIButton HouseDescriptionScrollDownButton { get; set; }

        public UIButton VisitorsLeftScrollButton { get; set; }
        public UIButton VisitorsRightScrollButton { get; set; }

        public UIButton HouseCategoryButton { get; set; }

        public UILabel HouseValueLabel { get; set; }
        public UILabel OccupantsNumberLabel { get; set; }

        public UIButton NeighborhoodNameButton { get; set; }
        public UIButton HouseNameButton { get; set; }
        public UIButton HouseLinkButton { get; set; }

        public Texture2D HouseCategory_MoneyButtonImage { get; set; }
        public Texture2D HouseCategory_OffbeatButtonImage { get; set; }
        public Texture2D HouseCategory_RomanceButtonImage { get; set; }
        public Texture2D HouseCategory_ServicesButtonImage { get; set; }
        public Texture2D HouseCategory_ShoppingButtonImage { get; set; }
        public Texture2D HouseCategory_SkillsButtonImage { get; set; }
        public Texture2D HouseCategory_WelcomeButtonImage { get; set; }
        public Texture2D HouseCategory_GamesButtonImage { get; set; }
        public Texture2D HouseCategory_EntertainmentButtonImage { get; set; }
        public Texture2D HouseCategory_ResidenceButtonImage { get; set; }
        public Texture2D HouseCategory_NoCategoryButtonImage { get; set; }
        public Texture2D RoommateThumbButtonImage { get; set; }
        public Texture2D VisitorThumbButtonImage { get; set; }

        private UILotThumbButton LotThumbnail { get; set; }
        private UIRoommateList RoommateList { get; set; }
        private UIPersonButton OwnerButton { get; set; }
        private Texture2D DefaultThumb;
        private string OriginalDescription;

        public Binding<Lot> CurrentLot;
        public override Vector2 Size { get; set; }

        private bool _Open;

        public UILotPage()
        {
            var script = RenderScript("housepage.uis");

            BackgroundNumOccupantsImage = script.Create<UIImage>("BackgroundNumOccupantsImage");
            AddAt(0, BackgroundNumOccupantsImage);

            BackgroundHouseCategoryThumbImage = script.Create<UIImage>("BackgroundHouseCategoryThumbImage");
            AddAt(0, BackgroundHouseCategoryThumbImage);

            BackgroundHouseLeaderThumbImage = script.Create<UIImage>("BackgroundHouseLeaderThumbImage");
            AddAt(0, BackgroundHouseLeaderThumbImage);

            BackgroundDescriptionEditImage = script.Create<UIImage>("BackgroundDescriptionEditImage");
            AddAt(0, BackgroundDescriptionEditImage);

            BackgroundDescriptionImage = script.Create<UIImage>("BackgroundDescriptionImage");
            AddAt(0, BackgroundDescriptionImage);
            
            BackgroundContractedImage = new UIImage();
            BackgroundContractedImage.Texture = ContractedBackgroundImage;
            this.AddAt(0, BackgroundContractedImage);
            BackgroundExpandedImage = new UIImage();
            BackgroundExpandedImage.Texture = ExpandedBackgroundImage;
            this.AddAt(0, BackgroundExpandedImage);

            ContractButton.OnButtonClick += (x) => Open = false;
            ExpandButton.OnButtonClick += (x) => Open = true;

            ExpandedCloseButton.OnButtonClick += Close;
            ContractedCloseButton.OnButtonClick += Close;

            LotThumbnail = script.Create<UILotThumbButton>("HouseThumbSetup");
            LotThumbnail.Init(RoommateThumbButtonImage, VisitorThumbButtonImage);
            DefaultThumb = TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, GameFacade.GameFilePath("userdata/houses/defaulthouse.bmp"));
            TextureUtils.ManualTextureMask(ref DefaultThumb, new uint[] { 0xFF000000 });
            LotThumbnail.SetThumbnail(DefaultThumb, 0);
            Add(LotThumbnail);

            RoommateList = script.Create<UIRoommateList>("RoommateList");
            Add(RoommateList);

            OwnerButton = script.Create<UIPersonButton>("HouseLeaderThumbSetup");
            OwnerButton.FrameSize = UIPersonButtonSize.LARGE;
            Add(OwnerButton);

            /** Drag **/
            UIUtils.MakeDraggable(BackgroundContractedImage, this, true);
            UIUtils.MakeDraggable(BackgroundExpandedImage, this, true);

            /** Description scroll **/
            HouseDescriptionSlider.AttachButtons(HouseDescriptionScrollUpButton, HouseDescriptionScrollDownButton, 1);
            HouseDescriptionTextEdit.AttachSlider(HouseDescriptionSlider);

            HouseLinkButton.OnButtonClick += JoinLot;
            HouseCategoryButton.OnButtonClick += ChangeCategory;
            HouseNameButton.OnButtonClick += ChangeName;
            LotThumbnail.OnLotClick += JoinLot;

            CurrentLot = new Binding<Lot>()
                .WithBinding(HouseNameButton, "Caption", "Lot_Name")
                .WithBinding(HouseValueLabel, "Caption", "Lot_Price", x => MoneyFormatter.Format((uint)x))
                .WithBinding(OccupantsNumberLabel, "Caption", "Lot_NumOccupants", x => x.ToString())
                .WithBinding(OwnerButton, "AvatarId", "Lot_LeaderID")
                .WithBinding(HouseCategoryButton, "Texture", "Lot_Category", x =>
                {
                    var category = (LotCategory)Enum.Parse(typeof(LotCategory), x.ToString());
                    switch (category)
                    {
                        case LotCategory.none:
                            return HouseCategory_NoCategoryButtonImage;
                        case LotCategory.welcome:
                            return HouseCategory_WelcomeButtonImage;
                        case LotCategory.money:
                            return HouseCategory_MoneyButtonImage;
                        case LotCategory.entertainment:
                            return HouseCategory_EntertainmentButtonImage;
                        case LotCategory.games:
                            return HouseCategory_GamesButtonImage;
                        case LotCategory.offbeat:
                            return HouseCategory_OffbeatButtonImage;
                        case LotCategory.residence:
                            return HouseCategory_ResidenceButtonImage;
                        case LotCategory.romance:
                            return HouseCategory_RomanceButtonImage;
                        case LotCategory.services:
                            return HouseCategory_ServicesButtonImage;
                        case LotCategory.shopping:
                            return HouseCategory_ShoppingButtonImage;
                        case LotCategory.skills:
                            return HouseCategory_SkillsButtonImage;
                        default:
                            return null;
                    }
                }).WithBinding(HouseCategoryButton, "Position", "Lot_Category", x =>
                {
                    return new Vector2(69+11-HouseCategoryButton.Texture.Width/8, 164+11-HouseCategoryButton.Texture.Height/2);
                })
                .WithMultiBinding(x => RefreshUI(), "Lot_LeaderID", "Lot_IsOnline", "Lot_Thumbnail", "Lot_Description", "Lot_RoommateVec");

            RefreshUI();
            NeighborhoodNameButton.Visible = false;

            Size = BackgroundExpandedImage.Size.ToVector2();
        }

        private void ChangeName(UIElement button)
        {
            var lotName = new UILotPurchaseDialog();
            lotName.OnNameChosen += (name) =>
            {
                if (CurrentLot != null && CurrentLot.Value != null && FindController<CoreGameScreenController>().IsMe(CurrentLot.Value.Lot_LeaderID))
                {
                    CurrentLot.Value.Lot_Name = name;
                    FindController<LotPageController>().SaveName(CurrentLot.Value);
                }
                UIScreen.RemoveDialog(lotName);
            };
            if (CurrentLot != null && CurrentLot.Value != null)
            {
                lotName.NameTextEdit.CurrentText = CurrentLot.Value.Lot_Name;
            }
            UIScreen.GlobalShowDialog(new DialogReference
            {
                Dialog = lotName,
                Controller = this,
                Modal = true,
            });
        }

        private void ChangeCategory(UIElement button)
        {
            //initiate dialog to get the target category
            var catDialog = new UILotCategoryDialog();
            UIScreen.GlobalShowDialog(new DialogReference
            {
                Dialog = catDialog,
                Controller = this,
                Modal = true,
            });
            catDialog.OnCategoryChange += ChangeCategory;
        }

        private void ChangeCategory(LotCategory cat)
        {
            if (CurrentLot != null && CurrentLot.Value != null && FindController<CoreGameScreenController>().IsMe(CurrentLot.Value.Lot_LeaderID))
            {
                CurrentLot.Value.Lot_Category = (byte)cat;
                FindController<LotPageController>().SaveCategory(CurrentLot.Value);
            }
        }

        public void TrySaveDescription()
        {
            if (CurrentLot != null && CurrentLot.Value != null && FindController<CoreGameScreenController>().IsMe(CurrentLot.Value.Lot_LeaderID)
                && HouseDescriptionTextEdit.CurrentText != CurrentLot.Value.Lot_Description)
            {
                CurrentLot.Value.Lot_Description = HouseDescriptionTextEdit.CurrentText;
                FindController<LotPageController>().SaveDescription(CurrentLot.Value);
            }
        }

        private void JoinLot(UIElement e) {
            FindController<CoreGameScreenController>().JoinLot(CurrentLot.Value.Id);
            Close(e);
        }

        private uint _Lot_LeaderID;
        public uint Lot_LeaderID
        {
            get { return _Lot_LeaderID; }
            set
            {
                _Lot_LeaderID = value;
            }
        }

        private void Close(UIElement button)
        {
            FindController<LotPageController>().Close();
        }

        public bool Open
        {
            get { return _Open; }
            set
            {
                _Open = value;
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            var isOpen = _Open == true;
            var isClosed = _Open == false;
            var isMyProperty = false;
            var isRoommate = false;
            var isOnline = false;

            if(CurrentLot != null && CurrentLot.Value != null)
            {
                isOnline = CurrentLot.Value.Lot_IsOnline || (CurrentLot.Value.Lot_LotAdmitInfo?.LotAdmitInfo_AdmitMode == 4);
                isMyProperty = FindController<CoreGameScreenController>().IsMe(CurrentLot.Value.Lot_LeaderID);
                    
                var roomies = new List<uint>();
                if (CurrentLot.Value.Lot_RoommateVec != null) roomies.AddRange(CurrentLot.Value.Lot_RoommateVec);
                roomies.Remove(CurrentLot.Value.Lot_LeaderID);
                foreach (var roomie in roomies) if (FindController<CoreGameScreenController>().IsMe(roomie)) isRoommate = true;
                RoommateList.UpdateList(roomies);

                var thumb = CurrentLot.Value.Lot_Thumbnail.Data;
                if (((thumb?.Length) ?? 0) == 0)
                    LotThumbnail.SetThumbnail(DefaultThumb, 0);
                else
                    LotThumbnail.SetThumbnail(ImageLoader.FromStream(GameFacade.GraphicsDevice, new MemoryStream(thumb)), CurrentLot.Value.Id);

                if (OriginalDescription != CurrentLot.Value.Lot_Description)
                {
                    OriginalDescription = CurrentLot.Value.Lot_Description;
                    HouseDescriptionTextEdit.CurrentText = OriginalDescription;
                }
            }

            var canJoin = isMyProperty || isRoommate || isOnline;

            HouseNameButton.Disabled = !isMyProperty;

            BackgroundContractedImage.Visible = isClosed;
            BackgroundExpandedImage.Visible = isOpen;
            RoommateList.Visible = isOpen;

            ExpandButton.Visible = isClosed;
            ExpandedCloseButton.Visible = isOpen;

            ContractButton.Visible = isOpen;
            ContractedCloseButton.Visible = isClosed;

            BackgroundDescriptionImage.Visible = isOpen && !isMyProperty;
            BackgroundDescriptionEditImage.Visible = isOpen && isMyProperty;
            HouseDescriptionTextEdit.Mode = (isMyProperty) ? UITextEditMode.Editor : UITextEditMode.ReadOnly;

            HouseDescriptionSlider.Visible =
                HouseDescriptionTextEdit.Visible =
                HouseDescriptionScrollUpButton.Visible =
                HouseDescriptionScrollDownButton.Visible = isOpen;

            VisitorsLeftScrollButton.Visible = VisitorsRightScrollButton.Visible = isOpen;

            HouseCategoryButton.Disabled = !isMyProperty;
            if (isMyProperty) LotThumbnail.Mode = UILotRelationship.OWNER;
            else if (isRoommate) LotThumbnail.Mode = UILotRelationship.ROOMMATE;
            else LotThumbnail.Mode = UILotRelationship.VISITOR;

            if(canJoin){
                HouseLinkButton.Disabled = false;
                LotThumbnail.Disabled = false;
            }else{
                HouseLinkButton.Disabled = true;
                LotThumbnail.Disabled = true;
            }
        }
    }

    public class UIRoommateList : UIContainer
    {
        private List<UIPersonButton> RoommateButtons = new List<UIPersonButton>();
        public UIRoommateList() : base()
        {
        }

        public void UpdateList(List<uint> roommates)
        {
            while (RoommateButtons.Count > roommates.Count) {
                Remove(RoommateButtons[RoommateButtons.Count - 1]);
                RoommateButtons.RemoveAt(RoommateButtons.Count - 1);
            }
            while (roommates.Count > RoommateButtons.Count) {
                var btn = new UIPersonButton();
                btn.FrameSize = UIPersonButtonSize.SMALL;
                btn.X = RoommateButtons.Count * (20 + 6); //6 is gutter size
                Add(btn);
                RoommateButtons.Add(btn);
            }
            for (int i = 0; i < RoommateButtons.Count; i++)
            {
                if (RoommateButtons[i].AvatarId != roommates[i])
                    RoommateButtons[i].AvatarId = roommates[i];
            }
        }
    }

    public class UILotThumbButton : UIContainer
    {
        private UIButton RoommateButton;
        private UIButton VisitorButton;
        private UIImage Thumbnail;
        public ButtonClickDelegate OnLotClick;
        public uint CurrentLotThumb;

        private UILotRelationship _Mode;

        public UILotThumbButton()
        {
        }

        public bool Disabled
        {
            get
            {
                return RoommateButton.Disabled;
            }
            set
            {
                RoommateButton.Disabled = value;
                VisitorButton.Disabled = value;
            }
        }

        public void Init(Texture2D roommateBtnTexture, Texture2D visitorButtonTexture)
        {
            RoommateButton = new UIButton(roommateBtnTexture);
            RoommateButton.Visible = false;
            Add(RoommateButton);

            VisitorButton = new UIButton(visitorButtonTexture);
            VisitorButton.Visible = false;
            Add(VisitorButton);

            RoommateButton.OnButtonClick += (UIElement btn) => { if (OnLotClick != null) OnLotClick(btn); };
            VisitorButton.OnButtonClick += (UIElement btn) => { if (OnLotClick != null) OnLotClick(btn); };

            Thumbnail = new UIImage();
            Thumbnail.X = 4;
            Add(Thumbnail);

            Mode = UILotRelationship.VISITOR;
        }

        public void SetThumbnail(Texture2D thumbnail, uint lot)
        {
            if (Thumbnail.Texture != thumbnail && CurrentLotThumb != 0) Thumbnail.Texture.Dispose(); 
            Thumbnail.Texture = thumbnail;
            Thumbnail.SetSize((thumbnail.Width > 144) ? thumbnail.Width / 2 : thumbnail.Width, (thumbnail.Height > 144) ? thumbnail.Height / 2 : thumbnail.Height);
            Thumbnail.Y = (95 - Thumbnail.Height) / 2.0f;
            Thumbnail.X = (4+128 - Thumbnail.Width) / 2.0f;
            CurrentLotThumb = lot;
        }

        public UILotRelationship Mode
        {
            get { return _Mode; }
            set
            {
                _Mode = value;

                RoommateButton.Visible = _Mode == UILotRelationship.OWNER || _Mode == UILotRelationship.ROOMMATE;
                VisitorButton.Visible = !RoommateButton.Visible;
            }
        }
    }

    public enum UILotRelationship
    {
        OWNER,
        ROOMMATE,
        VISITOR
    }
}
