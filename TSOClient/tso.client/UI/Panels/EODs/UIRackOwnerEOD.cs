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
    public class UIRackOwnerEOD : UIAbstractRackEOD
    {
        private UICollectionViewer OutfitBrowserOwner;

        public UIImage OwnerBackground;

        public UIButton btnStock { get; set; }
        public UIButton btnDelete { get; set; }
        public UIButton btnMale { get; set; }
        public UIButton btnFemale { get; set; }


        private RackOutfitGender SelectedGender;

        private Dictionary<uint, int> DirtyPrices = new Dictionary<uint, int>();
        private GameThreadTimeout DirtyTimeout;

        public UIRackOwnerEOD(UIEODController controller) : base(controller, "rackownereod.uis")
        {
        }

        
        protected override void InitUI(){
            base.InitUI();

            OwnerBackground = Script.Create<UIImage>("controlBackgroundOwner");
            AddAt(0, OwnerBackground);

            OutfitBrowserOwner = Script.Create<UICollectionViewer>("OutfitBrowserOwner");
            OutfitBrowserOwner.OnChange += x => UpdateUIState();
            OutfitBrowserOwner.PaginationHeight = 15;
            OutfitBrowserOwner.PaginationHeightDeduction = 0;
            OutfitBrowserOwner.Init();
            Add(OutfitBrowserOwner);

            btnMale.OnButtonClick += ToggleGender;
            btnFemale.OnButtonClick += ToggleGender;
            btnStock.OnButtonClick += BtnStock_OnButtonClick;
            btnDelete.OnButtonClick += BtnDelete_OnButtonClick;

            RackName.OnChange += Name_OnChange;

            foreach (var price in OutfitPrices){
                price.OnChange += Price_OnChange;
            }
        }

        protected override void RackNameHandler(string evt, string rackName)
        {
            base.RackNameHandler(evt, rackName);
            RackName.Mode = UITextEditMode.Editor;
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

        private void Name_OnChange(UIElement elemnt)
        {
            // change the name of the rack by verifying the data then sending a message to the server
            var validatedRackName = RackName.CurrentText;
            if ((validatedRackName.Length > 0) && (validatedRackName.Length < 33))
                Send("rackowner_update_name", validatedRackName);
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
            var selectedOutfit = GetSelectedOutfit();
            if (selectedOutfit == null) { return; }

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
            var selectedOutfit = GetSelectedOwnerOutfit();
            if (selectedOutfit == null) { return; }

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
            UpdateUIState();
            SetOwnerOutfits();
        }


        /**
         * Owner outfit grid
         */
        protected override void SetRackType(RackType type)
        {
            base.SetRackType(type);
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
            SetOwnerOutfits();
        }

        private void SetOwnerOutfits()
        {
            var outfits = Content.Content.Get().RackOutfits.GetByRackType(RackType);
            OutfitBrowserOwner.DataProvider = RackOutfitsToDataProvider(outfits);
            OutfitBrowserOwner.SelectedIndex = 0;
        }

        private List<object> RackOutfitsToDataProvider(RackOutfits outfits)
        {
            var appearanceType = GetAppearanceType();
            var dataProvider = new List<object>();
            foreach (var outfit in outfits.Outfits)
            {
                if (outfit.Gender != SelectedGender) { continue; }

                Outfit TmpOutfit = Content.Content.Get().AvatarOutfits.Get(outfit.AssetID);
                if (TmpOutfit == null) continue;
                Appearance TmpAppearance = Content.Content.Get().AvatarAppearances.Get(TmpOutfit.GetAppearance(appearanceType));
                FSO.Common.Content.ContentID thumbID = TmpAppearance.ThumbnailID;
                
                dataProvider.Add(new UIGridViewerItem
                {
                    Data = outfit,
                    Thumb = new Promise<Texture2D>(x => Content.Content.Get().AvatarThumbnails.Get(thumbID).Get(GameFacade.GraphicsDevice))
                });
            }
            return dataProvider;
        }



        /**
         * UI management
         */


        protected override void UpdateUIState()
        {
            base.UpdateUIState();

            var selectedOwnerOutfit = GetSelectedOwnerOutfit();
            var selectedOutfit = GetSelectedOutfit();
            
            //Stock button + tips
            if (IsFull)
            {
                SetTip(GameFacade.Strings.GetString("265", "12"));
                btnStock.Disabled = true;
            }
            else if(selectedOwnerOutfit != null)
            {
                if (LotController != null && selectedOwnerOutfit.Price > LotController.ActiveEntity.TSOState.Budget.Value){
                    //Cant afford it
                    SetTip(GameFacade.Strings.GetString("265", "13"));
                    btnStock.Disabled = true;
                }
                else
                {
                    //Can afford it
                    SetTip(GameFacade.Strings.GetString("265", "14") + selectedOwnerOutfit.Price);
                    btnStock.Disabled = false;
                }
            }else{
                SetTip("");
                btnStock.Disabled = false;
            }

            //Delete button
            btnDelete.Disabled = selectedOutfit == null;

            //Gender buttons
            if (SelectedGender == RackOutfitGender.Neutral){
                btnMale.Disabled = true;
                btnFemale.Disabled = true;
            }else{
                btnMale.Selected = SelectedGender == RackOutfitGender.Male;
                btnFemale.Selected = SelectedGender == RackOutfitGender.Female;
            }
        }

        private RackOutfit GetSelectedOwnerOutfit()
        {
            var selecteOutfit = OutfitBrowserOwner.SelectedItem as UIGridViewerItem;
            if(selecteOutfit != null)
            {
                return (RackOutfit)selecteOutfit.Data;
            }
            return null;
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

        protected override EODLiveModeOpt GetEODOptions()
        {
            return new EODLiveModeOpt
            {
                Buttons = 1,
                Expandable = true,
                Expanded = true,
                Height = EODHeight.TallTall,
                Length = EODLength.Full,
                Timer = EODTimer.None,
                Tips = EODTextTips.Short,
                TopPanelLength = EODLength.Full,
                TopPanelButtons = 1
            };
        }


        
    }
}
