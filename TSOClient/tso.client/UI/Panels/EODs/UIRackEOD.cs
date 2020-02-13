using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIRackEOD : UIAbstractRackEOD
    {
        public UIButton btnTryOn { get; set; }
        public UIButton btnPurchase { get; set; }

        public UIRackEOD(UIEODController controller) : base(controller, "rackeod.uis")
        {
        }

        protected override void InitUI()
        {
            base.InitUI();

            btnTryOn.OnButtonClick += BtnTryOn_OnButtonClick;
            btnPurchase.OnButtonClick += BtnPurchase_OnButtonClick;
        }

        protected override void InitEOD()
        {
            base.InitEOD();
            BinaryHandlers["rack_buy_error"] = PurchaseErrorHandler;
        }

        private void PurchaseErrorHandler(string name, byte[] type)
        {
            string title = GameFacade.Strings.GetString("264", "5"); // "Purchase Item"
            String message = "";
            // I already own this outfit - should do a custom error message but that means a new .cst entry...
            if (type[0] == 0)
            {
                message = GameFacade.Strings.GetString("264", "10"); // "You have no more room in your Backpack"
            }
            else if (type[0] == 1)
            // I already own 5 outfits of this type
            {
                message = GameFacade.Strings.GetString("264", "10"); // "You have no more room in your Backpack"
            }
            // An unknown purchase error occured, probably not enough money suddenly
            else
            {
                message = GameFacade.Strings.GetString("264", "9"); // "You can't afford that Item."
            }
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = title,
                Message = message,
                Buttons = UIAlertButton.Ok((btn) =>
                {
                    UIScreen.RemoveDialog(alert);
                }),
                Alignment = TextAlignment.Center
            }, true);
        }

        private void BtnPurchase_OnButtonClick(Framework.UIElement button)
        {
            var selectedOutfit = GetSelectedOutfit();
            if (selectedOutfit == null) { return; }

            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = GameFacade.Strings.GetString("264", "5"),
                Message = GameFacade.Strings.GetString("264", "6"),
                Buttons = UIAlertButton.YesNoCancel(
                    yes => {
                        Send("rack_purchase", selectedOutfit.outfit_id.ToString() + ",true");
                        UIScreen.RemoveDialog(alert);
                    }, 
                    no => {
                        Send("rack_purchase", selectedOutfit.outfit_id.ToString() + ",false");
                        UIScreen.RemoveDialog(alert);
                    }, 
                    cancel => { UIScreen.RemoveDialog(alert); }
                ),
                Alignment = TextAlignment.Left
            }, true);

        }

        private void BtnTryOn_OnButtonClick(Framework.UIElement button)
        {
            var selectedOutfit = GetSelectedOutfit();
            if (selectedOutfit == null) { return; }

            Send("rack_try_outfit_on", selectedOutfit.outfit_id.ToString());
        }

        protected override void UpdateUIState()
        {
            base.UpdateUIState();

            // customers need not edit the rack name
            RackName.Mode = UITextEditMode.ReadOnly;

            var selected = GetSelectedOutfit();
            if (selected == null)
            {
                //Browsing the rack
                SetTip(GameFacade.Strings.GetString("264", "7"));
                btnTryOn.Disabled = true;
                btnPurchase.Disabled = true;

                for (var i = 0; i < 8; i++)
                {
                    var priceField = OutfitPrices[i];
                    // customers need not edit the displayed prices in their own UI
                    if (priceField != null)
                    {
                        priceField.Mode = UITextEditMode.ReadOnly;
                    }
                }
            }
            else
            {
                var isMyGender = false;
                var canAfford = false;

                if (LotController != null && LotController.ActiveEntity is VMAvatar)
                {
                    var avatar = (VMAvatar)LotController.ActiveEntity;
                    bool male = (avatar.GetPersonData(VMPersonDataVariable.Gender) == 0);

                    //Is it for my gender?
                    var outfit = Content.Content.Get().RackOutfits.GetAllOutfits().FirstOrDefault(x => x.AssetID == selected.asset_id);
                    if(outfit != null){
                        if(outfit.Gender == Content.Model.RackOutfitGender.Neutral){
                            isMyGender = true;
                        }else if(outfit.Gender == Content.Model.RackOutfitGender.Male && male){
                            isMyGender = true;
                        }else if(outfit.Gender == Content.Model.RackOutfitGender.Female && !male){
                            isMyGender = true;
                        }
                    }

                    //Can I afford it?
                    if(selected.sale_price <= LotController.ActiveEntity.TSOState.Budget.Value){
                        //Can't afford it
                        canAfford = true;
                    }
                }

                btnTryOn.Disabled = !isMyGender;
                btnPurchase.Disabled = !isMyGender || !canAfford;

                if (!isMyGender)
                {
                    SetTip(GameFacade.Strings.GetString("264", "8"));
                }else if (!canAfford){
                    SetTip(GameFacade.Strings.GetString("264", "9"));
                }else{
                    SetTip(GameFacade.Strings.GetString("264", "7"));
                }

                RackName.Mode = UITextEditMode.ReadOnly;
            }
        }

        protected override EODLiveModeOpt GetEODOptions()
        {
            return new EODLiveModeOpt
            {
                Buttons = 2,
                Expandable = false,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Timer = EODTimer.None,
                Tips = EODTextTips.Short
            };
        }
    }
}
