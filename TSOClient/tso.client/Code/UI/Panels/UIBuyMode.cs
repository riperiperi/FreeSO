using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Controls;
using Microsoft.Xna.Framework.Graphics;
using TSO.Simantics;
using TSOClient.LUI;
using TSOClient.Code.UI.Controls.Catalog;
using tso.world.model;
using TSO.Simantics.entities;

namespace TSOClient.Code.UI.Panels
{
    public class UIBuyMode : UIContainer
    {
        public UIImage Background;
        public Texture2D catalogBackground { get; set; }
        public Texture2D inventoryRoommateBackground { get; set; }
        public Texture2D inventoryVisitorBackground { get; set; }

        public VM vm;
        public VMAvatar SelectedAvatar;

        //roommate catalog elements
        public UIImage CatBg;
        public UISlider ProductCatalogSlider { get; set; }
        public UIButton ProductCatalogPreviousPageButton { get; set; } //that's a mouthful
        public UIButton ProductCatalogNextPageButton { get; set; }

        //roommate inventory catalog elements
        public UIImage InventoryCatBg;
        public UISlider InventoryCatalogRoommateSlider { get; set; }
        public UIButton InventoryCatalogRoommatePreviousPageButton { get; set; } //that's a mouthful
        public UIButton InventoryCatalogRoommateNextPageButton { get; set; }

        //non-roommate inventory catalog elements
        public UIImage NonRMInventoryCatBg;
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

        public UIButton MapBuildingModeButton { get; set; }
        public UIButton PetsButton { get; set; }

        public UICatalog Catalog;
        public UIObjectHolder Holder;
        private VMMultitileGroup BuyItem;

        private Dictionary<UIButton, int> CategoryMap;
        private List<UICatalogElement> CurrentCategory;

        private bool RoomCategories = false;
        private bool Roommate = true; //if false, shows visitor inventory only.
        private int Mode = 0;

        public UIBuyMode() {
            var script = this.RenderScript("buypanel"+((GlobalSettings.Default.GraphicsWidth < 1024)?"":"1024")+".uis");

            Background = new UIImage(GetTexture((GlobalSettings.Default.GraphicsWidth < 1024) ? (ulong)0x000000D800000002 : (ulong)0x0000018300000002));
            Background.Y = 0;
            this.AddAt(0, Background);

            CatBg = new UIImage(catalogBackground);
            CatBg.Position = new Microsoft.Xna.Framework.Vector2(250, 5);
            this.AddAt(1, CatBg);

            InventoryCatBg = new UIImage(inventoryRoommateBackground);
            InventoryCatBg.Position = new Microsoft.Xna.Framework.Vector2(250, 5);
            this.AddAt(2, InventoryCatBg);

            NonRMInventoryCatBg = new UIImage(inventoryVisitorBackground);
            NonRMInventoryCatBg.Position = new Microsoft.Xna.Framework.Vector2(68, 5);
            this.AddAt(3, InventoryCatBg);

            Catalog = new UICatalog((GlobalSettings.Default.GraphicsWidth < 1024) ? 14 : 24);
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

            SeatingButton.OnButtonClick += new ButtonClickDelegate(ChangeCategory);
            SurfacesButton.OnButtonClick += new ButtonClickDelegate(ChangeCategory);
            DecorativeButton.OnButtonClick += new ButtonClickDelegate(ChangeCategory);
            ElectronicsButton.OnButtonClick += new ButtonClickDelegate(ChangeCategory);
            AppliancesButton.OnButtonClick += new ButtonClickDelegate(ChangeCategory);
            SkillButton.OnButtonClick += new ButtonClickDelegate(ChangeCategory);
            LightingButton.OnButtonClick += new ButtonClickDelegate(ChangeCategory);
            MiscButton.OnButtonClick += new ButtonClickDelegate(ChangeCategory);
            PetsButton.OnButtonClick += new ButtonClickDelegate(ChangeCategory);
            MapBuildingModeButton.OnButtonClick += new ButtonClickDelegate(ChangeCategory);

            ProductCatalogPreviousPageButton.OnButtonClick += new ButtonClickDelegate(PreviousPage);
            InventoryCatalogRoommatePreviousPageButton.OnButtonClick += new ButtonClickDelegate(PreviousPage);
            InventoryCatalogVisitorPreviousPageButton.OnButtonClick += new ButtonClickDelegate(PreviousPage);

            ProductCatalogNextPageButton.OnButtonClick += new ButtonClickDelegate(NextPage);
            InventoryCatalogRoommateNextPageButton.OnButtonClick += new ButtonClickDelegate(NextPage);
            InventoryCatalogVisitorNextPageButton.OnButtonClick += new ButtonClickDelegate(NextPage);

            ProductCatalogSlider.MinValue = 0;
            InventoryCatalogRoommateSlider.MinValue = 0;
            InventoryCatalogVisitorSlider.MinValue = 0;

            ProductCatalogSlider.OnChange += new ChangeDelegate(PageSlider);
            InventoryCatalogRoommateSlider.OnChange += new ChangeDelegate(PageSlider);
            InventoryCatalogVisitorSlider.OnChange += new ChangeDelegate(PageSlider);

            SetMode(0);
            SetRoomCategories(false);
        }

        void Catalog_OnSelectionChange(int selection)
        {
            if (BuyItem != null && Holder.Holding != null && BuyItem == Holder.Holding.Group) {
                foreach (var obj in BuyItem.Objects) {
                    vm.Context.RemoveObjectInstance(obj);
                }
            }
            BuyItem = vm.Context.CreateObjectInstance(CurrentCategory[selection].GUID, new LotTilePos(0, 0, 1), Direction.NORTH);
            Holder.SetSelected(BuyItem);
            //var pos = vm.Context.World.State.CenterTile;
            
            //vm.Context.CreateObjectInstance(CurrentCategory[selection].GUID, (short)pos.X, (short)pos.Y, 1, (Direction)(1<<(((6-(int)vm.Context.World.State.Rotation)%4) * 2)));
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

            UIButton button = (UIButton)elem;
            button.Selected = true;
            if (!CategoryMap.ContainsKey(button)) return;
            CurrentCategory = UICatalog.Catalog[CategoryMap[button]];
            Catalog.SetCategory(CurrentCategory);

            int total = Catalog.TotalPages();

            ProductCatalogSlider.MaxValue = total - 1;
            ProductCatalogSlider.Value = 0;


            ProductCatalogSlider.MaxValue = total - 1;
            ProductCatalogSlider.Value = 0;


            ProductCatalogSlider.MaxValue = total - 1;
            ProductCatalogSlider.Value = 0;

            ProductCatalogNextPageButton.Disabled = (total == 1);
            InventoryCatalogRoommateNextPageButton.Disabled = (total == 1);
            InventoryCatalogVisitorNextPageButton.Disabled = (total == 1);

            ProductCatalogPreviousPageButton.Disabled = true;
            InventoryCatalogRoommatePreviousPageButton.Disabled = true;
            InventoryCatalogVisitorPreviousPageButton.Disabled = true;

            SetMode(1);
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
            InventoryCatalogVisitorSlider.Visible = (mode == 2 && !Roommate);
            InventoryCatalogVisitorNextPageButton.Visible = (mode == 2 && !Roommate);
            InventoryCatalogVisitorPreviousPageButton.Visible = (mode == 2 && !Roommate);

            Mode = mode;
        }

        public void SetRoommate(bool value)
        {
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
