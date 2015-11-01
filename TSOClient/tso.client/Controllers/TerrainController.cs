using FSO.Client.Regulators;
using FSO.Client.Rendering.City;
using FSO.Client.UI;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Screens;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Common.Utils;
using FSO.Server.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.Controllers
{
    public class TerrainController
    {
        private CoreGameScreenController Parent;
        private Terrain View;
        private IClientDataService DataService;
        private IShardRealestateDomain Realestate;
        private PurchaseLotRegulator PurchaseRegulator;

        private Binding<Lot> CurrentHoverLot;
        private GameThreadTimeout HoverTimeout;

        public TerrainController(CoreGameScreenController parent, IClientDataService ds, Network.Network network, IRealestateDomain domain, PurchaseLotRegulator purchaseRegulator)
        {
            this.Parent = parent;
            this.DataService = ds;
            this.PurchaseRegulator = purchaseRegulator;

            PurchaseRegulator.OnError += PurchaseRegulator_OnError;
            PurchaseRegulator.OnTransition += PurchaseRegulator_OnTransition;
            Realestate = domain.GetByShard(network.MyShard.Id);

            CurrentHoverLot = new Binding<Lot>()
                .WithMultiBinding(RefreshTooltip, "Lot_Price", "Lot_IsOnline", "Lot_Name");
        }

        ~TerrainController()
        {
            PurchaseRegulator.OnError -= PurchaseRegulator_OnError;
            PurchaseRegulator.OnTransition -= PurchaseRegulator_OnTransition;
        }

        public void ZoomIn(){

        }

        public void ZoomOut(){
            if (HoverTimeout != null) { HoverTimeout.Clear(); }
            CurrentHoverLot.Value = null;
        }

        private void RefreshTooltip(BindingChange[] changes)
        {
            //Called if price, online or name change
            if (CurrentHoverLot.Value != null) {
                Parent.Screen.CityTooltip.Text = "Vacant Lot: $" + CurrentHoverLot.Value.Lot_Price;
            }else{
                Parent.Screen.CityTooltip.Text = null;
            }
        }

        public void Init(Terrain terrain){
            View = terrain;
        }

        public bool IsPurchasable(int x, int y)
        {
            return Realestate.IsPurchasable((ushort)x, (ushort)y);
        }

        public void HoverTile(int x, int y)
        {
            //Slight delay
            CurrentHoverLot.Value = null;
            if (HoverTimeout != null) { HoverTimeout.Clear(); }

            if (Realestate.IsPurchasable((ushort)x, (ushort)y))
            {
                HoverTimeout = GameThread.SetTimeout(() =>
                {
                    var id = MapCoordinates.Pack((ushort)x, (ushort)y);
                    DataService.Get<Lot>(id).ContinueWith(lot =>
                    {
                        CurrentHoverLot.Value = lot.Result;

                        //Not loaded yet
                        if (lot.Result.Lot_Price == 0)
                        {
                            DataService.Request(Server.DataService.Model.MaskedStruct.MapView_RollOverInfo_Lot_Price, id);
                        }
                    });
                }, 500);
            }
        }

        public void ClickLot(int x, int y)
        {
            if (!Realestate.IsPurchasable((ushort)x, (ushort)y))
            {
                return;
            }

            var id = MapCoordinates.Pack((ushort)x, (ushort)y);
            DataService.Get<Lot>(id).ContinueWith(result =>
            {
                if (result.Result.Lot_Price == 0)
                {
                    //We need to request the price
                    DataService.Request(MaskedStruct.MapView_RollOverInfo_Lot_Price, id).ContinueWith(masked =>
                    {
                        ShowLotBuyDialog((Lot)masked.Result);
                    });
                }
                else
                {
                    //Good to show dialog
                    ShowLotBuyDialog(result.Result);
                }
            });            
        }

        private UIAlert _LotBuyAlert;
        private Lot _BuyLot;

        private void ShowLotBuyDialog(Lot lot)
        {
            if (_LotBuyAlert != null) { return; }

            _BuyLot = lot;
            Parent.Screen.CityTooltipHitArea.HideTooltip();

            //TODO: Put my actual money in
            //TODO: Disable yes if cant afford
            UIAlertOptions AlertOptions = new UIAlertOptions();
            AlertOptions.Title = GameFacade.Strings.GetString("246", "1");
            AlertOptions.Message = GameFacade.Strings.GetString("215", "23", new string[]{ lot.Lot_Price.ToString(), "0" });
            AlertOptions.Buttons = new UIAlertButton[] {
                    new UIAlertButton(UIAlertButtonType.Yes, new ButtonClickDelegate(BuyPropertyAlert_OnButtonClick)),
                    new UIAlertButton(UIAlertButtonType.No) };

            _LotBuyAlert = UIScreen.GlobalShowAlert(AlertOptions, true);
        }

        private UILotPurchaseDialog _LotBuyName;

        private void BuyPropertyAlert_OnButtonClick(UIElement button) {
            UIScreen.RemoveDialog(_LotBuyAlert);
            _LotBuyAlert = null;

            //User needs to name the property
            _LotBuyName = new UILotPurchaseDialog();
            UIScreen.GlobalShowDialog(new DialogReference {
                Dialog = _LotBuyName,
                Controller = this,
                Modal = true,
            });
        }
        
        public void PurchaseLot(string name)
        {
            //Show waiting dialog
            UIScreen.RemoveDialog(_LotBuyName);
            ShowCreationProgressBar(true);

            PurchaseRegulator.Purchase(new PurchaseLotRequest {
                X = _BuyLot.Lot_Location.Location_X,
                Y = _BuyLot.Lot_Location.Location_Y,
                Name = name
            });
        }


        private void PurchaseRegulator_OnError(object data)
        {
            ShowCreationProgressBar(false);
            //TODO: Find error messages in lang table
            UIScreen.GlobalShowAlert(new UIAlertOptions
            {
                Message = data.ToString(),
                //TODO: Find something in string tables?
                Title = "Buy Property",
                Width = 300
            }, true);
        }

        private void PurchaseRegulator_OnTransition(string state, object data)
        {
            if(state == "PurchaseComplete")
            {
                ShowCreationProgressBar(false);
            }
        }

        private UIAlert _ProgressAlert;

        public void ShowCreationProgressBar(bool show)
        {
            if (show)
            {
                if (_ProgressAlert == null)
                {
                    _ProgressAlert = UIScreen.GlobalShowAlert(new UIAlertOptions
                    {
                        Message = "",
                        Buttons = new UIAlertButton[] { },
                        //TODO: Find something in string tables?
                        Title = "Buy Property",
                        ProgressBar = true,
                        Width = 300
                    }, true);
                }
            }
            else
            {
                if (_ProgressAlert != null)
                {
                    UIScreen.RemoveDialog(_ProgressAlert);
                    _ProgressAlert = null;
                }
            }
        }
    }
}
