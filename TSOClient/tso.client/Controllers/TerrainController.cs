using FSO.Client.Regulators;
using FSO.Client.Rendering.City;
using FSO.Client.Rendering.City.Plugins;
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
using FSO.Content.Model;
using FSO.Files.RC;
using FSO.Server.Clients;
using FSO.Server.DataService.Model;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using FSO.Server.Protocol.Electron.Packets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Client.Controllers
{
    public class TerrainController : IAriesMessageSubscriber, IDisposable
    {
        public CoreGameScreenController Parent;
        public IShardRealestateDomain Realestate { get; }

        private Terrain View;
        private IClientDataService DataService;
        private PurchaseLotRegulator PurchaseRegulator;
        private LotThumbContent LotThumbs;
        private uint ShardId;

        private Binding<Lot> CurrentHoverLot;
        private Binding<City> CurrentCity;
        private GameThreadTimeout HoverTimeout;
        private Network.Network Network;

        public bool PlacingTownHall;
        public bool TownHallMove;
        public uint TownHallNhood;
        public string TownHallNhoodName;

        public TerrainController(CoreGameScreenController parent, IClientDataService ds, Network.Network network, IRealestateDomain domain, PurchaseLotRegulator purchaseRegulator)
        {
            this.Parent = parent;
            this.DataService = ds;
            this.PurchaseRegulator = purchaseRegulator;
            Network = network;

            PurchaseRegulator.OnError += PurchaseRegulator_OnError;
            PurchaseRegulator.OnTransition += PurchaseRegulator_OnTransition;
            PurchaseRegulator.OnPurchased += PurchaseRegulator_OnPurchased;
            ShardId = (uint)network.MyShard.Id;
            Realestate = domain.GetByShard((int)ShardId);

            CurrentHoverLot = new Binding<Lot>()
                .WithMultiBinding(RefreshTooltip, "Lot_Price", "Lot_IsOnline", "Lot_Name", "Lot_NumOccupants", "Lot_LeaderID");

            CurrentCity = new Binding<City>().WithMultiBinding(RefreshCity, "City_ReservedLotInfo", "City_SpotlightsVector");

            LotThumbs = new LotThumbContent(parent.CityResource);

            Realestate.TrackUndo(network.MyCharacter);
            Realestate.OnMapChange += MapChange;

            network.CityClient.AddSubscriber(this);
        }

        private void MapChange(Rectangle obj)
        {
            View.GenerateCityMesh(GameFacade.GraphicsDevice, obj);
            View.RegenerateVertexColor();
        }

        public void CommitMapChange(CityEditBase cmd)
        {
            cmd.AvatarId = Network.MyCharacter;

            // Send it over to the server.
            // Make sure it's not temp (but don't overwrite it for the local copy)

            var prevTemp = cmd.IsTemp;
            cmd.IsTemp = false;
            cmd.Color = GlobalSettings.Default.ChatColor;
            Network.CityClient.Write(new CityUpdateRequest() { Command = new(cmd) });
            cmd.IsTemp = prevTemp;
        }

        public void UpdateThumbnail(byte[] data)
        {
            Network.CityClient.Write(new CityUpdateCommand() { Mode = CityUpdateCommandMode.SetThumbnail, Thumbnail = data });
        }

        public void UpdateCityName(string name)
        {
            Network.CityClient.Write(new CityUpdateCommand() { Mode = CityUpdateCommandMode.SetCityName, CityName = name });
        }

        public void SendCityCommand(CityUpdateCommandMode mode, int uid)
        {
            Network.CityClient.Write(new CityUpdateCommand()
            {
                AvatarID = Network.MyCharacter,
                Mode = mode,
                TargetUID = uid
            });
        }

        public bool UpdateTempMapChange(CityEditBase cmd)
        {
            if (cmd != null)
            {
                cmd.AvatarId = Network.MyCharacter;
                cmd.IsTemp = true;

                // TODO: submit temp to city?
            }

            bool valid = Realestate.SetMyTempCommand(cmd);

            if (!valid && View.Plugin is MapPainterPlugin painter)
            {
                painter.ShowError(painter.LockProperties ? 55 : 53);
                return false;
            }

            return true;
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

            Network.CityClient.RemoveSubscriber(this);

            LotThumbs.Dispose();
        }

        public void ZoomIn(){

        }

        public void ZoomOut(){
            if (HoverTimeout != null) { HoverTimeout.Clear(); }
            CurrentHoverLot.Value = null;
        }

        public CityMap GetCityMap()
        {
            return Realestate.GetMap();
        }

        public void HideTooltip()
        {
            CurrentHoverLot.Value = null;
        }

        private void RefreshTooltip(BindingChange[] changes)
        {
            //Called if price, online or name change
            GameThread.NextUpdate((state) =>
            {
                if (CurrentHoverLot.Value != null && View.Plugin == null)
                {
                    var lot = CurrentHoverLot.Value;
                    var name = lot.Lot_Name;
                    var occupied = IsTileOccupied((int)(lot.Id >> 16), (int)(lot.Id & 0xFFFF));
                    if (!occupied)
                    {
                        var text = GameFacade.Strings.GetString("215", "9", new string[] { lot.Lot_Price.ToString() });
                        var flat = Realestate.GetSlope((ushort)(lot.Id >> 16), (ushort)(lot.Id & 0xFFFF));
                        var map = Realestate.GetMap();
                        var type = map.GetTerrain((ushort)(lot.Id >> 16), (ushort)(lot.Id & 0xFFFF));

                        text += "\r\n"+type.ToString()+", "+((flat == 0)?"Flat":"Sloped ("+flat+")");

                        var nhood = View.NeighGeom.NhoodNearest((int)(lot.Id >> 16), (int)(lot.Id & 0xFFFF));
                        if (nhood != -1)
                        {
                            var nhoodObj = View.NeighGeom.Data[nhood];
                            text += "\r\n" + nhoodObj.Name;
                        }
                        Parent.Screen.CityTooltip.Text = text;
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

        private string LastLotJSON;
        private void RefreshCity(BindingChange[] changes)
        {
            if (CurrentCity.Value != null)
            {
                GameThread.NextUpdate((state) =>
                {
                    bool updated = View.LotTiles.UpdateWithCity(CurrentCity.Value, DataService);
                    var neighJSON = CurrentCity.Value.City_NeighJSON;

                    if (updated)
                    {
                        View.SignalCityDirty();
                    }

                    if (neighJSON != LastLotJSON)
                    {
                        try
                        {
                            var neigh = JsonConvert.DeserializeObject<List<Rendering.City.Model.CityNeighbourhood>>(neighJSON);
                            Rendering.City.Model.CityNeighbourhood.Init(neigh);
                            View.NeighGeom.Data = neigh;
                            View.NeighGeom.Generate(GameFacade.GraphicsDevice);
                        } catch
                        {

                        }

                        LastLotJSON = neighJSON;
                    }
                });        
            }
        }

        public void Init(Terrain terrain){
            View = terrain;

            DataService.Get<City>((uint)0).ContinueWith(city =>
            {
                CurrentCity.Value = city.Result;
                DataService.Request(Server.DataService.Model.MaskedStruct.CurrentCity, 0);
                DataService.Request(Server.DataService.Model.MaskedStruct.City_NeighLayout, 0);
            });
        }

        public void RequestNewCity()
        {
            if (LastLotJSON == null)
                DataService.Request(MaskedStruct.City_NeighLayout, 0);
            DataService.Request(MaskedStruct.CurrentCity, 0);
        }

        public bool IsPurchasable(int x, int y)
        {
            return Realestate.IsPurchasable((ushort)x, (ushort)y) && Parent.CanPurchaseLots;
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

            if (IsPurchasable(x, y))
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
                }, 250);
            }
        }

        public Texture2D RequestLotThumb(uint location) {
            return LotThumbs.GetLotThumbForFrame(ShardId, location);
        }

        public FSOF RequestLotFacade(uint location)
        {
            return LotThumbs.GetLotFacadeForFrame(ShardId, location);
        }

        public void OverrideLotThumb(uint location, Texture2D tex)
        {
            LotThumbs.OverrideLotThumb(ShardId, location, tex);
        }

        public void OverrideLotFacade(uint location, FSOF tex)
        {
            LotThumbs.OverrideLotFacade(ShardId, location, tex);
        }

        public LotThumbEntry LockLotThumb(uint location)
        {
            return LotThumbs.GetLotEntry(ShardId, location, false);
        }

        public void UnlockLotThumb(uint location)
        {
            LotThumbs.ReleaseLotThumb(ShardId, location, false);
        }

        public LotThumbEntry LockLotFacade(uint location)
        {
            return LotThumbs.GetLotEntry(ShardId, location, true);
        }

        public void UnlockLotFacade(uint location)
        {
            LotThumbs.ReleaseLotThumb(ShardId, location, true);
        }

        public void ClickLot(int x, int y, bool forceLotPage)
        {
            var id = MapCoordinates.Pack((ushort)x, (ushort)y);
            var occupied = IsTileOccupied(x, y);
            DataService.Get<Lot>(id).ContinueWith(result =>
            {
                if (occupied)
                {
                    GameThread.InUpdate(() =>
                    {
                        if (PlacingTownHall)
                            UIAlert.Alert("", GameFacade.Strings.GetString("f115", "51"), true);
                        else
                            Parent.ShowLotPage(id);
                    });
                }
                else if (forceLotPage && Realestate.IsOpenable((ushort)x, (ushort)y))
                {
                    GameThread.InUpdate(() =>
                    {
                        Parent.ShowLotPage(id);
                    });
                }
                else if (!IsPurchasable(x, y))
                    return;
                else if (PlacingTownHall && View.NeighGeom.NhoodNearestDB(x, y) != TownHallNhood)
                {
                    UIAlert.Alert("", GameFacade.Strings.GetString("f115", "50", new string[] { TownHallNhoodName }), true);
                    return;
                }
                else
                {
                    if (PlacingTownHall)
                    {
                        //we don't particularly care about the price,
                        //all we need to know is if it is in the correct nhood

                        var ourCash = Parent.Screen.VisualBudget;
                        _BuyLot = result.Result;

                        if (ourCash < 2000)
                        {
                            UIAlert.Alert("", GameFacade.Strings.GetString("f115", "90"), true);
                        }
                        else
                        {
                            UIAlert.YesNo("", GameFacade.Strings.GetString("f115", "49"), true,
                                (complete) =>
                                {
                                    if (complete)
                                    {
                                        if (!TownHallMove)
                                        {
                                            //User needs to name the property
                                            _LotBuyName = new UILotPurchaseDialog();
                                            UIScreen.GlobalShowDialog(new DialogReference
                                            {
                                                Dialog = _LotBuyName,
                                                Controller = this,
                                                Modal = true,
                                            });
                                        }
                                        else
                                        {
                                            PurchaseRegulator.Purchase(new Regulators.PurchaseLotRequest
                                            {
                                                X = _BuyLot.Lot_Location.Location_X,
                                                Y = _BuyLot.Lot_Location.Location_Y,
                                                Name = "",
                                                StartFresh = false,
                                                Mayor = true
                                            });
                                        }
                                    }
                                });
                        }
                    }
                    else
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
                    }
                }
            });            
        }

        private UIAlert _LotBuyAlert;
        private Lot _BuyLot;

        private void ShowLotBuyDialog(Lot lot)
        {
            GameThread.InUpdate(() =>
            {
                GameFacade.Cursor.SetCursor(Common.Rendering.Framework.CursorType.Hourglass);
                if (_LotBuyAlert != null) { return; }
                _LotBuyAlert = new UIAlert(new UIAlertOptions() { Title = "", Message = "" }); //just fill this space til we spawn the dialog.
                _BuyLot = lot;
                Parent.Screen.CityTooltipHitArea.HideTooltip();

                var price = lot.Lot_Price;
                var ourCash = Parent.Screen.VisualBudget;


                DataService.Request(MaskedStruct.SimPage_Main, Network.MyCharacter).ContinueWith(x =>
                {
                    var avatar = x.Result as Avatar;
                    if (!x.IsFaulted && avatar != null && avatar.Avatar_LotGridXY != 0)
                    {
                        //we already have a lot. We need to show the right dialog depending on whether or not we're owner.
                        var oldID = avatar.Avatar_LotGridXY;
                        DataService.Request(MaskedStruct.PropertyPage_LotInfo, oldID).ContinueWith(y =>
                        {
                            GameThread.SetTimeout(() => //setting a timeout here because for some reason when the request finishes we might not have all of the data yet...
                            {
                                GameFacade.Cursor.SetCursor(Common.Rendering.Framework.CursorType.Normal);
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
                                    new UIAlertButton(UIAlertButtonType.Yes, (btn) => {
                                        UIScreen.RemoveDialog(_LotBuyAlert);
                                        _LotBuyAlert = UIScreen.GlobalShowAlert(new UIAlertOptions() {
                                            Message = GameFacade.Strings.GetString("211", "57"),
                                            Buttons = new UIAlertButton[0]
                                            }, true);
                                        Parent.MoveMeOut(oldID, (result) => {
                                            if (result) BuyPropertyAlert_OnButtonClick(btn);
                                        });
                                    }),
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
                            }, 100);
                        });
                    }
                    else
                    {
                        //we don't have a lot
                        GameThread.InUpdate(() =>
                        {
                            _LotBuyAlert = null;
                            ShowNormalLotBuy("$" + price.ToString(), "$" + ourCash.ToString());
                            var canBuy = price <= ourCash;
                            UIButton toDisable;
                            if (_LotBuyAlert.ButtonMap.TryGetValue(UIAlertButtonType.Yes, out toDisable)) toDisable.Disabled = !canBuy;
                            GameFacade.Cursor.SetCursor(Common.Rendering.Framework.CursorType.Normal);
                        });
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
            if (_LotBuyAlert != null)
            {
                UIScreen.RemoveDialog(_LotBuyAlert);
                _LotBuyAlert = null;
            }

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
            PurchaseRegulator.Purchase(new Regulators.PurchaseLotRequest
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

            PurchaseRegulator.Purchase(new Regulators.PurchaseLotRequest {
                X = _BuyLot.Lot_Location.Location_X,
                Y = _BuyLot.Lot_Location.Location_Y,
                Name = name,
                Mayor = PlacingTownHall
            });
            _LotBuyName = null;
        }

        public void PlaceTownHall(bool move, uint nhoodID, string nhoodName)
        {
            if (PlacingTownHall) return;

            PlacingTownHall = true;
            TownHallMove = move;
            TownHallNhood = nhoodID;
            TownHallNhoodName = nhoodName;

            Parent.Screen.Title.SetOverrideMode(
                GameFacade.Strings.GetString("f115", move ? "47" : "46", new string[] { nhoodName }), 
                () => {
                    EndTownHall();
                });

            UIAlert.Alert("", GameFacade.Strings.GetString("f115", "91", new string[] { nhoodName }), true);
        }

        public void EndTownHall()
        {
            Parent.Screen.Title.ClearOverrideMode();
            PlacingTownHall = false;
        }

        private void PurchaseRegulator_OnError(object data)
        {
            GameThread.NextUpdate(x =>
            {
                ShowCreationProgressBar(false);
                //TODO: Find error messages in lang table
                var reason = (PurchaseLotFailureReason)data;

                string error = "An error occurred.";
                switch (reason)
                {
                    case PurchaseLotFailureReason.INSUFFICIENT_FUNDS:
                        error = GameFacade.Strings.GetString("215", "26");
                        break;
                    case PurchaseLotFailureReason.NOT_OFFLINE_FOR_MOVE:
                        error = GameFacade.Strings.GetString("211", "53");
                        break;
                    case PurchaseLotFailureReason.IN_LOT_CANT_EVICT:
                        error = GameFacade.Strings.GetString("211", "64");
                        break;
                    case PurchaseLotFailureReason.LOT_TAKEN:
                    case PurchaseLotFailureReason.LOT_NOT_PURCHASABLE:
                        error = GameFacade.Strings.GetString("211", "46");
                        break;
                    case PurchaseLotFailureReason.NAME_TAKEN:
                        error = GameFacade.Strings.GetString("247", "15");
                        break;
                    default:
                        error = GameFacade.Strings.GetString("211", "55") + " ("+reason.ToString()+")";
                        break;
                }
                UIScreen.GlobalShowAlert(new UIAlertOptions
                {
                    Message = error,
                    //TODO: Find something in string tables?
                    Title = "",
                    Width = 300
                }, true);
            });
        }

        private void PurchaseRegulator_OnTransition(string state, object data)
        {
            GameThread.InUpdate(() =>
            {
                if (state == "PurchaseComplete")
                {
                    DataService.Request(MaskedStruct.CurrentCity, 0);
                    ShowCreationProgressBar(false);
                    if (PlacingTownHall)
                    {
                        EndTownHall();
                    }
                }
            });
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

        public void MessageReceived(AriesClient client, object message)
        {
            if (message is CityUpdateCommand cmd && View.Plugin is MapPainterPlugin map)
            {
                GameThread.NextUpdate(x =>
                {
                    switch (cmd.Mode)
                    {
                        case CityUpdateCommandMode.CommandError:
                            Realestate.SetMyTempCommand(null);
                            map.ShowError(53);
                            break;
                        case CityUpdateCommandMode.UndoError:
                            map.ShowError(54);
                            break;
                    }
                });
            }
            else if (message is CityUpdateResponse response)
            {
                GameThread.NextUpdate(x =>
                {
                    var screen = UIScreen.Current as CoreGameScreen;

                    foreach (var item in response.Commands)
                    {
                        if (item.Command.AvatarId == Network.MyCharacter)
                        {
                            return;
                        }

                        var mod = CityModification.FromCommand(View.MapData, item.Command);
                        if (mod != null)
                        {
                            View.AddModification(mod);
                            screen?.CityUpdateLayer?.RegisterModification(mod);
                        }
                    }
                });
            }
        }
    }
}
