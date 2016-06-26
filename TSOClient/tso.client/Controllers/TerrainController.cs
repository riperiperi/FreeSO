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
using Microsoft.Xna.Framework;
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
        private Binding<City> CurrentCity;
        private GameThreadTimeout HoverTimeout;
        private Network.Network Network;

        public TerrainController(CoreGameScreenController parent, IClientDataService ds, Network.Network network, IRealestateDomain domain, PurchaseLotRegulator purchaseRegulator)
        {
            this.Parent = parent;
            this.DataService = ds;
            this.PurchaseRegulator = purchaseRegulator;
            Network = network;

            PurchaseRegulator.OnError += PurchaseRegulator_OnError;
            PurchaseRegulator.OnTransition += PurchaseRegulator_OnTransition;
            Realestate = domain.GetByShard(network.MyShard.Id);

            CurrentHoverLot = new Binding<Lot>()
                .WithMultiBinding(RefreshTooltip, "Lot_Price", "Lot_IsOnline", "Lot_Name", "Lot_NumOccupants", "Lot_LeaderID");

            CurrentCity = new Binding<City>().WithMultiBinding(RefreshCity, "City_ReservedLotInfo", "City_SpotlightsVector");
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
            GameThread.NextUpdate((state) =>
            {
                if (CurrentHoverLot.Value != null)
                {
                    var lot = CurrentHoverLot.Value;
                    var name = lot.Lot_Name;
                    var occupied = IsTileOccupied((int)(lot.Id >> 16), (int)(lot.Id & 0xFFFF));
                    if (!occupied)
                    {
                        Parent.Screen.CityTooltip.Text = GameFacade.Strings.GetString("215", "9", new string[] { lot.Lot_Price.ToString() });
                    }
                    else
                    {
                        var text = GameFacade.Strings.GetString("215", "3", new string[] { name });
                        if (lot.Lot_LeaderID == Network.MyCharacter) text += "\r\n" + GameFacade.Strings.GetString("215", "5");
                        else if (!lot.Lot_IsOnline) text += "\r\n" + GameFacade.Strings.GetString("215", "6");

                        if (lot.Lot_IsOnline) text += "\r\n" + GameFacade.Strings.GetString("215", "4", new string[] { lot.Lot_NumOccupants.ToString() });
                        Parent.Screen.CityTooltip.Text = text;
                    }
                }
                else
                {
                    Parent.Screen.CityTooltip.Text = null;
                }
            });
        }

        private void RefreshCity(BindingChange[] changes)
        {
            if (CurrentCity.Value != null)
            {
                var mapData = LotTileEntry.GenFromCity(CurrentCity.Value);
                GameThread.NextUpdate((state) => View.populateCityLookup(mapData));        
            }
        }

        public void Init(Terrain terrain){
            View = terrain;

            DataService.Get<City>((uint)0).ContinueWith(city =>
            {
                CurrentCity.Value = city.Result;
                DataService.Request(Server.DataService.Model.MaskedStruct.CurrentCity, 0);
            });
        }

        public bool IsPurchasable(int x, int y)
        {
            return Realestate.IsPurchasable((ushort)x, (ushort)y);
        }

        private bool IsTileOccupied(int x, int y)
        {
            return View.LotTileLookup.ContainsKey(new Vector2(x, y));
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
                    var occupied = IsTileOccupied(x, y);
                    DataService.Get<Lot>(id).ContinueWith(lot =>
                    {
                        CurrentHoverLot.Value = lot.Result;

                        //Not loaded yet
                        if (lot.Result.Lot_Price == 0)
                        {
                            if (occupied) DataService.Request(MaskedStruct.MapView_RollOverInfo_Lot, id);
                            else DataService.Request(MaskedStruct.MapView_RollOverInfo_Lot_Price, id);
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
            var occupied = IsTileOccupied(x, y);
            DataService.Get<Lot>(id).ContinueWith(result =>
            {

                if (occupied)
                {
                    Parent.ShowLotPage(id);
                }
                else if (result.Result.Lot_Price == 0)
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
