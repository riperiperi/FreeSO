using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Common.Serialization;
using FSO.Common.Utils;
using FSO.Content.Model;
using FSO.SimAntics.Engine.TSOGlobalLink.Model;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.Vitaboy;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIRackOwnerEOD : UIEOD
    {

        private UIScript Script;
        private RackType RackType;
        private UICollectionViewer OutfitBrowserOwner;
        private UICollectionViewer OutfitBrowser;

        public UIImage OwnerBackground;

        public UIButton btnStock { get; set; }
        public UIButton btnDelete { get; set; }
        public UIButton btnMale { get; set; }
        public UIButton btnFemale { get; set; }

        public UITextEdit Outfit1Price { get; set; }
        public UITextEdit Outfit2Price { get; set; }
        public UITextEdit Outfit3Price { get; set; }
        public UITextEdit Outfit4Price { get; set; }
        public UITextEdit Outfit5Price { get; set; }
        public UITextEdit Outfit6Price { get; set; }
        public UITextEdit Outfit7Price { get; set; }
        public UITextEdit Outfit8Price { get; set; }
        private UITextEdit[] OutfitPrices;

        private RackOutfitGender SelectedGender;
        private VMGLOutfit[] Stock;

        private Dictionary<uint, int> DirtyPrices = new Dictionary<uint, int>();
        private GameThreadTimeout DirtyTimeout;

        public UIRackOwnerEOD(UIEODController controller) : base(controller)
        {
            InitUI();
            InitEOD();
        }

        private void InitUI(){
            Script = this.RenderScript("rackownereod.uis");

            OwnerBackground = Script.Create<UIImage>("controlBackgroundOwner");
            AddAt(0, OwnerBackground);
            AddAt(0, Script.Create<UIImage>("controlBackground"));

            OutfitBrowserOwner = Script.Create<UICollectionViewer>("OutfitBrowserOwner");
            OutfitBrowserOwner.OnChange += UpdateState;
            OutfitBrowserOwner.PaginationHeight = 15;
            OutfitBrowserOwner.PaginationHeightDeduction = 0;
            OutfitBrowserOwner.Init();
            Add(OutfitBrowserOwner);

            OutfitBrowser = Script.Create<UICollectionViewer>("OutfitBrowser");
            OutfitBrowser.PaginationStyle = UIPaginationStyle.LEFT_RIGHT_ARROWS;
            OutfitBrowser.Init();
            OutfitBrowser.OnSelectedPageChanged += x => UpdateState();
            OutfitBrowser.OnChange += x => UpdateState();
            Add(OutfitBrowser);

            btnMale.OnButtonClick += ToggleGender;
            btnFemale.OnButtonClick += ToggleGender;
            btnStock.OnButtonClick += BtnStock_OnButtonClick;
            btnDelete.OnButtonClick += BtnDelete_OnButtonClick;

            OutfitPrices = new UITextEdit[] { Outfit1Price, Outfit2Price, Outfit3Price, Outfit4Price, Outfit5Price, Outfit6Price, Outfit7Price, Outfit8Price };

            foreach(var price in OutfitPrices){
                price.OnChange += Price_OnChange;
            }
        }

        private void Price_OnChange(UIElement element)
        {
            var input = (UITextEdit)element;
            var price = input.CurrentText;

            var isValid = VMEODRackOwnerPlugin.PRICE_VALIDATION.IsMatch(price);
            if (!isValid) {
                //TODO: Text box does not seem to like been updated in the update event loop, fix this
                return;
                /*for (var i = 0; i < price.Length; i++) {
                    if (!Char.IsDigit(price[i])) {
                        price = price.Substring(i, 1);
                        i--;
                    }
                }

                if (price.Length == 0) { price = "1"; }
                if (price[0] == '0') {
                    price = "1" + price.Substring(1);
                }

                input.CurrentText = price;*/
            }

            //Mark as dirty
            var newSalePrice = -1;
            if (!int.TryParse(price, out newSalePrice)) { return; }

            var buttonIndex = Array.IndexOf(OutfitPrices, input);
            if (buttonIndex == -1) { return; }

            var priceIndex = GetPriceIndex(buttonIndex);
            if (priceIndex == -1) { return; }

            var outfit = Stock[priceIndex];

            //Which outfit is this for?
            lock (DirtyPrices)
            {
                DirtyPrices[outfit.outfit_id] = newSalePrice;
            }

            if(DirtyTimeout != null){
                DirtyTimeout.Clear();
            }

            DirtyTimeout = GameThread.SetTimeout(() => FlushDirtyPrices(), 2000);
        }

        private void FlushDirtyPrices()
        {
            lock (DirtyPrices){
                foreach (var key in DirtyPrices.Keys){
                    var price = DirtyPrices[key];
                    Send("rackowner_update_price", key + "," + price);
                }
                DirtyPrices.Clear();
            }
        }

        private void BtnDelete_OnButtonClick(UIElement button)
        {
            var selecteGridItem = OutfitBrowser.SelectedItem as UIGridViewerItem;
            if (selecteGridItem == null) { return; }

            var selectedOutfit = (VMGLOutfit)selecteGridItem.Data;
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = GameFacade.Strings.GetString("265", "9"),
                Message = GameFacade.Strings.GetString("265", "10"),
                Buttons = UIAlertButton.YesNo(yes => {
                    Send("rackowner_delete", selectedOutfit.outfit_id.ToString());
                    UIScreen.RemoveDialog(alert);
                }, no => { UIScreen.RemoveDialog(alert); }),
                Alignment = TextAlignment.Left
            }, true);
        }

        /// <summary>
        /// Stock the selected outfit
        /// </summary>
        /// <param name="button"></param>
        private void BtnStock_OnButtonClick(Framework.UIElement button)
        {
            var selecteGridItem = OutfitBrowserOwner.SelectedItem as UIGridViewerItem;
            if (selecteGridItem == null) { return; }

            var selectedOutfit = (RackOutfit)selecteGridItem.Data;
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = GameFacade.Strings.GetString("265", "7"),
                Message = GameFacade.Strings.GetString("265", "8"),
                Buttons = UIAlertButton.YesNo(yes => {
                    Send("rackowner_stock", selectedOutfit.AssetID.ToString());
                    UIScreen.RemoveDialog(alert);
                }, no => { UIScreen.RemoveDialog(alert); }),
                Alignment = TextAlignment.Left
            }, true);
        }

        private void ToggleGender(Framework.UIElement button)
        {
            if(button == btnMale){
                SelectedGender = RackOutfitGender.Male;
            }else if(button == btnFemale){
                SelectedGender = RackOutfitGender.Female;
            }
            UpdateState();
            SetOutfits();
        }

        private void UpdateState()
        {
            if(SelectedGender == RackOutfitGender.Neutral){
                btnMale.Disabled = true;
                btnFemale.Disabled = true;
            }else{
                btnMale.Selected = SelectedGender == RackOutfitGender.Male;
                btnFemale.Selected = SelectedGender == RackOutfitGender.Female;
            }

            btnStock.Disabled = OutfitBrowserOwner.SelectedItem == null;

            var selecteOutfitd = OutfitBrowserOwner.SelectedItem as UIGridViewerItem;
            if (selecteOutfitd == null){
                SetTip("");
            }else{
                var rackOutfit = (RackOutfit)selecteOutfitd.Data;

                if (LotController != null && rackOutfit.Price > LotController.ActiveEntity.TSOState.Budget.Value){
                    SetTip(GameFacade.Strings.GetString("265", "13"));
                    btnStock.Disabled = true;
                }
                else{
                    SetTip(GameFacade.Strings.GetString("265", "14") + rackOutfit.Price);
                }
            }

            if (Stock != null && Stock.Length >= VMEODRackOwnerPlugin.MAX_OUTFITS)
            {
                btnStock.Disabled = true;
                SetTip(GameFacade.Strings.GetString("265", "12"));
            }

            btnDelete.Disabled = OutfitBrowser.SelectedItem == null;

            //Align pricing with scroll position

            for(var i=0; i < 8; i++){
                var priceField = OutfitPrices[i];
                var priceIndex = GetPriceIndex(i);

                if (priceIndex != -1)
                {
                    priceField.CurrentText = Stock[priceIndex].sale_price.ToString();
                    priceField.Mode = UITextEditMode.Editor;
                }
                else{
                    priceField.CurrentText = "";
                    priceField.Mode = UITextEditMode.ReadOnly;
                }
            }

            //OutfitPrices
        }

        private int GetPriceIndex(int i)
        {
            var offset = OutfitBrowser.SelectedPage * OutfitBrowser.ItemsPerPage;
            var stockLength = Stock != null ? Stock.Length : 0;

            if(offset + i < stockLength)
            {
                return offset + i;
            }
            else
            {
                return -1;
            }
        }

        private void UpdateState(Framework.UIElement element)
        {
            UpdateState();
        }

        private void InitEOD()
        {
            PlaintextHandlers["rackowner_show"] = Show;
            BinaryHandlers["rackowner_browse"] = Browse;
        }

        public void Browse(string evt, byte[] body)
        {
            var packet = IoBufferUtils.Deserialize<VMEODRackOwnerBrowseResponse>(body, null);
            Stock = packet.Outfits;

            var dataProvider = new List<object>();

            foreach (var outfit in Stock)
            {
                Outfit TmpOutfit = Content.Content.Get().AvatarOutfits.Get(RackOutfit.GetOutfitID(outfit.asset_id));
                Appearance TmpAppearance = Content.Content.Get().AvatarAppearances.Get(TmpOutfit.GetAppearance(AppearanceType.Light));
                FSO.Common.Content.ContentID thumbID = TmpAppearance.ThumbnailID;

                dataProvider.Add(new UIGridViewerItem
                {
                    Data = outfit,
                    Thumb = new Promise<Texture2D>(x => Content.Content.Get().AvatarThumbnails.Get(thumbID).Get(GameFacade.GraphicsDevice))
                });
            }
            OutfitBrowser.DataProvider = dataProvider;
            UpdateState();
        }

        public void Show(string evt, string txt)
        {
            RackType = (RackType)short.Parse(txt);
            switch (RackType)
            {
                case RackType.Decor_Back:
                case RackType.Decor_Head:
                case RackType.Decor_Shoe:
                case RackType.Decor_Tail:
                    SelectedGender = RackOutfitGender.Neutral;
                    break;
                default:
                    SelectedGender = RackOutfitGender.Male;
                    break;
            }
            SetOutfits();
            UpdateState();

            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 1,
                Expandable = true,
                Height = EODHeight.TallTall,
                Length = EODLength.Full,
                Timer = EODTimer.None,
                Tips = EODTextTips.Short,
                TopPanelLength = EODLength.Full,
                TopPanelButtons = 1
            });
        }

        private void SetOutfits()
        {
            var outfits = Content.Content.Get().RackOutfits.GetByRackType(RackType);
            OutfitBrowserOwner.DataProvider = RackOutfitsToDataProvider(outfits);
            OutfitBrowserOwner.SelectedIndex = 0;
        }

        private List<object> RackOutfitsToDataProvider(RackOutfits outfits)
        {
            var dataProvider = new List<object>();
            foreach (var outfit in outfits.Outfits)
            {
                if (outfit.Gender != SelectedGender) { continue; }

                Outfit TmpOutfit = Content.Content.Get().AvatarOutfits.Get(outfit.GetOutfitID());
                Appearance TmpAppearance = Content.Content.Get().AvatarAppearances.Get(TmpOutfit.GetAppearance(AppearanceType.Light));
                FSO.Common.Content.ContentID thumbID = TmpAppearance.ThumbnailID;
                
                dataProvider.Add(new UIGridViewerItem
                {
                    Data = outfit,
                    Thumb = new Promise<Texture2D>(x => Content.Content.Get().AvatarThumbnails.Get(thumbID).Get(GameFacade.GraphicsDevice))
                });
            }
            return dataProvider;
        }

        public override void OnClose()
        {
            Send("close", "");
            base.OnClose();
        }

        private void SetExpanded(bool expanded)
        {
            OutfitBrowserOwner.Visible = expanded;
            OwnerBackground.Visible = expanded;
            btnStock.Visible = expanded;
            btnMale.Visible = expanded;
            btnFemale.Visible = expanded;
        }

        public override void OnExpand()
        {
            SetExpanded(true);
        }

        public override void OnContract()
        {
            SetExpanded(false);
        }
    }
}
