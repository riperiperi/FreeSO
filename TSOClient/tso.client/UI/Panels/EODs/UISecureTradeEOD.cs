using FSO.Client.UI.Controls;
using FSO.Client.UI.Controls.Catalog;
using FSO.Client.UI.Framework.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics.Model;
using FSO.Content.Interfaces;
using FSO.Common.Rendering.Framework.IO;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Entities;
using System.IO;
using FSO.Common;
using FSO.SimAntics;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Client.Controllers.Panels;
using FSO.Common.Utils;
using FSO.Client.UI.Model;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.Client.UI.Panels.EODs
{
    public class UISecureTradeEOD : UIEOD
    {
        public UIScript Script;
        public UICatalog Catalog;
        public UICatalog OfferCatalog;
        public UICatalog OtherOfferCatalog;
        public int TimeSinceType;
        public UIQueryPanel QueryPanel { get { return LotController.QueryPanel; } }

        private List<VMInventoryItem> LastInventory;
        private List<UICatalogElement> CurrentInventory;

        public UISlider InventoryCatalogSecureTradingSlider { get; set; }
        public UIButton InventoryCatalogSecureTradingPreviousPageButton { get; set; }
        public UIButton InventoryCatalogSecureTradingNextPageButton { get; set; }

        public UITextEdit OurAvatarMoneySymbol { get; set; }
        public UITextEdit OtherAvatarMoneySymbol { get; set; }

        public UITextEdit AmountEntry { get; set; }
        public UITextEdit OtherAvatarAmount { get; set; }

        public UIButton AcceptButton { get; set; }
        public UIButton OtherAcceptedButton { get; set; }
        public UILabel LockoutTimerLabel;

        public UIImage InventoryCatalogRoommateImage;

        public bool LastMouse;

        public uint DragUID = 0;
        public UICatalogItem DragItem;

        public VMEODSecureTradePlayer MyOffer = new VMEODSecureTradePlayer();
        public VMEODSecureTradePlayer OtherOffer = new VMEODSecureTradePlayer();

        public UIVMPersonButton MyPerson;
        public UIVMPersonButton OtherPerson;

        public string MyLotName;
        public bool OwnerOfLot;
        private bool Small800;

        private List<Tuple<Rectangle, float>> NotifyRects = new List<Tuple<Rectangle, float>>();

        public UISecureTradeEOD(UIEODController controller) : base(controller)
        {
            var ctr = ControllerUtils.BindController<SecureTradeController>(this);
            InitUI();
            InitEOD();
        }

        protected virtual void InitUI()
        {
            Small800 = (GlobalSettings.Default.GraphicsWidth < 1024) || Common.FSOEnvironment.UIZoomFactor > 1f;
            Script = this.RenderScript("securetradingeod"+((Small800)?"":"1024")+".uis");

            Catalog = new UICatalog(Small800 ? 18 : 28);
            Script.ApplyControlProperties(Catalog, "InventoryCatalogSecureTrading");
            Catalog.X -= 2;
            Add(Catalog);

            OfferCatalog = new UICatalog(10); //only uses top row
            OfferCatalog.Position = Catalog.Position + new Vector2(41, 116);
            Add(OfferCatalog);

            OtherOfferCatalog = new UICatalog(10); //only uses top row
            OtherOfferCatalog.Position = Catalog.Position + new Vector2(41, 166);
            Add(OtherOfferCatalog);

            OurAvatarMoneySymbol.CurrentText = "$";
            OtherAvatarMoneySymbol.CurrentText = "$";
            AmountEntry.Alignment = Framework.TextAlignment.Center;
            OtherAvatarAmount.Alignment = Framework.TextAlignment.Center;

            AmountEntry.OnChange += AmountEntry_OnChange;

            AcceptButton.OnButtonClick += AcceptClicked;
            OtherAcceptedButton.Disabled = true;
            OtherAcceptedButton.ForceState = 0;

            LockoutTimerLabel = new UILabel();
            LockoutTimerLabel.Size = AmountEntry.Size;
            LockoutTimerLabel.Alignment = TextAlignment.Center;
            LockoutTimerLabel.Position = (AmountEntry.Position + OtherAvatarAmount.Position) / 2;
            Add(LockoutTimerLabel);

            SetMyPerson(LotController.vm.MyUID);

            Add(QueryPanel);

            InventoryCatalogRoommateImage = Script.Create<UIImage>("InventoryCatalogRoommateImage");
            Add(InventoryCatalogRoommateImage);

            Catalog.OnSelectionChange += Catalog_OnSelectionChange;
            OfferCatalog.OnSelectionChange += OfferCatalog_OnSelectionChange;
            OtherOfferCatalog.OnSelectionChange += OtherOfferCatalog_OnSelectionChange;

            InventoryCatalogSecureTradingSlider.AttachButtons(InventoryCatalogSecureTradingPreviousPageButton, InventoryCatalogSecureTradingNextPageButton, 1f);
            InventoryCatalogSecureTradingSlider.OnChange += (el) => { SetPage((int)InventoryCatalogSecureTradingSlider.Value); };

            BuildInventory();

            FindController<SecureTradeController>().GetOurLotsName((name, owner) =>
            {
                MyLotName = name;
                OwnerOfLot = owner;
                if (MyLotName != null) BuildInventory();
            });
        }

        private void OtherOfferCatalog_OnSelectionChange(int selection)
        {
            var elemItem = OtherOfferCatalog.Selected[selection];
            var item = OtherOffer.ObjectOffer.FirstOrDefault(x => x != null && (x.PID == (uint)elemItem.Tag || (x.PID < 3 && (uint)elemItem.Tag == x.GUID)));
            if (item == null) return;

            var oldGUID = item.GUID;
            if (item.LotID > 0) item.GUID = (uint)((item.GUID == 1) ? 0x3495FC60 : 0x34B4B46A);
            var BuyItem = CreateObjGroup(item);
            item.GUID = oldGUID;

            if (BuyItem == null) return;
            if (item.LotID > 0) BuyItem.Name = elemItem.Item.Name;
            QueryPanel.SetInfo(LotController.vm, BuyItem.Objects[0], true);
            QueryPanel.Mode = 2;
            QueryPanel.Tab = 1;
            QueryPanel.Active = true;

            BuyItem.Delete(LotController.vm.Context);
        }

        private void AcceptClicked(Framework.UIElement button)
        {
            MyOffer.Accepted = !MyOffer.Accepted;
            AcceptButton.Selected = MyOffer.Accepted;
            Send("trade_offer", "a" + (MyOffer.Accepted?"1":"0"));
        }

        private void AmountEntry_OnChange(Framework.UIElement element)
        {
            TimeSinceType = FSOEnvironment.RefreshRate;
            int parse;
            if (int.TryParse(AmountEntry.CurrentText, out parse))
            {
                MyOffer.MoneyOffer = parse;
                Send("trade_offer", "m" + parse);
            }
        }

        private VMMultitileGroup CreateObjGroup(VMEODSecureTradeObject item)
        {
            var data = item.Data;
            VMStandaloneObjectMarshal state = null;
            if (data != null)
            {
                state = new VMStandaloneObjectMarshal();
                try
                {
                    using (var reader = new BinaryReader(new MemoryStream(data)))
                    {
                        state.Deserialize(reader);
                    }
                    foreach (var e in state.Entities) ((VMGameObjectMarshal)e).Disabled = 0;
                }
                catch (Exception)
                {
                    //failed to restore state
                    state = null;
                }
            }

            VMMultitileGroup BuyItem;

            if (state != null)
            {
                BuyItem = state.CreateInstance(LotController.vm, true);
                BuyItem.ChangePosition(LotView.Model.LotTilePos.OUT_OF_WORLD, LotView.Model.Direction.NORTH, LotController.vm.Context, VMPlaceRequestFlags.UserPlacement);
                if (BuyItem.Objects.Count == 0) BuyItem = null;
            }
            else
            {
                BuyItem = LotController.vm.Context.CreateObjectInstance(item.GUID, LotView.Model.LotTilePos.OUT_OF_WORLD, LotView.Model.Direction.NORTH, true);
                if (BuyItem == null || BuyItem.Objects.Count == 0)
                {
                    BuyItem = null;
                    return null; //uh
                }
            }
            return BuyItem;
        }

        private void OfferCatalog_OnSelectionChange(int selection)
        {
            var elemItem = OfferCatalog.Selected[selection];
            var item = MyOffer.ObjectOffer.FirstOrDefault(x => x != null && (x.PID == (uint)elemItem.Tag || (x.PID < 3 && (uint)elemItem.Tag < 3)));
            if (item == null) return;

            var oldGUID = item.GUID;
            if (item.LotID > 0) item.GUID = (uint)((item.GUID == 1) ? 0x3495FC60 : 0x34B4B46A);
            var BuyItem = CreateObjGroup(item);
            item.GUID = oldGUID;
            if (BuyItem == null) return;
            if (item.LotID > 0) BuyItem.Name = elemItem.Item.Name;

            QueryPanel.SetInfo(LotController.vm, BuyItem.Objects[0], true);
            QueryPanel.Mode = 2;
            QueryPanel.Tab = 1;
            QueryPanel.Active = true;

            BuyItem.Delete(LotController.vm.Context);

            BeginDrag(elemItem, item.PID);
        }

        private void Catalog_OnSelectionChange(int selection)
        {
            if (selection >= CurrentInventory.Count) return;
            var item = CurrentInventory[selection];
            Catalog.SetActive(selection, true);
            var BuyItem = LotController.vm.Context.CreateObjectInstance(item.Item.GUID, LotView.Model.LotTilePos.OUT_OF_WORLD, LotView.Model.Direction.NORTH, true);
            if (BuyItem == null || BuyItem.Objects.Count == 0)
            {
                BuyItem = null;
                return; //uh
            }
            if (item.Item.Name != null && item.Item.Name != "")
                BuyItem.Name = item.Item.Name;
            if (item.Item.DisableLevel > 1 && ((VMTSOAvatarState)LotController.ActiveEntity.TSOState).Permissions < VMTSOAvatarPermissions.Admin)
            {
                HIT.HITVM.Get().PlaySoundEvent(UISounds.Error);
                QueryPanel.Active = false;
                return; //can't trade this
            }
            QueryPanel.SetInfo(LotController.vm, BuyItem.Objects[0], false);
            QueryPanel.Mode = 2;
            QueryPanel.Tab = 0;
            QueryPanel.Active = true;
            BuyItem.Delete(LotController.vm.Context);

            if (item.Tag is uint) {
                BeginDrag(item, (uint)(item.Tag));
            }
        }

        private void BeginDrag(UICatalogElement item, uint uid)
        {
            DragUID = uid;
            DragItem = new UICatalogItem(true);
            DragItem.SetDisabled(false);
            DragItem.Info = item;
            DragItem.Info.CalcPrice = item.CalcPrice;

            DragItem.Icon = (item.Special?.Res != null) ? item.Special.Res.GetIcon(item.Special.ResID) : Catalog.GetObjIcon(item.Item.GUID);
            DragItem.Tooltip = (item.CalcPrice > 0) ? ("$" + item.CalcPrice.ToString()) : null;
            LastMouse = true;

            Add(DragItem);

            BuildInventory();
            BuildOffer(MyOffer.ObjectOffer, OfferCatalog);
        }

        private void UpdateCatalog()
        {
            Catalog.SetCategory(CurrentInventory);

            int total = Catalog.TotalPages();

            InventoryCatalogSecureTradingSlider.MaxValue = total - 1;
            InventoryCatalogSecureTradingSlider.Value = 0;
            
            InventoryCatalogSecureTradingNextPageButton.Disabled = (total == 1);
            InventoryCatalogSecureTradingPreviousPageButton.Disabled = true;
        }

        private void BuildInventory()
        {
            var inventory = LotController.vm.MyInventory;
            var lastCatPage = Catalog.GetPage();
            LastInventory = new List<VMInventoryItem>(inventory);
            if (CurrentInventory == null) CurrentInventory = new List<UICatalogElement>();
            CurrentInventory.Clear();
            //if we own a lot, we can transfer it to the other user
            if (MyLotName != null && OwnerOfLot)
            {
                var deed = GenCatItem(0x3495FC60);
                deed.Name += " - " + MyLotName;
                if (DragUID != 1 && Array.FindIndex(MyOffer.ObjectOffer, x => x != null && x.GUID == 1) == -1)
                    CurrentInventory.Add(new UICatalogElement { Item = deed, Tag = (uint)1 });
                var deedWithObjects = GenCatItem(0x34B4B46A);
                deedWithObjects.Name += " - " + MyLotName;
                if (DragUID != 2 && Array.FindIndex(MyOffer.ObjectOffer, x => x != null && x.GUID == 2) == -1)
                    CurrentInventory.Add(new UICatalogElement { Item = deedWithObjects, Tag = (uint)2 });
            }

            foreach (var item in inventory)
            {
                if (item.ObjectPID == DragUID || Array.FindIndex(MyOffer.ObjectOffer, x=> x != null && x.PID == item.ObjectPID) > -1) continue;
                var catItem = Content.Content.Get().WorldCatalog.GetItemByGUID(item.GUID);
                if (catItem == null) { catItem = GenCatItem(item.GUID); }

                var obj = catItem.Value;
                //note that catalog items are structs, so we can modify their properties freely without affecting the permanant store.
                //todo: what if this is null? it shouldn't be, but still
                obj.Name = (item.Name == "") ? obj.Name : item.Name;
                obj.Price = 0;
                //todo: make icon for correct graphic.
                CurrentInventory.Add(new UICatalogElement { Item = obj, Tag = item.ObjectPID });
            }
            UpdateCatalog(); //refresh display
            SetPage(Math.Min(Catalog.TotalPages() - 1, lastCatPage));
        }

        private void BuildOffer(VMEODSecureTradeObject[] offer, UICatalog target)
        {
            var inventory = LotController.vm.MyInventory;

            var offercat = new List<UICatalogElement>();
            foreach (var item in offer)
            {
                ObjectCatalogItem? catItem;
                uint tag = 0;
                if (item == null || item.PID == DragUID || (item.LotID > 0 && DragUID > 0 && DragUID < 3))
                {
                    catItem = new ObjectCatalogItem() { GUID = uint.MaxValue };
                } else if (item.LotID > 0) {
                    var c = GenCatItem((uint)((item.GUID == 1) ? 0x3495FC60 : 0x34B4B46A));
                    
                    if (item.GUID == 2)
                        c.Name = item.LotName + " w/ " + item.ObjectCount + " objects ($" + item.ObjectValue + ")";
                    else
                        c.Name += " - " + item.LotName;
                    catItem = c;
                    tag = item.GUID;
                }
                else
                {
                    catItem = Content.Content.Get().WorldCatalog.GetItemByGUID(item.GUID);
                    if (catItem == null) { catItem = GenCatItem(item.GUID); }
                    tag = item.PID;
                }

                var obj = catItem.Value;
                obj.Price = 0;
                offercat.Add(new UICatalogElement { Item = obj, Tag = tag });
            }
            target.SetCategory(offercat);
        }

        private bool _InternalChange;
        public void SetPage(int page)
        {
            if (_InternalChange) return;
            _InternalChange = true;
            Catalog.SetPage(page);

            InventoryCatalogSecureTradingSlider.Value = page;
            _InternalChange = false;
        }

        private ObjectCatalogItem GenCatItem(uint GUID)
        {
            var obj = Content.Content.Get().WorldObjects.Get(GUID);
            if (obj == null)
            {
                return new ObjectCatalogItem()
                {
                    Name = "Unknown Object",
                    GUID = GUID
                };
            }
            else
            {
                //todo: get ctss?
                return new ObjectCatalogItem()
                {
                    Name = obj.OBJ.ChunkLabel,
                    GUID = GUID
                };
            }
        }

        public override void Update(UpdateState state)
        {
            var mouseDown = state.MouseState.LeftButton == ButtonState.Pressed;
            if (QueryPanel.Parent != this)
            {
                Remove(QueryPanel);
                Add(QueryPanel);
            }

            for (int i=0; i<NotifyRects.Count; i++)
            {
                var rect = NotifyRects[i];
                var time = rect.Item2 - 1f / (FSOEnvironment.RefreshRate * 4);
                if (time < 0)
                    NotifyRects.RemoveAt(i--);
                else
                    NotifyRects[i] = new Tuple<Rectangle, float>(rect.Item1, time);
            }

            if (TimeSinceType > 0) TimeSinceType--;
            else
            {
                var str = MyOffer.MoneyOffer.ToString();
                if (AmountEntry.CurrentText != str)
                {
                    _InternalChange = true;
                    AmountEntry.CurrentText = str;
                    _InternalChange = false;
                }
            }

            base.Update(state);
            bool refreshInventory = false;

            //are we currently moving an inventory item?
            if (DragItem != null)
            {
                DragItem.Position = GlobalPoint(state.MouseState.Position.ToVector2() - new Vector2(22, 22));
                if (!mouseDown)
                {
                    //try place the item down
                    var inventoryRect = LocalRect(Catalog.Position.X, Catalog.Position.Y, (Catalog.PageSize / 2) * 45, 80);
                    var myOfferRect = LocalRect(OfferCatalog.Position.X, OfferCatalog.Position.Y, (OfferCatalog.PageSize / 2) * 45, 80);

                    if (inventoryRect.Contains(state.MouseState.Position))
                    {
                        //remove from my offer, if it's present
                        var index = Array.FindIndex(MyOffer.ObjectOffer, x => x != null && (x.PID == DragUID || (x.GUID < 3 && x.GUID == DragUID)));
                        if (index != -1)
                        {
                            MyOffer.ObjectOffer[index] = null;
                            Send("trade_offer", "i0|"+index);
                        }
                    }
                    else if (myOfferRect.Contains(state.MouseState.Position))
                    {
                        //add to my offer, it it isn't present.
                        var index = Array.FindIndex(MyOffer.ObjectOffer, x => x != null && x.PID == DragUID);
                        if (index == -1)
                        {
                            var targ = Math.Min(4, (state.MouseState.Position.X - myOfferRect.X) / 45);
                            if (DragUID == 1 || DragUID == 2)
                            {
                                MyOffer.ObjectOffer[targ] =
                                    new VMEODSecureTradeObject(DragUID, 0, null);
                                Send("trade_offer", "p" + ((DragUID == 2)?"o":"n") + targ);
                            }
                            else
                            {
                                var obj = LastInventory.First(x => x.ObjectPID == DragUID);

                                MyOffer.ObjectOffer[targ] =
                                    new VMEODSecureTradeObject(obj.GUID, obj.ObjectPID, null);
                                Send("trade_offer", "i" + obj.ObjectPID + "|" + targ);
                            }
                        }
                    }

                    //do nothing, stop dragging
                    Remove(DragItem);
                    DragUID = 0;
                    DragItem = null;
                    BuildOffer(MyOffer.ObjectOffer, OfferCatalog);
                    refreshInventory = true;
                }
            }

            var inventory = LotController.vm.MyInventory;
            if (LastInventory != null)
            {
                if (LastInventory.Count != inventory.Count) refreshInventory = true;
                else
                {
                    for (int i = 0; i < inventory.Count; i++)
                    {
                        if (LastInventory[i] != inventory[i])
                        {
                            refreshInventory = true;
                            break;
                        }
                    }
                }
            }
            else { refreshInventory = true; }
            if (refreshInventory)
            {
                BuildInventory();
            }

            LastMouse = mouseDown;
        }


        protected virtual void InitEOD()
        {
            PlaintextHandlers["trade_show"] = P_Show;
            PlaintextHandlers["trade_message"] = P_Message;
            PlaintextHandlers["trade_time"] = P_Time;
            BinaryHandlers["trade_me"] = B_Me;
            BinaryHandlers["trade_other"] = B_Other;
        }

        private EODLiveModeOpt GetEODOptions()
        {
            return new EODLiveModeOpt()
            {
                Height = EODHeight.Trade,
                Length = EODLength.None,
            };
        }

        private void SetMyPerson(uint persist)
        {
            if (MyPerson != null)
            {
                if (MyPerson.Avatar.PersistID == persist) return;
                Remove(MyPerson);
            }
            var ava = LotController.vm.GetObjectByPersist(persist);
            if (ava == null) return;
            MyPerson = new UIVMPersonButton((VMAvatar)ava, LotController.vm, true);
            MyPerson.Position = new Vector2(43, 134);
            Add(MyPerson);
        }

        private void SetOtherPerson(uint persist)
        {
            if (OtherPerson != null)
            {
                if (OtherPerson.Avatar.PersistID == persist) return;
                Remove(OtherPerson);
            }
            OtherPerson = new UIVMPersonButton((VMAvatar)LotController.vm.GetObjectByPersist(persist), LotController.vm, true);
            OtherPerson.Position = new Vector2(43, 184);
            Add(OtherPerson);
        }

        protected virtual void P_Show(string evt, string txt)
        {
            var options = GetEODOptions();
            Controller.ShowEODMode(options);
        }

        protected virtual void P_Message(string evt, string txt)
        {
            var msg = txt.Split('|');
            if (msg.Length < 2) return;
            var title = GameFacade.Strings.GetString("224", msg[0]);
            if (title.Contains('*')) title = "";
            var body = GameFacade.Strings.GetString("224", msg[1]);
            UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = title,
                Message = body
            }, true);
        }

        protected virtual void P_Time(string evt, string txt)
        {
            int timeNum;
            if (!int.TryParse(txt, out timeNum)) return;
            AcceptButton.Disabled = (timeNum != 0);
            AcceptButton.ForceState = (timeNum != 0)?0:-1;

            LockoutTimerLabel.Visible = (timeNum != 0);
            LockoutTimerLabel.Caption = "("+timeNum+")";
        }

        private void NotifyChange(VMEODSecureTradePlayer prev, VMEODSecureTradePlayer now, Vector2 position)
        {
            if (prev.MoneyOffer != now.MoneyOffer)
            {
                //money changed
                NotifyRects.Add(new Tuple<Rectangle, float>(new Rectangle(position.ToPoint() + new Point((Small800)?229:453, 7), new Point(102, 29)), 1.0f));
            }

            for (int i=0; i<5; i++)
            {
                var pobj = prev.ObjectOffer[i];
                var nobj = now.ObjectOffer[i];
                if (((nobj == null) != (pobj == null)) || (nobj != null && (nobj.GUID != pobj.GUID || nobj.PID != pobj.PID)))
                {
                    NotifyRects.Add(new Tuple<Rectangle, float>(new Rectangle(position.ToPoint() + new Point(i*45+1, 2), new Point(43, 43)), 1.0f));
                }
            }
        }

        protected virtual void B_Me(string evt, byte[] txt)
        {
            try
            {
                var data = new VMEODSecureTradePlayer(txt);

                NotifyChange(MyOffer, data, OfferCatalog.Position);
                
                MyOffer = data;
                SetMyPerson(data.PlayerPersist);
                BuildInventory();
                BuildOffer(MyOffer.ObjectOffer, OfferCatalog);

                //our payout money is synced a second after we stop typing.
                AcceptButton.Selected = MyOffer.Accepted;
            }
            catch (Exception) { }
        }

        protected virtual void B_Other(string evt, byte[] txt)
        {
            try
            {
                var data = new VMEODSecureTradePlayer(txt);

                NotifyChange(OtherOffer, data, OtherOfferCatalog.Position);

                OtherOffer = data;
                SetOtherPerson(data.PlayerPersist);
                BuildOffer(OtherOffer.ObjectOffer, OtherOfferCatalog);

                OtherAvatarAmount.CurrentText = data.MoneyOffer.ToString();
                OtherAcceptedButton.ForceState = data.Accepted?3:0;
            }
            catch (Exception) { }
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);

            var white = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
            foreach (var notify in NotifyRects)
            {
                var intensity = (float)(1-Math.Pow((1 - notify.Item2), 1 / 5f));
                var rect = notify.Item1;
                DrawLocalTexture(batch, white, null, rect.Location.ToVector2(), rect.Size.ToVector2(), Color.Yellow * intensity);
            }
        }
    }
}
