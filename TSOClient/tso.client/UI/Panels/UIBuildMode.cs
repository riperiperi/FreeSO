/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FSO.LotView.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics;
using FSO.SimAntics.Entities;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Controls.Catalog;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.LotControls;
using FSO.Client.UI.Model;
using FSO.Common;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.Client.UI.Panels
{
    //TODO: very similar to buy mode... maybe make a them both subclasses of a single abstract "purchase panel" class.

    public class UIBuildMode : UICachedContainer
    {
        public UIButton TerrainButton { get; set; }
        public UIButton WaterButton { get; set; }
        public UIButton WallButton { get; set; }
        public UIButton WallpaperButton { get; set; }
        public UIButton StairButton { get; set; }
        public UIButton FireplaceButton { get; set; }

        public UIButton PlantButton { get; set; }
        public UIButton FloorButton { get; set; }
        public UIButton DoorButton { get; set; }
        public UIButton WindowButton { get; set; }
        public UIButton RoofButton { get; set; }
        public UIButton HandButton { get; set; }

        public Texture2D subtoolsBackground { get; set; }
        public Texture2D dividerImage { get; set; }

        public UIImage Background;
        public UIImage Divider;

        public UIImage SubToolBg;
        public UISlider SubtoolsSlider { get; set; }
        public UIButton PreviousPageButton { get; set; } 
        public UIButton NextPageButton { get; set; }

        private Dictionary<UIButton, int> CategoryMap;
        private List<UICatalogElement> CurrentCategory;

        private UILabel ObjLimitLabel;
        private int LastObjCount = -1;

        public UICatalog Catalog;
        public UIObjectHolder Holder;
        public UIQueryPanel QueryPanel { get { return LotController.QueryPanel; } }
        public UILotControl LotController;
        private VMMultitileGroup BuyItem;

        private int OldSelection = -1;

        private UISlider RoofSlider;
        private UIButton RoofSteepBtn;
        private UIButton RoofShallowBtn;
        private uint TicksSinceRoof = 0;
        private bool SendRoofValue = false;

        public UIBuildMode(UILotControl lotController)
        {
            LotController = lotController;
            Holder = LotController.ObjectHolder;

            var useSmall = (FSOEnvironment.UIZoomFactor>1f || GlobalSettings.Default.GraphicsWidth < 1024);
            var script = this.RenderScript("buildpanel" + (useSmall ? "" : "1024") + ".uis");

            Background = new UIImage(GetTexture(useSmall ? (ulong)0x000000D800000002 : (ulong)0x0000018300000002));
            Background.Y = 0;
            Background.BlockInput();
            this.AddAt(0, Background);

            Size = Background.Size.ToVector2();

            Divider = new UIImage(dividerImage);
            Divider.Position = new Vector2(337, 14);
            this.AddAt(1, Divider);

            SubToolBg = new UIImage(subtoolsBackground);
            SubToolBg.Position = new Vector2(336, 5);
            this.AddAt(2, SubToolBg);

            Catalog = new UICatalog(useSmall ? 10 : 20);
            Catalog.ActiveVM = lotController.vm;
            Catalog.OnSelectionChange += new CatalogSelectionChangeDelegate(Catalog_OnSelectionChange);
            Catalog.Position = new Vector2(364, 7);
            this.Add(Catalog);

            CategoryMap = new Dictionary<UIButton, int>
            {
                { TerrainButton, 10 },
                { WaterButton, 5 },
                { WallButton, 7 },
                { WallpaperButton, 8 },
                { StairButton, 2 },
                { FireplaceButton, 4 },

                { PlantButton, 3 },
                { FloorButton, 9 },
                { DoorButton, 0 },
                { WindowButton, 1 },
                { RoofButton, 6 },
                { HandButton, 28 },
            };

            TerrainButton.Disabled = (LotController?.ActiveEntity?.TSOState as VMTSOAvatarState)?.Permissions < VMTSOAvatarPermissions.Admin;
            TerrainButton.OnButtonClick += ChangeCategory;
            WaterButton.OnButtonClick += ChangeCategory;
            WallButton.OnButtonClick += ChangeCategory;
            WallpaperButton.OnButtonClick += ChangeCategory;
            StairButton.OnButtonClick += ChangeCategory;
            FireplaceButton.OnButtonClick += ChangeCategory;

            PlantButton.OnButtonClick += ChangeCategory;
            FloorButton.OnButtonClick += ChangeCategory;
            DoorButton.OnButtonClick += ChangeCategory;
            WindowButton.OnButtonClick += ChangeCategory;
            RoofButton.OnButtonClick += ChangeCategory;
            HandButton.OnButtonClick += ChangeCategory;

            PreviousPageButton.OnButtonClick += PreviousPage;
            NextPageButton.OnButtonClick += NextPage;
            SubtoolsSlider.MinValue = 0;
            SubtoolsSlider.OnChange += PageSlider;

            Holder.OnPickup += HolderPickup;
            Holder.OnDelete += HolderDelete;
            Holder.OnPutDown += HolderPutDown;
            DynamicOverlay.Add(QueryPanel);

            LotController.ObjectHolder.Roommate = true;
            LotController.QueryPanel.Roommate = true;

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

            RoofSteepBtn = new UIButton(GetTexture(0x4C200000001));
            RoofSteepBtn.X = 46;
            RoofSteepBtn.Y = 6;
            Add(RoofSteepBtn);
            RoofShallowBtn = new UIButton(GetTexture(0x4C700000001));
            RoofShallowBtn.X = 46;
            RoofShallowBtn.Y = 92;
            Add(RoofShallowBtn);


            RoofSlider = new UISlider();
            RoofSlider.Orientation = 1;
            RoofSlider.Texture = GetTexture(0x4AB00000001);
            RoofSlider.MinValue = 0f;
            RoofSlider.MaxValue = 1.25f;
            RoofSlider.AllowDecimals = true;
            RoofSlider.AttachButtons(RoofSteepBtn, RoofShallowBtn, 0.25f);
            RoofSlider.X = 48;
            RoofSlider.Y = 24;
            RoofSlider.OnChange += (elem) =>
            {
                if (RoofSlider.Value != (1.25f - LotController.vm.Context.Architecture.RoofPitch))
                {
                    LotController.vm.Context.Blueprint.RoofComp.SetStylePitch(
                        LotController.vm.Context.Architecture.RoofStyle,
                        (1.25f - RoofSlider.Value)
                        );
                    SendRoofValue = true;
                }
            };
            RoofSlider.SetSize(0, 64f);
            Add(RoofSlider);

            RoofSteepBtn.Visible = false;
            RoofShallowBtn.Visible = false;
            RoofSlider.Visible = false;
        }
    

        public void PageSlider(UIElement element)
        {
            var slider = (UISlider)element;
            SetPage((int)Math.Round(slider.Value));
        }

        public void SetPage(int page)
        {
            bool noPrev = (page == 0);
            PreviousPageButton.Disabled = noPrev;

            bool noNext = (page + 1 == Catalog.TotalPages());
            NextPageButton.Disabled = noNext;

            Catalog.SetPage(page);
            if (OldSelection != -1) Catalog.SetActive(OldSelection, true);

            SubtoolsSlider.Value = page;
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

        public void ChangeCategory(UIElement elem)
        {
            TerrainButton.Selected = false;
            WaterButton.Selected = false;
            WallButton.Selected = false;
            WallpaperButton.Selected = false;
            StairButton.Selected = false;
            FireplaceButton.Selected = false;

            PlantButton.Selected = false;
            FloorButton.Selected = false;
            DoorButton.Selected = false;
            WindowButton.Selected = false;
            RoofButton.Selected = false;
            HandButton.Selected = false;

            UIButton button = (UIButton)elem;
            button.Selected = true;
            if (!CategoryMap.ContainsKey(button)) return;
            CurrentCategory = UICatalog.Catalog[CategoryMap[button]];
            Catalog.SetCategory(CurrentCategory);

            var isRoof = CategoryMap[button] == 6;
            RoofShallowBtn.Visible = isRoof;
            RoofSteepBtn.Visible = isRoof;
            RoofSlider.Visible = isRoof;
            RoofSlider.Value = 1.25f - LotController.vm.Context.Architecture.RoofPitch;

            int total = Catalog.TotalPages();
            OldSelection = -1;

            SubtoolsSlider.MaxValue = total - 1;
            SubtoolsSlider.Value = 0;

            NextPageButton.Disabled = (total == 1);

            if (LotController.CustomControl != null)
            {
                LotController.CustomControl.Release();
                LotController.CustomControl = null;
            }

            PreviousPageButton.Disabled = true;

            var showsubtools = CategoryMap[button] != 10;
            SubToolBg.Visible = showsubtools;
            SubtoolsSlider.Visible = showsubtools;
            PreviousPageButton.Visible = showsubtools;
            NextPageButton.Visible = showsubtools;
            
            return;
        }

        void Catalog_OnSelectionChange(int selection)
        {
            Holder.ClearSelected();
            var item = CurrentCategory[selection];

            if (LotController.ActiveEntity != null && item.CalcPrice > LotController.ActiveEntity.TSOState.Budget.Value) {
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
                QueryPanel.SetInfo(LotController.vm, BuyItem.Objects[0], false);
                QueryPanel.Mode = 1;
                QueryPanel.Tab = 0;
                QueryPanel.Active = true;
                Holder.SetSelected(BuyItem);
            }

            OldSelection = selection;
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
                if (!holding.IsBought && (state.ShiftDown))
                {
                    //place another
                    var prevDir = holding.Dir;
                    Catalog_OnSelectionChange(OldSelection);
                    Holder.Holding.Dir = prevDir;
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

        public override void Update(UpdateState state)
        {
            CategoryMap[TerrainButton] = (state.ShiftDown && (LotController?.ActiveEntity?.TSOState as VMTSOAvatarState)?.Permissions >= VMTSOAvatarPermissions.Admin) ? 29 : 10;
            var objCount = LotController.vm.Context.ObjectQueries.NumUserObjects;
            if (LastObjCount != objCount)
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
                LastObjCount = objCount;
            }

            if (LotController.ActiveEntity != null) Catalog.Budget = (int)LotController.ActiveEntity.TSOState.Budget.Value;
            TicksSinceRoof++;
            if (TicksSinceRoof > 30 && SendRoofValue)
            {
                LotController.vm.SendCommand(new SimAntics.NetPlay.Model.Commands.VMNetSetRoofCmd()
                {
                    Pitch = 1.25f - RoofSlider.Value,
                    Style = LotController.vm.Context.Architecture.RoofStyle
                });
                SendRoofValue = false;
                TicksSinceRoof = 0;
            }
            base.Update(state);
        }
    }
}
