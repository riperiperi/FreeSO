using FSO.Client.UI.Controls;
using FSO.Client.UI.Controls.Catalog;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.UI.Model;
using FSO.Client.UI.Panels.LotControls;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.LotView.Model;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;

namespace FSO.Client.UI.Panels
{
    public abstract class UIAbstractCatalogPanel : UICachedContainer
    {
        public UIImage Background;
        public UICatalog Catalog;
        public UIObjectHolder Holder;
        public UIQueryPanel QueryPanel { get { return LotController.QueryPanel; } }
        public UILotControl LotController;
        protected VMMultitileGroup BuyItem;

        protected UILabel ObjLimitLabel;
        protected int LastObjCount = -1;
        protected bool LastDonator;
        protected int OldSelection = -1;
        protected UIScript Script;

        protected Dictionary<UIButton, int> CategoryMap;
        protected List<UICatalogElement> CurrentCategory;

        protected Dictionary<uint, byte> UpgradeLevelMemory = new Dictionary<uint, byte>();

        protected UIButton SearchButton;
        protected UICatalogSearchPanel SearchPanel;

        protected bool UseSmall;
        public UIAbstractCatalogPanel(string mode, UILotControl lotController)
        {
            LotController = lotController;
            Holder = LotController.ObjectHolder;

            var useSmall = (FSOEnvironment.UIZoomFactor > 1f || GlobalSettings.Default.GraphicsWidth < 1024);
            UseSmall = useSmall;
            var script = this.RenderScript(mode + (useSmall ? "" : "1024") + ".uis");
            Script = script;

            Background = new UIImage(GetTexture(useSmall ? (ulong)0x000000D800000002 : (ulong)0x0000018300000002));
            Background.Y = 0;
            Background.BlockInput();
            this.AddAt(0, Background);
            Size = Background.Size;

            Catalog = new UICatalog((mode == "buildpanel") ? (useSmall ? 10 : 20) : (useSmall ? 14 : 24));
            Catalog.LotControl = lotController;
            Catalog.OnSelectionChange += new CatalogSelectionChangeDelegate(Catalog_OnSelectionChange);

            this.Add(Catalog);

            //prepare catalog map
            InitCategoryMap();
            foreach (UIButton btn in CategoryMap.Keys)
            {
                btn.OnButtonClick += ChangeCategory;
            }

            Holder.OnPickup += HolderPickup;
            Holder.OnDelete += HolderDelete;
            Holder.OnPutDown += HolderPutDown;
            Holder.BeforeRelease += HolderBeforeRelease;
            DynamicOverlay.Add(QueryPanel);

            ObjLimitLabel = new UILabel();
            ObjLimitLabel.CaptionStyle = ObjLimitLabel.CaptionStyle.Clone();
            ObjLimitLabel.CaptionStyle.Shadow = true;
            ObjLimitLabel.CaptionStyle.Color = Microsoft.Xna.Framework.Color.White;
            ObjLimitLabel.Caption = "127/250 Objects";
            ObjLimitLabel.Y = -20;
            ObjLimitLabel.X = Background.Width / 2 - 100;
            ObjLimitLabel.Size = new Microsoft.Xna.Framework.Vector2(200, 0);
            ObjLimitLabel.Alignment = TextAlignment.Center;
            DynamicOverlay.Add(ObjLimitLabel);

            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            SearchPanel = new UICatalogSearchPanel(this);
            SearchPanel.XOffset = Background.Width - 259;
            SearchPanel.OnUpdate += SearchUpdated;

            SearchButton = new UIButton(ui.Get("cat_search.png").Get(gd));
            SearchButton.Y = 8;
            SearchButton.X = Background.Width - (6 + 13);
            SearchButton.OnButtonClick += (UIElement btn) => { SearchButton.Selected = SearchPanel.Toggle(); };
            this.Add(SearchButton);
        }

        private void SearchUpdated(string term)
        {
            Catalog.SetSearchTerm(term);
            SetPage(0);
            Invalidate();
        }

        private void HolderBeforeRelease(UIObjectSelection holding, UpdateState state)
        {
            // remember the upgrade level between entering the catalog
            if (!holding.IsBought)
            {
                var guid = holding.Group.GUID;
                var baseObj = holding.Group.BaseObject;
                var level = (baseObj.PlatformState as VMTSOObjectState)?.UpgradeLevel ?? 0;
                UpgradeLevelMemory[guid] = level;
            }
        }
        
        public abstract void InitCategoryMap();
        public abstract void ChangeCategory(UIElement elem);
        public abstract void SetPage(int page);

        public void PageSlider(UIElement element)
        {
            var slider = (UISlider)element;
            SetPage((int)Math.Round(slider.Value));
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
            if (page + 1 == totalPages) return;
            SetPage(page + 1);
        }

        public override void Removed()
        {
            //clean up loose ends
            Holder.OnPickup -= HolderPickup;
            Holder.OnDelete -= HolderDelete;
            Holder.OnPutDown -= HolderPutDown;

            if (LotController.CustomControl != null)
            {
                LotController.CustomControl.Release();
                LotController.CustomControl = null;
            }

            if (Holder.Holding != null)
            {
                //delete object that hasn't been placed yet
                //TODO: all holding objects should obviously just be ghosts.
                //Holder.Holding.Group.Delete(vm.Context);
                Holder.ClearSelected();
                QueryPanel.Active = false;
            }

            SearchPanel.Parent?.Remove(SearchPanel);

            base.Removed();
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
                if (!holding.IsBought && holding.InventoryPID == 0 && (state.ShiftDown))
                {
                    //place another
                    var prevDir = holding.Dir;
                    Catalog_OnSelectionChange(OldSelection);
                    if (Holder.Holding != null) Holder.Holding.Dir = prevDir;
                }
                else
                {
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
        
        private float? UpgradeBuyItem(uint guid, byte level)
        {
            var upgrades = Content.Content.Get().Upgrades;
            var filename = BuyItem.BaseObject.Object.Resource.Iff.Filename;
            var price = upgrades.GetUpgradePrice(filename, guid, level);
            if (price != null)
            {
                foreach (var obj in BuyItem.Objects)
                {
                    var state = obj.PlatformState as VMTSOObjectState;
                    if (state != null)
                    {
                        state.UpgradeLevel = level;
                    }
                }
                BuyItem.InitialPrice = price.Value;
                return price.Value;
            }
            return null;
        }

        protected virtual void Catalog_OnSelectionChange(int selection)
        {
            if (BuyItem != null)
            {
                var baseObj = BuyItem.BaseObject;
                if (baseObj != null)
                {
                    var guid = (baseObj.MasterDefinition ?? baseObj.Object.OBJ).GUID;
                    var level = (baseObj.PlatformState as VMTSOObjectState)?.UpgradeLevel ?? 0;
                    UpgradeLevelMemory[guid] = level;
                }
            }
            Holder.ClearSelected();
            var item = Catalog.Filtered[selection];

            if (LotController.ActiveEntity != null && item.CalcPrice > LotController.ActiveEntity.TSOState.Budget.Value)
            {
                HIT.HITVM.Get().PlaySoundEvent(UISounds.Error);
                return;
            }

            if (OldSelection != -1) Catalog.SetActive(OldSelection, false);
            Catalog.SetActive(selection, true);

            if (LotController.CustomControl != null)
            {
                LotController.CustomControl.Release();
                LotController.CustomControl = null;
            }

            if (item.Special != null)
            {
                var res = item.Special.Res;
                var resID = item.Special.ResID;
                if (res != null && res.GetName(resID) != "")
                {
                    QueryPanel.SetInfo(res.GetThumb(resID), res.GetName(resID), res.GetDescription(resID), res.GetPrice(resID));
                    QueryPanel.Mode = 1;
                    QueryPanel.Tab = 0;
                    QueryPanel.Active = true;
                }
                LotController.CustomControl = (UICustomLotControl)Activator.CreateInstance(item.Special.Control, LotController.vm, LotController.World, LotController, item.Special.Parameters);
            }
            else
            {
                BuyItem = LotController.vm.Context.CreateObjectInstance(item.Item.GUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH, true);
                if (item.Attributes != null)
                {
                    for (int i = 0; i < item.Attributes.Count; i++) {
                        BuyItem.BaseObject.SetAttribute(i, (short)item.Attributes[i]);
                    }
                }
                byte upgradeLevel = 0;

                float? price = null;
                if (UpgradeLevelMemory.TryGetValue(item.Item.GUID, out upgradeLevel) && upgradeLevel > 0)
                {
                    price = UpgradeBuyItem(item.Item.GUID, upgradeLevel);
                }
                // token objects should not be placable.
                if (item.Item.DisableLevel < 3) Holder.SetSelected(BuyItem);
                QueryPanel.SetInfo(LotController.vm, BuyItem.BaseObject, false);
                QueryPanel.Mode = 1;
                QueryPanel.Tab = 0;
                QueryPanel.Active = true;
            }

            OldSelection = selection;
        }

        public override void Update(UpdateState state)
        {
            if (SearchPanel.Parent == null)
            {
                SearchPanel.SetParent(this.Parent);
            }

            base.Update(state);
        }
    }
}
