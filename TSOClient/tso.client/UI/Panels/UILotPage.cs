using FSO.Client.Controllers;
using FSO.Client.Controllers.Panels;
using FSO.Client.Model;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using FSO.Server.Database.DA.Lots;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
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
        private UIPersonButton OwnerButton { get; set; }

        public Binding<Lot> CurrentLot;

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
            LotThumbnail.SetThumbnail(
                TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, GameFacade.GameFilePath("userdata/houses/defaulthouse.bmp"))
            );
            Add(LotThumbnail);

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

            LotThumbnail.OnLotClick += JoinLot;

            CurrentLot = new Binding<Lot>()
                .WithBinding(HouseNameButton, "Caption", "Lot_Name")
                .WithBinding(HouseDescriptionTextEdit, "CurrentText", "Lot_Description")
                .WithBinding(HouseValueLabel, "Caption", "Lot_Price", x => MoneyFormatter.Format((uint)x))
                .WithBinding(OccupantsNumberLabel, "Caption", "Lot_NumOccupants", x => x.ToString())
                .WithBinding(OwnerButton, "AvatarId", "Lot_LeaderID")
                .WithBinding(HouseCategoryButton, "Texture", "Lot_Category", x =>
                {
                    var category = (DbLotCategory)Enum.Parse(typeof(DbLotCategory), x.ToString());
                    switch (category)
                    {
                        case DbLotCategory.none:
                            return HouseCategory_NoCategoryButtonImage;
                        case DbLotCategory.welcome:
                            return HouseCategory_WelcomeButtonImage;
                        case DbLotCategory.money:
                            return HouseCategory_MoneyButtonImage;
                        case DbLotCategory.entertainment:
                            return HouseCategory_EntertainmentButtonImage;
                        case DbLotCategory.games:
                            return HouseCategory_GamesButtonImage;
                        case DbLotCategory.offbeat:
                            return HouseCategory_OffbeatButtonImage;
                        case DbLotCategory.residence:
                            return HouseCategory_ResidenceButtonImage;
                        case DbLotCategory.romance:
                            return HouseCategory_RomanceButtonImage;
                        case DbLotCategory.services:
                            return HouseCategory_ServicesButtonImage;
                        case DbLotCategory.shopping:
                            return HouseCategory_ShoppingButtonImage;
                        case DbLotCategory.skills:
                            return HouseCategory_SkillsButtonImage;
                        default:
                            return null;
                    }
                })
                .WithMultiBinding(x => RefreshUI(), "Lot_LeaderID", "Lot_IsOnline");

            RefreshUI();
            NeighborhoodNameButton.Visible = false;
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
                isOnline = CurrentLot.Value.Lot_IsOnline;
                isMyProperty = FindController<CoreGameScreenController>().IsMe(CurrentLot.Value.Lot_LeaderID);
            }

            var canJoin = isMyProperty || isRoommate || isOnline;

            BackgroundContractedImage.Visible = isClosed;
            BackgroundExpandedImage.Visible = isOpen;

            ExpandButton.Visible = isClosed;
            ExpandedCloseButton.Visible = isOpen;

            ContractButton.Visible = isOpen;
            ContractedCloseButton.Visible = isClosed;

            BackgroundDescriptionImage.Visible = isOpen && !isMyProperty;
            BackgroundDescriptionEditImage.Visible = isOpen && isMyProperty;

            HouseDescriptionSlider.Visible =
                HouseDescriptionTextEdit.Visible =
                HouseDescriptionScrollUpButton.Visible =
                HouseDescriptionScrollDownButton.Visible = isOpen;

            VisitorsLeftScrollButton.Visible = VisitorsRightScrollButton.Visible = isOpen;

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

    public class UILotThumbButton : UIContainer
    {
        private UIButton RoommateButton;
        private UIButton VisitorButton;
        private UIImage Thumbnail;
        public ButtonClickDelegate OnLotClick;

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

        public void SetThumbnail(Texture2D thumbnail)
        {
            TextureUtils.ManualTextureMask(ref thumbnail, new uint[] { 0xFF000000 });
            Thumbnail.Texture = thumbnail;
            Thumbnail.Y = (VisitorButton.Size.Y - thumbnail.Height) / 2.0f;
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
