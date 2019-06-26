using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework.Parser;
using Microsoft.Xna.Framework.Graphics;
using FSO.SimAntics.Engine.Scopes;
using FSO.Common.Serialization;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.Engine.TSOGlobalLink.Model;
using FSO.Vitaboy;
using FSO.Common.Utils;
using FSO.SimAntics;
using FSO.Client.UI.Framework;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIDresserEOD : UIEOD
    {
        protected UIScript Script;
        
        /** Script ui **/
        public Texture2D imageBackgroundDay { get; set; }
        public Texture2D imageBackgroundSleep { get; set; }
        public Texture2D imageBackgroundSwim { get; set; }

        public UIButton btnDay { get; set; }
        public UIButton btnSleep { get; set; }
        public UIButton btnSwim { get; set; }
        public UIButton btnDecorHead { get; set; }
        public UIButton btnDecorBack { get; set; }
        public UIButton btnDecorShoes { get; set; }
        public UIButton btnDecorTail { get; set; }
        public UIButton btnDelete { get; set; }
        public UIButton btnAccept { get; set; }

        public UIRadioButton btnDefault1 { get; set; }
        public UIRadioButton btnDefault2 { get; set; }
        public UIRadioButton btnDefault3 { get; set; }
        public UIRadioButton btnDefault4 { get; set; }
        public UIRadioButton btnDefault5 { get; set; }

        private UIImage Background;
        private VMPersonSuits SelectedTab = VMPersonSuits.DefaultDaywear;

        protected UICollectionViewer OutfitBrowser;
        private VMGLOutfit[] Outfits;

        public UIDresserEOD(UIEODController controller) : base(controller)
        {
            InitUI();
            InitEOD();
        }

        private void InitUI()
        {
            Script = this.RenderScript("dressereod.uis");
            Background = Script.Create<UIImage>("controlBackgroundPos");
            Background.Texture = imageBackgroundDay;
            AddAt(0, Background);

            btnDay.OnButtonClick += SetTab;
            btnSleep.OnButtonClick += SetTab;
            btnSwim.OnButtonClick += SetTab;
            btnDecorBack.OnButtonClick += SetTab;
            btnDecorHead.OnButtonClick += SetTab;
            btnDecorShoes.OnButtonClick += SetTab;
            btnDecorTail.OnButtonClick += SetTab;

            OutfitBrowser = Script.Create<UICollectionViewer>("OutfitBrowser");
            OutfitBrowser.PaginationStyle = UIPaginationStyle.NONE;
            OutfitBrowser.Init();
            OutfitBrowser.OnSelectedPageChanged += x => UpdateUIState();
            OutfitBrowser.OnChange += x => UpdateUIState();
            Add(OutfitBrowser);

            btnAccept.OnButtonClick += BtnAccept_OnButtonClick;
            btnDelete.OnButtonClick += BtnDelete_OnButtonClick;

            var defaultButtons = DefaultButtons;
            for(var i=0; i < defaultButtons.Length; i++){
                defaultButtons[i].RadioData = i;
                defaultButtons[i].RadioGroup = "DresserDefault";
                defaultButtons[i].OnButtonClick += DefaultRadio_OnButtonClick;
            }
        }

        /**
         * EOD callbacks
         */

        protected void InitEOD()
        {
            PlaintextHandlers["dresser_show"] = ShowEOD;
            PlaintextHandlers["dresser_refresh_default"] = (evt, body) => UpdateUIState();
            BinaryHandlers["set_outfits"] = SetOutfits;
        }

        protected virtual void SetOutfits(string evt, byte[] body)
        {
            var packet = IoBufferUtils.Deserialize<VMEODRackStockResponse>(body, null);
            Outfits = packet.Outfits;
            UpdateDataProvider();
            UpdateUIState();
        }

        protected virtual void ShowEOD(string evt, string txt)
        {
            var options = GetEODOptions();
            UpdateUIState();
            EODController.ShowEODMode(options);
        }

        /**
         * UI Events
         */
        
        private void BtnDelete_OnButtonClick(Framework.UIElement button)
        {
            if (OutfitBrowser.DataProvider == null) { return; }

            var index = OutfitBrowser.SelectedIndex;
            if (index >= 0 && index < OutfitBrowser.DataProvider.Count)
            {
                var outfit = (VMGLOutfit)((UIGridViewerItem)OutfitBrowser.DataProvider[index]).Data;
                if (outfit == null) { return; }

                UIAlert alert = null;
                alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Title = GameFacade.Strings.GetString("266", "7"),
                    Message = GameFacade.Strings.GetString("266", "8"),
                    Buttons = UIAlertButton.YesNo(
                        yes => {
                            Send("dresser_delete_outfit", outfit.outfit_id.ToString());
                            UIScreen.RemoveDialog(alert);
                        },
                        no => {
                            UIScreen.RemoveDialog(alert);
                        }
                    ),
                    Alignment = TextAlignment.Left
                }, true);
            }
        }

        private void DefaultRadio_OnButtonClick(Framework.UIElement button)
        {
            if (OutfitBrowser.DataProvider == null) { return; }

            var index = (int)((UIRadioButton)button).RadioData;
            if (index >= 0 && index < OutfitBrowser.DataProvider.Count)
            {
                var outfit = (VMGLOutfit)((UIGridViewerItem)OutfitBrowser.DataProvider[index]).Data;
                if (outfit == null) { return; }

                Send("dresser_set_default", ((short)SelectedTab).ToString() + "," + outfit.outfit_id.ToString());
            }
        }

        private void BtnAccept_OnButtonClick(Framework.UIElement button)
        {
            var outfit = GetSelectedOutfit();
            if (outfit == null) { return; }
            Send("dresser_change_outfit", outfit.outfit_id.ToString());
        }

        private void SetTab(Framework.UIElement button)
        {
            if (button == btnDay)
            {
                SelectedTab = VMPersonSuits.DefaultDaywear;
            }
            else if (button == btnSleep)
            {
                SelectedTab = VMPersonSuits.DefaultSleepwear;
            }
            else if (button == btnSwim)
            {
                SelectedTab = VMPersonSuits.DefaultSwimwear;
            }
            else if (button == btnDecorHead)
            {
                SelectedTab = VMPersonSuits.DecorationHead;
            }
            else if (button == btnDecorBack)
            {
                SelectedTab = VMPersonSuits.DecorationBack;
            }
            else if (button == btnDecorShoes)
            {
                SelectedTab = VMPersonSuits.DecorationShoes;
            }
            else if (button == btnDecorTail)
            {
                SelectedTab = VMPersonSuits.DecorationTail;
            }

            UpdateDataProvider();
            UpdateUIState();
        }


        /**
         * UI Utils
         */

        private void UpdateUIState()
        {
            var selected = GetSelectedOutfit() != null;
            btnAccept.Disabled = !selected;
            btnDelete.Disabled = !selected;

            var canSetDefaults = CanSetDefaults();
            var numOutfits = OutfitBrowser.DataProvider != null ? OutfitBrowser.DataProvider.Count : 0;
            var selectedDefault = GetDefaultOutfitIndex();

            if (!VMPersonSuitsUtils.IsDecoration(SelectedTab)){
                if(numOutfits <= 1){
                    //Must leave at least one outfit
                    btnDelete.Disabled = true;
                }
            }

            for (var i=0; i < DefaultButtons.Length; i++)
            {
                var defaultButton = DefaultButtons[i];
                if (canSetDefaults){
                    defaultButton.Visible = true;
                    defaultButton.Selected = selectedDefault == i;
                    defaultButton.Disabled = i >= numOutfits;
                }
                else{
                    defaultButton.Visible = false;
                }
            }
        }

        private void UpdateDataProvider()
        {
            if (Outfits == null) return;
            var dataProvider = new List<object>();
            var outfitsInCategory = Outfits.Where(x => x.outfit_type == (byte)SelectedTab).ToList();
            var appearanceType = GetAppearanceType();

            foreach (var outfit in outfitsInCategory)
            {
                //TODO: Use current avatars appearance type
                Outfit TmpOutfit = Content.Content.Get().AvatarOutfits.Get(outfit.asset_id);
                Appearance TmpAppearance = Content.Content.Get().AvatarAppearances.Get(TmpOutfit.GetAppearance(appearanceType));
                FSO.Common.Content.ContentID thumbID = TmpAppearance.ThumbnailID;

                dataProvider.Add(new UIGridViewerItem
                {
                    Data = outfit,
                    Thumb = new Promise<Texture2D>(x => Content.Content.Get().AvatarThumbnails.Get(thumbID).Get(GameFacade.GraphicsDevice))
                });
            }
            OutfitBrowser.DataProvider = dataProvider;
        }


        private int GetDefaultOutfitIndex()
        {
            if (OutfitBrowser.DataProvider == null) {
                return -1;
            }

            var defaultOutfitForCategory = GetDefaultOutfit();
            var item = OutfitBrowser.DataProvider.FirstOrDefault(x =>
            {
                var outfit = (VMGLOutfit)((UIGridViewerItem)x).Data;
                return outfit.asset_id == defaultOutfitForCategory;
            });

            if (item == null) { return -1; }
            return OutfitBrowser.DataProvider.IndexOf(item);
        }

        public AppearanceType GetAppearanceType()
        {
            if (LotController != null && LotController.ActiveEntity is VMAvatar)
            {
                var avatar = (VMAvatar)LotController.ActiveEntity;
                return avatar.Avatar.Appearance;
            }
            return AppearanceType.Light;
        }

        private ulong GetDefaultOutfit()
        {
            ulong outfit = 0;
            if (LotController != null && LotController.ActiveEntity is VMAvatar)
            {
                var avatar = (VMAvatar)LotController.ActiveEntity;
                switch (SelectedTab)
                {
                    case VMPersonSuits.DefaultDaywear:
                        outfit = avatar.DefaultSuits.Daywear.ID;
                        break;
                    case VMPersonSuits.DefaultSleepwear:
                        outfit = avatar.DefaultSuits.Sleepwear.ID;
                        break;
                    case VMPersonSuits.DefaultSwimwear:
                        outfit = avatar.DefaultSuits.Swimwear.ID;
                        break;
                }
            }

            return outfit;
        }

        protected EODLiveModeOpt GetEODOptions()
        {
            return new EODLiveModeOpt
            {
                Buttons = 2,
                Expandable = false,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Timer = EODTimer.None,
                Tips = EODTextTips.None
            };
        }

        public override void OnClose()
        {
            Send("close", "");
            base.OnClose();
        }

        public VMGLOutfit GetSelectedOutfit()
        {
            var selectedItem = OutfitBrowser.SelectedItem as UIGridViewerItem;
            if (selectedItem != null)
            {
                return (VMGLOutfit)selectedItem.Data;
            }
            return null;
        }

        private bool CanSetDefaults()
        {
            if (SelectedTab == VMPersonSuits.DecorationBack ||
                SelectedTab == VMPersonSuits.DecorationHead ||
                SelectedTab == VMPersonSuits.DecorationShoes ||
                SelectedTab == VMPersonSuits.DecorationTail)
            {
                return false;
            }
            return true;
        }

        public UIRadioButton[] DefaultButtons
        {
            get
            {
                return new UIRadioButton[] {
                    btnDefault1,
                    btnDefault2,
                    btnDefault3,
                    btnDefault4,
                    btnDefault5
                };
            }
        }
    }
    
}
