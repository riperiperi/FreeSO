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
using Microsoft.Xna.Framework;
using FSO.Content;
using FSO.Content.Interfaces;

namespace FSO.Client.UI.Panels
{
    public class UIBuyMode : UIAbstractCatalogPanel
    {
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

        private List<VMInventoryItem> LastInventory;
        private List<UICatalogElement> CurrentInventory;
        
        private bool RoomCategories = false;
        private bool Roommate = true; //if false, shows visitor inventory only.
        private int Mode = 0;

        public UIBuyMode(UILotControl lotController) : base("buypanel", lotController) {

            InventoryButtonBackgroundImage = Script.Create<UIImage>("InventoryButtonBackgroundImage");
            this.AddAt(1, InventoryButtonBackgroundImage);
            
            CatBg = Script.Create<UIImage>("ProductCatalogImage");
            this.AddAt(2, CatBg);

            InventoryCatBg = Script.Create<UIImage>("InventoryCatalogRoommateImage");
            this.AddAt(3, InventoryCatBg);

            NonRMInventoryCatBg = Script.Create<UIImage>("InventoryCatalogVisitorImage");
            this.AddAt(4, NonRMInventoryCatBg);

            InventoryCatalogVisitorIcon = Script.Create<UIImage>("InventoryCatalogVisitorIcon");
            this.AddAt(5, InventoryCatalogVisitorIcon);

            Catalog.Position = new Microsoft.Xna.Framework.Vector2(275, 7);

            //MapBuildingModeButton.OnButtonClick += ChangeCategory;
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
        }

        public override void InitCategoryMap()
        {
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
        }

        public override void Update(UpdateState state)
        {
            var objCount = LotController.vm.Context.ObjectQueries.NumUserObjects;
            if (LastObjCount != objCount || LastDonator != LotController.ObjectHolder.DonateMode)
            {
                if (LastDonator != LotController.ObjectHolder.DonateMode)
                {
                    if (CurrentInventory != null && CurrentCategory == CurrentInventory && LotController.ObjectHolder.DonateMode)
                        UIAlert.Alert(GameFacade.Strings.GetString("f114", "2"), GameFacade.Strings.GetString("f114", "3"), true);
                    Catalog.SetPage(Catalog.Page); //update prices
                }
                if (LotController.ObjectHolder.DonateMode)
                {
                    ObjLimitLabel.Caption = GameFacade.Strings.GetString("f114", "4");
                    ObjLimitLabel.CaptionStyle.Color = new Color(255, 201, 38);
                }
                else
                {
                    var limit = LotController.vm.TSOState.ObjectLimit;
                    ObjLimitLabel.Caption = objCount + "/" + limit + " Objects";
                    var lerp = objCount / (float)limit;
                    if (lerp < 0.5)
                        ObjLimitLabel.CaptionStyle.Color = Color.White;
                    if (lerp < 0.75)
                        ObjLimitLabel.CaptionStyle.Color = Color.Lerp(Color.White, new Color(255, 201, 38), lerp * 4 - 2);
                    else
                        ObjLimitLabel.CaptionStyle.Color = Color.Lerp(new Color(255, 201, 38), Color.Red, lerp * 4 - 3);
                }
                LastObjCount = objCount;
                LastDonator = LotController.ObjectHolder.DonateMode;
            }

            if (LotController.ActiveEntity != null)
            {
                Catalog.Budget = (int)LotController.Budget;
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
                    var lastCatPage = Catalog.GetPage();
                    LastInventory = new List<VMInventoryItem>(inventory);
                    if (CurrentInventory == null) CurrentInventory = new List<UICatalogElement>();
                    CurrentInventory.Clear();
                    foreach (var item in inventory)
                    {
                        var catItem = Content.Content.Get().WorldCatalog.GetItemByGUID(item.GUID);
                        if (catItem == null) { catItem = GenCatItem(item.GUID); }

                        var obj = catItem.Value;
                        //note that catalog items are structs, so we can modify their properties freely without affecting the permanant store.
                        //todo: what if this is null? it shouldn't be, but still
                        obj.Name = (item.Name == "")?obj.Name:item.Name;
                        obj.Price = 0;
                        //todo: make icon for correct graphic.
                        CurrentInventory.Add(new UICatalogElement { Item = obj });
                    }
                    if (Mode == 2)
                    {
                        ChangeCategory(InventoryButton); //refresh display
                        SetPage(Math.Min(Catalog.TotalPages()-1, lastCatPage));
                    }
                }
            }
            base.Update(state);
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
            } else
            {
                //todo: get ctss?
                return new ObjectCatalogItem()
                {
                    Name = obj.OBJ.ChunkLabel,
                    GUID = GUID
                };
            }
        }

        override protected void Catalog_OnSelectionChange(int selection)
        {
            base.Catalog_OnSelectionChange(selection);
            if (CurrentCategory == CurrentInventory)
            {
                if (selection < LastInventory.Count && Holder.Holding != null)
                {
                    Holder.Holding.InventoryPID = LastInventory[selection].ObjectPID;
                    Holder.Holding.Price = 0;
                }
            }
        }

        public override void SetPage(int page)
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

        public override void ChangeCategory(UIElement elem)
        {
            foreach (var btn in CategoryMap.Keys)
                btn.Selected = false;
            InventoryButton.Selected = false;

            UIButton button = (UIButton)elem;
            button.Selected = true;
            SetMode((elem == InventoryButton) ? 2 : 1);
            if (elem == InventoryButton)
            {
                if (CurrentCategory != CurrentInventory && LotController.ObjectHolder.DonateMode)
                {
                    UIAlert.Alert(GameFacade.Strings.GetString("f114", "2"), GameFacade.Strings.GetString("f114", "3"), true);
                }
                CurrentCategory = CurrentInventory;
            }
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
            QueryPanel.InInventory = (mode == 2) ? 1 : 0;
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

            var useSmall = UseSmall;

            if (mode == 1) { Catalog.X = 275; Catalog.PageSize = (useSmall)?14:24; }
            else if (mode == 2 && Roommate) { Catalog.X = 272; Catalog.PageSize = (useSmall) ? 14 : 24; }
            else if (mode == 2 && !Roommate) { Catalog.X = 98; Catalog.PageSize = (useSmall) ? 22 : 30; }

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
