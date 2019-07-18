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

    public class UIBuildMode : UIAbstractCatalogPanel
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
        
        public UIImage Divider;

        public UIImage SubToolBg;
        public UISlider SubtoolsSlider { get; set; }
        public UIButton PreviousPageButton { get; set; } 
        public UIButton NextPageButton { get; set; }

        private UISlider RoofSlider;
        private UIButton RoofSteepBtn;
        private UIButton RoofShallowBtn;
        private uint TicksSinceRoof = 0;
        private bool SendRoofValue = false;

        public UIBuildMode(UILotControl lotController) : base("buildpanel", lotController)
        {
            Divider = new UIImage(dividerImage);
            Divider.Position = new Vector2(337, 14);
            this.AddAt(1, Divider);

            SubToolBg = new UIImage(subtoolsBackground);
            SubToolBg.Position = new Vector2(336, 5);
            this.AddAt(2, SubToolBg);

            Catalog.Position = new Vector2(364, 7);

            PreviousPageButton.OnButtonClick += PreviousPage;
            NextPageButton.OnButtonClick += NextPage;
            SubtoolsSlider.MinValue = 0;
            SubtoolsSlider.OnChange += PageSlider;

            LotController.ObjectHolder.Roommate = true;
            LotController.QueryPanel.Roommate = true;

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

        public override void InitCategoryMap()
        {
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
        }

        public override void SetPage(int page)
        {
            bool noPrev = (page == 0);
            PreviousPageButton.Disabled = noPrev;

            bool noNext = (page + 1 == Catalog.TotalPages());
            NextPageButton.Disabled = noNext;

            Catalog.SetPage(page);
            if (OldSelection != -1) Catalog.SetActive(OldSelection, true);

            SubtoolsSlider.Value = page;
        }

        public override void ChangeCategory(UIElement elem)
        {
            QueryPanel.InInventory = false;
            foreach (var btn in CategoryMap.Keys)
                btn.Selected = false;

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

        public override void Update(UpdateState state)
        {
            CategoryMap[TerrainButton] = (state.ShiftDown && (LotController?.ActiveEntity?.TSOState as VMTSOAvatarState)?.Permissions >= VMTSOAvatarPermissions.Admin) ? 29 : 10;
            var objCount = LotController.vm.Context.ObjectQueries.NumUserObjects;
            if (LastObjCount != objCount || LastDonator != LotController.ObjectHolder.DonateMode)
            {
                if (LastDonator != LotController.ObjectHolder.DonateMode)
                {
                    Catalog.SetPage(Catalog.Page); //update prices
                }
                if (LotController.ObjectHolder.DonateMode) {
                    ObjLimitLabel.Caption = GameFacade.Strings.GetString("f114", "4");
                    ObjLimitLabel.CaptionStyle.Color = new Color(255, 201, 38);
                } else {
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

            if (LotController.ActiveEntity != null) Catalog.Budget = (int)LotController.Budget;
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
