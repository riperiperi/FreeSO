/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework.Graphics;
using FSO.SimAntics;
using FSO.Client.UI.Controls.Catalog;
using FSO.LotView.Model;
using FSO.SimAntics.Entities;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework.Input;
using FSO.SimAntics.Model;
using FSO.Common;

namespace FSO.Client.UI.Panels
{
    public class UIBuyMode : UIDestroyablePanel
    {
        public UIImage Background;
        public Texture2D catalogBackground { get; set; }
        public Texture2D inventoryRoommateBackground { get; set; }
        public Texture2D inventoryVisitorBackground { get; set; }

        public VM vm;

        //roommate catalog elements
        public UIImage CatBg;
        public UISlider ProductCatalogSlider { get; set; }
        public UIButton ProductCatalogPreviousPageButton { get; set; } //that's a mouthful
        public UIButton ProductCatalogNextPageButton { get; set; }

        //roommate inventory catalog elements
        public UIImage InventoryCatBg;
        public UIImage InventoryButtonBackgroundImage { get; set; }
        public UISlider InventoryCatalogRoommateSlider { get; set; }
        public UIButton InventoryCatalogRoommatePreviousPageButton { get; set; } //that's a mouthful
        public UIButton InventoryCatalogRoommateNextPageButton { get; set; }

        //non-roommate inventory catalog elements
        public UIImage NonRMInventoryCatBg;
        public UIImage InventoryCatalogVisitorIcon;
        public UISlider InventoryCatalogVisitorSlider { get; set; }
        public UIButton InventoryCatalogVisitorPreviousPageButton { get; set; } //that's a mouthful
        public UIButton InventoryCatalogVisitorNextPageButton { get; set; }

        public UIButton SeatingButton { get; set; }
        public UIButton SurfacesButton { get; set; }
        public UIButton DecorativeButton { get; set; }
        public UIButton ElectronicsButton { get; set; }
        public UIButton AppliancesButton { get; set; }
        public UIButton SkillButton { get; set; }
        public UIButton LightingButton { get; set; }
        public UIButton MiscButton { get; set; }

        public UIButton LivingRoomButton { get; set; }
        public UIButton DiningRoomButton { get; set; }
        public UIButton BedroomButton { get; set; }
        public UIButton StudyRoomButton { get; set; }
        public UIButton KitchenButton { get; set; }
        public UIButton BathRoomButton { get; set; }
        public UIButton OutsideButton { get; set; }
        public UIButton MiscRoomButton { get; set; }
        public UIButton InventoryButton { get; set; }

        public UIButton MapBuildingModeButton { get; set; }
        public UIButton PetsButton { get; set; }

        public UICatalog Catalog;
        public UIObjectHolder Holder;
        public UIQueryPanel QueryPanel;
        public UILotControl LotController;
        private VMMultitileGroup BuyItem;

        private Dictionary<UIButton, int> CategoryMap;
        private List<UICatalogElement> CurrentCategory;
        private List<VMInventoryItem> LastInventory;
        private List<UICatalogElement> CurrentInventory;

        private bool RoomCategories = false;
        private bool Roommate = true; //if false, shows visitor inventory only.
        private int Mode = 0;
        private int OldSelection = -1;

        public UIBuyMode(UILotControl lotController) {

            LotController = lotController;
            Holder = LotController.ObjectHolder;
            QueryPanel = LotController.QueryPanel;

            var useSmall = (FSOEnvironment.UIZoomFactor > 1f || GlobalSettings.Default.GraphicsWidth < 1024);
            var script = this.RenderScript("buypanel"+(useSmall?"":"1024")+".uis");

            Background = new UIImage(GetTexture(useSmall ? (ulong)0x000000D800000002 : (ulong)0x0000018300000002));
            Background.Y = 0;
            Background.BlockInput();
            this.AddAt(0, Background);

            InventoryButtonBackgroundImage = script.Create<UIImage>("InventoryButtonBackgroundImage");
            this.AddAt(1, InventoryButtonBackgroundImage);
            
            CatBg = script.Create<UIImage>("ProductCatalogImage");
            this.AddAt(2, CatBg);

            InventoryCatBg = script.Create<UIImage>("InventoryCatalogRoommateImage");
            this.AddAt(3, InventoryCatBg);

            NonRMInventoryCatBg = script.Create<UIImage>("InventoryCatalogVisitorImage");
            this.AddAt(4, NonRMInventoryCatBg);

            InventoryCatalogVisitorIcon = script.Create<UIImage>("InventoryCatalogVisitorIcon");
            this.AddAt(5, InventoryCatalogVisitorIcon);

            Catalog = new UICatalog(useSmall ? 14 : 24);
            Catalog.OnSelectionChange += new CatalogSelectionChangeDelegate(Catalog_OnSelectionChange);
            Catalog.Position = new Microsoft.Xna.Framework.Vector2(275, 7);
            this.Add(Catalog);

            CategoryMap = new Dictionary<UIButton, int>
            {
                { SeatingButton, 12 },
                { SurfacesButton, 13 },
                { AppliancesButton, 14 },
                { ElectronicsButton, 15 },
                { SkillButton, 16 },
                { DecorativeButton, 17 },
                { MiscButton, 18 },
                { LightingButton, 19 },
                { PetsButton, 20 },
            };

            SeatingButton.OnButtonClick += ChangeCategory;
            SurfacesButton.OnButtonClick += ChangeCategory;
            DecorativeButton.OnButtonClick += ChangeCategory;
            ElectronicsButton.OnButtonClick += ChangeCategory;
            AppliancesButton.OnButtonClick += ChangeCategory;
            SkillButton.OnButtonClick += ChangeCategory;
            LightingButton.OnButtonClick += ChangeCategory;
            MiscButton.OnButtonClick += ChangeCategory;
            PetsButton.OnButtonClick += ChangeCategory;
            MapBuildingModeButton.OnButtonClick += ChangeCategory;
            InventoryButton.OnButtonClick += ChangeCategory;

            ProductCatalogPreviousPageButton.OnButtonClick += PreviousPage;
            InventoryCatalogRoommatePreviousPageButton.OnButtonClick += PreviousPage;
            InventoryCatalogVisitorPreviousPageButton.OnButtonClick += PreviousPage;

            ProductCatalogNextPageButton.OnButtonClick += NextPage;
            InventoryCatalogRoommateNextPageButton.OnButtonClick += NextPage;
            InventoryCatalogVisitorNextPageButton.OnButtonClick += NextPage;

            ProductCatalogSlider.MinValue = 0;
            InventoryCatalogRoommateSlider.MinValue = 0;
            InventoryCatalogVisitorSlider.MinValue = 0;

            ProductCatalogSlider.OnChange += PageSlider;
            InventoryCatalogRoommateSlider.OnChange += PageSlider;
            InventoryCatalogVisitorSlider.OnChange += PageSlider;

            SetMode(0);
            SetRoomCategories(false);

            Holder.OnPickup += HolderPickup;
            Holder.OnDelete += HolderDelete;
            Holder.OnPutDown += HolderPutDown;
            Add(QueryPanel);
        }

        public override void Destroy()
        {
            //clean up loose ends
            Holder.OnPickup -= HolderPickup;
            Holder.OnDelete -= HolderDelete;
            Holder.OnPutDown -= HolderPutDown;

            if (Holder.Holding != null)
            {
                //delete object that hasn't been placed yet
                //TODO: all holding objects should obviously just be ghosts.
                //Holder.Holding.Group.Delete(vm.Context);
                Holder.ClearSelected();
                QueryPanel.Active = false;
            }
        }

        private void HolderPickup(UIObjectSelection holding, UpdateState state)
        {
            QueryPanel.Mode = 0;
            QueryPanel.Active = true;
            QueryPanel.SetInfo(LotController.vm, holding.RealEnt ?? holding.Group.BaseObject, holding.IsBought);
            QueryPanel.Tab = 1;
        }
        private void HolderPutDown(UIObjectSelection holding, UpdateState state)
        {
            if (OldSelection != -1)
            {
                if (!holding.IsBought && holding.InventoryPID == 0 && (state.KeyboardState.IsKeyDown(Keys.LeftShift) || state.KeyboardState.IsKeyDown(Keys.RightShift))) {
                    //place another
                    var prevDir = holding.Dir;
                    Catalog_OnSelectionChange(OldSelection);
                    if (Holder.Holding != null) Holder.Holding.Dir = prevDir;
                } else {
                    Catalog.SetActive(OldSelection, false);
                    OldSelection = -1;
                }
            }
            QueryPanel.Active = false;
        }

        private void HolderDelete(UIObjectSelection holding, UpdateState state)
        {
            if (OldSelection != -1)
            {
                Catalog.SetActive(OldSelection, false);
                OldSelection = -1;
            }
            QueryPanel.Active = false;
        }

        public override void Update(UpdateState state)
        {

            if (LotController.ActiveEntity != null)
            {
                Catalog.Budget = (int)LotController.ActiveEntity.TSOState.Budget.Value;
                bool refreshInventory = false;
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
                } else { refreshInventory = true; }
                if (refreshInventory)
                {
                    LastInventory = new List<VMInventoryItem>(inventory);
                    if (CurrentInventory == null) CurrentInventory = new List<UICatalogElement>();
                    CurrentInventory.Clear();
                    foreach (var item in inventory)
                    {
                        var obj = Content.Content.Get().WorldCatalog.GetItemByGUID(item.GUID).Value;
                        //note that catalog items are structs, so we can modify their properties freely without affecting the permanant store.
                        //todo: what if this is null? it shouldn't be, but still
                        obj.Name = (item.Name == "")?obj.Name:item.Name;
                        obj.Price = 0;
                        //todo: make icon for correct graphic.
                        CurrentInventory.Add(new UICatalogElement { Item = obj });
                    }
                    if (Mode == 2) ChangeCategory(InventoryButton); //refresh display
                }
            }
            base.Update(state);
        }

        void Catalog_OnSelectionChange(int selection)
        {
            var item = CurrentCategory[selection];

            if (LotController.ActiveEntity != null && item.Item.Price > LotController.ActiveEntity.TSOState.Budget.Value)
            {
                HIT.HITVM.Get().PlaySoundEvent(Model.UISounds.Error);
                return;
            }

            if (OldSelection != -1) Catalog.SetActive(OldSelection, false);
            Catalog.SetActive(selection, true);
            BuyItem = LotController.vm.Context.CreateObjectInstance(item.Item.GUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH, true);
            if (BuyItem == null) return; //uh
            QueryPanel.SetInfo(LotController.vm, BuyItem.Objects[0], false);
            QueryPanel.Mode = 1;
            QueryPanel.Tab = 0;
            QueryPanel.Active = true;
            Holder.SetSelected(BuyItem);
            if (CurrentCategory == CurrentInventory)
            {
                if (selection < LastInventory.Count)
                {
                    Holder.Holding.InventoryPID = LastInventory[selection].ObjectPID;
                    Holder.Holding.Price = 0;
                }
            }
            OldSelection = selection;
        }

        public void PageSlider(UIElement element)
        {
            var slider = (UISlider)element;
            SetPage((int)Math.Round(slider.Value));
        }

        public void SetPage(int page)
        {
            bool noPrev = (page == 0);
            ProductCatalogPreviousPageButton.Disabled = noPrev;
            InventoryCatalogRoommatePreviousPageButton.Disabled = noPrev;
            InventoryCatalogVisitorPreviousPageButton.Disabled = noPrev;

            bool noNext = (page + 1 == Catalog.TotalPages());
            ProductCatalogNextPageButton.Disabled = noNext;
            InventoryCatalogRoommateNextPageButton.Disabled = noNext;
            InventoryCatalogVisitorNextPageButton.Disabled = noNext;

            Catalog.SetPage(page);
            if (OldSelection != -1) Catalog.SetActive(OldSelection, true);

            ProductCatalogSlider.Value = page;
            InventoryCatalogRoommateSlider.Value = page;
            InventoryCatalogVisitorSlider.Value = page;
        }

        public void PreviousPage(UIElement button)
        {
            int page = Catalog.GetPage();
            if (page == 0) return;
            SetPage(page - 1);
        }

        public void NextPage(UIElement button)
        {
            int page = Catalog.GetPage();
            int totalPages = Catalog.TotalPages();
            if (page+1 == totalPages) return;
            SetPage(page + 1);
        }

        public void ChangeCategory(UIElement elem)
        {
            SeatingButton.Selected = false;
            SurfacesButton.Selected = false;
            DecorativeButton.Selected = false;
            ElectronicsButton.Selected = false;
            AppliancesButton.Selected = false;
            SkillButton.Selected = false;
            LightingButton.Selected = false;
            MiscButton.Selected = false;
            PetsButton.Selected = false;
            InventoryButton.Selected = false;

            UIButton button = (UIButton)elem;
            button.Selected = true;
            SetMode((elem == InventoryButton) ? 2 : 1);
            if (elem == InventoryButton) CurrentCategory = CurrentInventory;
            else
            {
                if (!CategoryMap.ContainsKey(button)) return;
                CurrentCategory = UICatalog.Catalog[CategoryMap[button]];
            }
            Catalog.SetCategory(CurrentCategory);

            int total = Catalog.TotalPages();
            OldSelection = -1;

            ProductCatalogSlider.MaxValue = total - 1;
            ProductCatalogSlider.Value = 0;

            InventoryCatalogRoommateSlider.MaxValue = total - 1;
            InventoryCatalogRoommateSlider.Value = 0;

            InventoryCatalogVisitorSlider.MaxValue = total - 1;
            InventoryCatalogVisitorSlider.Value = 0;

            ProductCatalogNextPageButton.Disabled = (total == 1);
            InventoryCatalogRoommateNextPageButton.Disabled = (total == 1);
            InventoryCatalogVisitorNextPageButton.Disabled = (total == 1);

            ProductCatalogPreviousPageButton.Disabled = true;
            InventoryCatalogRoommatePreviousPageButton.Disabled = true;
            InventoryCatalogVisitorPreviousPageButton.Disabled = true;

            return;
        }

        public void SetMode(int mode)
        {
            if (!Roommate) mode = 2;
            CatBg.Visible = (mode == 1);
            ProductCatalogSlider.Visible = (mode == 1);
            ProductCatalogNextPageButton.Visible = (mode == 1);
            ProductCatalogPreviousPageButton.Visible = (mode == 1);

            InventoryCatBg.Visible = (mode == 2 && Roommate);
            InventoryCatalogRoommateSlider.Visible = (mode == 2 && Roommate);
            InventoryCatalogRoommateNextPageButton.Visible = (mode == 2 && Roommate);
            InventoryCatalogRoommatePreviousPageButton.Visible = (mode == 2 && Roommate);

            NonRMInventoryCatBg.Visible = (mode == 2 && !Roommate);
            InventoryCatalogVisitorIcon.Visible = (mode == 2 && !Roommate);
            InventoryCatalogVisitorSlider.Visible = (mode == 2 && !Roommate);
            InventoryCatalogVisitorNextPageButton.Visible = (mode == 2 && !Roommate);
            InventoryCatalogVisitorPreviousPageButton.Visible = (mode == 2 && !Roommate);

            if (mode == 1) { Catalog.X = 275; Catalog.PageSize = 24; }
            else if (mode == 2 && Roommate) { Catalog.X = 272; Catalog.PageSize = 24; }
            else if (mode == 2 && !Roommate) { Catalog.X = 98; Catalog.PageSize = 30; }

            Catalog.SetPage(0);

            Mode = mode;
        }

        public void SetRoommate(bool value)
        {
            LotController.ObjectHolder.Roommate = value;
            LotController.QueryPanel.Roommate = value;
            if (Roommate == value) return;
            Roommate = value;
            SetMode(Mode);
            SetRoomCategories(RoomCategories);
        }

        public void SetRoomCategories(bool value) {
            bool active = Roommate && (!value);
            SeatingButton.Visible = active;
            SurfacesButton.Visible = active;
            DecorativeButton.Visible = active;
            ElectronicsButton.Visible = active;
            AppliancesButton.Visible = active;
            SkillButton.Visible = active;
            LightingButton.Visible = active;
            MiscButton.Visible = active;
            PetsButton.Visible = active;
            InventoryButton.Visible = active;
            MapBuildingModeButton.Visible = false;

            active = Roommate && (value);
            LivingRoomButton.Visible = active;
            DiningRoomButton.Visible = active;
            BedroomButton.Visible = active;
            StudyRoomButton.Visible = active;
            KitchenButton.Visible = active;
            BathRoomButton.Visible = active;
            OutsideButton.Visible = active;
            MiscRoomButton.Visible = active;

            RoomCategories = value;
        }
    }
}
