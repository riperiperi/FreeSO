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
using FSO.Files;
using FSO.Server.DataService.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.Client.Controllers
{
    public class TerrainController : IDisposable
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
            PurchaseRegulator.OnPurchased += PurchaseRegulator_OnPurchased;
            Realestate = domain.GetByShard(network.MyShard.Id);

            CurrentHoverLot = new Binding<Lot>()
                .WithMultiBinding(RefreshTooltip, "Lot_Price", "Lot_IsOnline", "Lot_Name", "Lot_NumOccupants", "Lot_LeaderID");

            CurrentCity = new Binding<City>().WithMultiBinding(RefreshCity, "City_ReservedLotInfo", "City_SpotlightsVector");
        }

        private void PurchaseRegulator_OnPurchased(int newBudget)
        {
            Parent.Screen.VisualBudget = (uint)newBudget;
        }

        public void Dispose()
        {
            PurchaseRegulator.OnError -= PurchaseRegulator_OnError;
            PurchaseRegulator.OnTransition -= PurchaseRegulator_OnTransition;
            PurchaseRegulator.OnPurchased -= PurchaseRegulator_OnPurchased;
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

                //We know if lots are online, we can update the data service
                DataService.GetMany<Lot>(mapData.Select(x => (object)(uint)x.packed_pos).ToArray()).ContinueWith(x =>
                {
                    if (!x.IsCompleted){
                        return;
                    }

                    foreach (var lot in x.Result)
                    {
                        var mapItem = mapData.FirstOrDefault(y => y.packed_pos == lot.Id);
                        if (mapItem != null) {
                            lot.Lot_IsOnline = (mapItem.flags & LotTileFlags.Online) == LotTileFlags.Online;
                        }
                    }
                });

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

        public void RequestNewCity()
        {
            DataService.Request(MaskedStruct.CurrentCity, 0);
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

        public void RequestLotThumb(uint location, Callback<Texture2D> onRetrieved) {
            DataService.Request(MaskedStruct.MapView_NearZoom_Lot_Thumbnail, location).ContinueWith(x =>
            {
                //happens in game thread
                var lot = (Lot)x.Result;
                if (lot == null) return;
                var thumb = lot.Lot_Thumbnail;
                if (thumb.Data.Length == 0) return;
                onRetrieved(ImageLoader.FromStream(GameFacade.GraphicsDevice, new MemoryStream(thumb.Data)));
            });
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
            GameThread.InUpdate(() =>
            {
                if (_LotBuyAlert != null) { return; }
                _LotBuyAlert = new UIAlert(new UIAlertOptions() { Title = "", Message = "" }); //just fill this space til we spawn the dialog.
                _BuyLot = lot;
                Parent.Screen.CityTooltipHitArea.HideTooltip();

                var price = lot.Lot_Price;
                var ourCash = Parent.Screen.VisualBudget;

                DataService.Get<Avatar>(Network.MyCharacter).ContinueWith(x =>
                {
                    if (!x.IsFaulted && x.Result != null && x.Result.Avatar_LotGridXY != 0)
                    {
                        //we already have a lot. We need to show the right dialog depending on whether or not we're owner.
                        var oldID = x.Result.Avatar_LotGridXY;
                        DataService.Request(MaskedStruct.PropertyPage_LotInfo, oldID).ContinueWith(y =>
                        {
                            GameThread.InUpdate(() =>
                            {
                                bool canBuy = true;
                                if (!y.IsFaulted && y.Result != null)
                                {
                                    var old = (Lot)y.Result;
                                    UIAlertOptions AlertOptions = new UIAlertOptions();
                                    if (old.Lot_LeaderID == Network.MyCharacter)
                                    {
                                        //we are the owner
                                        var oldVal = old.Lot_Price;
                                        var moveFee = 2000;
                                        var moveCost = moveFee + price;

                                        canBuy = (moveCost - oldVal) <= ourCash;
                                        if (old.Lot_RoommateVec.Count > 1)
                                        {
                                            //we have other roommates.
                                            AlertOptions.Title = GameFacade.Strings.GetString("215", "10");
                                            AlertOptions.Message = GameFacade.Strings.GetString("215", "12",
                                                new string[] { "$" + price.ToString(), "$" + ourCash.ToString(), "$" + moveCost.ToString(), "$" + moveFee.ToString(), "$" + oldVal.ToString() });
                                            AlertOptions.Buttons = new UIAlertButton[] {
                                        new UIAlertButton(UIAlertButtonType.Yes, (button) => { MoveLot(false); }, GameFacade.Strings.GetString("215", "14")),
                                        new UIAlertButton(UIAlertButtonType.Cancel, BuyPropertyAlert_OnCancel)
                                        };
                                        }
                                        else
                                        {
                                            //we live alone
                                            AlertOptions.Title = GameFacade.Strings.GetString("215", "10");
                                            AlertOptions.Message = GameFacade.Strings.GetString("215", "16",
                                                new string[] { "$" + price.ToString(), "$" + ourCash.ToString(), "$" + moveCost.ToString(), "$" + moveFee.ToString(), "$" + oldVal.ToString() });
                                            AlertOptions.Buttons = new UIAlertButton[] {
                                        new UIAlertButton(UIAlertButtonType.OK, (button) => { MoveLot(false); }, GameFacade.Strings.GetString("215", "17")),
                                        new UIAlertButton(UIAlertButtonType.Yes, (button) => { MoveLot(true); }, GameFacade.Strings.GetString("215", "18")),
                                        new UIAlertButton(UIAlertButtonType.Cancel, BuyPropertyAlert_OnCancel)
                                        };
                                        }
                                    }
                                    else
                                    {
                                        //we are a roommate.
                                        //can leave and start a new lot with no issue.
                                        canBuy = price <= ourCash;
                                        AlertOptions.Title = GameFacade.Strings.GetString("215", "10");
                                        AlertOptions.Message = GameFacade.Strings.GetString("215", "20", new string[] { "$" + price.ToString(), "$" + ourCash.ToString() });
                                        AlertOptions.Buttons = new UIAlertButton[] {
                                    new UIAlertButton(UIAlertButtonType.Yes, new ButtonClickDelegate(BuyPropertyAlert_OnButtonClick)),
                                    new UIAlertButton(UIAlertButtonType.No, BuyPropertyAlert_OnCancel)
                                    };
                                    }

                                    AlertOptions.Width = 600;
                                    _LotBuyAlert = UIScreen.GlobalShowAlert(AlertOptions, true);
                                    UIButton toDisable;
                                    if (_LotBuyAlert.ButtonMap.TryGetValue(UIAlertButtonType.OK, out toDisable)) toDisable.Disabled = !canBuy;
                                    if (_LotBuyAlert.ButtonMap.TryGetValue(UIAlertButtonType.Yes, out toDisable)) toDisable.Disabled = !canBuy;
                                }
                                else
                                {
                                    canBuy = price <= ourCash;
                                    ShowNormalLotBuy("$" + price.ToString(), "$" + ourCash.ToString());
                                    UIButton toDisable;
                                    if (_LotBuyAlert.ButtonMap.TryGetValue(UIAlertButtonType.Yes, out toDisable)) toDisable.Disabled = !canBuy;
                                }
                            });
                        });
                    }
                    else
                    {
                        //we don't have a lot
                        _LotBuyAlert = null;
                        ShowNormalLotBuy("$"+price.ToString(), "$" + ourCash.ToString());
                    }
                });
            });
        }

        private void ShowNormalLotBuy(string price, string ourCash)
        {
            UIAlertOptions AlertOptions = new UIAlertOptions();
            AlertOptions.Title = GameFacade.Strings.GetString("246", "1");
            AlertOptions.Message = GameFacade.Strings.GetString("215", "23", new string[] { price, ourCash });
            AlertOptions.Buttons = new UIAlertButton[] {
                    new UIAlertButton(UIAlertButtonType.Yes, new ButtonClickDelegate(BuyPropertyAlert_OnButtonClick)),
                    new UIAlertButton(UIAlertButtonType.No, BuyPropertyAlert_OnCancel) };

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

        private void BuyPropertyAlert_OnCancel(UIElement button)
        {
            UIScreen.RemoveDialog(_LotBuyAlert);
            _LotBuyAlert = null;
        }

        public void MoveLot(bool freshStart)
        {
            UIScreen.RemoveDialog(_LotBuyAlert);
            _LotBuyAlert = null;
            PurchaseRegulator.Purchase(new PurchaseLotRequest
            {
                X = _BuyLot.Lot_Location.Location_X,
                Y = _BuyLot.Lot_Location.Location_Y,
                Name = "",
                StartFresh = freshStart
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
            _LotBuyName = null;
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
                DataService.Request(MaskedStruct.CurrentCity, 0);
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
