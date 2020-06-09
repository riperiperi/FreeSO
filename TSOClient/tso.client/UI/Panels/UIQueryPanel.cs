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
using Microsoft.Xna.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics;
using FSO.Files.Formats.IFF.Chunks;
using FSO.HIT;
using FSO.Client.UI.Model;
using FSO.LotView;
using FSO.LotView.Components;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.Common;
using FSO.SimAntics.Model;
using FSO.UI.Panels;
using FSO.SimAntics.Model.Platform;
using FSO.Client.UI.Panels.Upgrades;
using FSO.SimAntics.Engine;

namespace FSO.Client.UI.Panels
{
    public class UIQueryPanel : UICachedContainer
    {
        public UILotControl LotParent;
        public Texture2D BackgroundImageCatalog { get; set; }
        public Texture2D BackgroundImageTrade { get; set; }
        public Texture2D BackgroundImagePanel { get; set; }
        public Texture2D ImageWearBack { get; set; }
        public UIImage QuerybackPanel;
        public UIImage QuerybackCatalog;
        public UIImage QuerybackTrade;

        public UIButton GeneralTabButton { get; set; }
        public UIButton SpecificTabButton { get; set; }

        //general tab only
        public Texture2D ImageDescriptionBackground { get; set; }
        public Texture2D ImageMotivesBackground { get; set; }
        public Texture2D ImageGeneralTab { get; set; }

        public UIImage DescriptionBackgroundImage;
        public UIImage MotivesBackgroundImage;
        public UIImage GeneralTabImage;

        public UITextEdit DescriptionText { get; set; }
        public UISlider DescriptionSlider { get; set; }
        public UIButton DescriptionScrollUpButton { get; set; }
        public UIButton DescriptionScrollDownButton { get; set; }

        public UITextEdit MotivesText { get; set; }
        public UISlider MotivesSlider { get; set; }
        public UIButton MotivesScrollUpButton { get; set; }
        public UIButton MotivesScrollDownButton { get; set; }

        //specific tab only
        public Texture2D ImageSpecificTab { get; set; }
        public UIImage SpecificTabImage;

        public UILabel ObjectIsBrokenText { get; set; }
        public UILabel ObjectNeedsUpgradeText { get; set; }
        public UILabel ObjectIsDisabledText { get; set; }

        public UIProgressBar WearProgressBar { get; set; }
        public UILabel WearValueText { get; set; }
        public UILabel WearLabelText { get; set; }

        public UITextEdit ForSalePrice { get; set; }

        public UIButton SellBackButton { get; set; }
        public UIButton InventoryButton { get; set; }

        public UILabel ObjectNameText { get; set; }
        public UILabel ObjectOwnerText { get; set; }
        public UILabel ObjectValueText { get; set; }
        public UILabel ObjectCrafterText { get; set; }

        public UIButton AsyncSaleButton { get; set; }
        public UIButton AsyncCancelSaleButton { get; set; }
        public UIButton AsyncEditPriceButton { get; set; }
        public UIButton AsyncBuyButton { get; set; }
        public UIImage AsyncSaleButtonBG { get; set; }
        public UIImage AsyncCancelSaleButtonBG { get; set; }

        public Texture2D GeneralOwnerPriceBack { get; set; }
        public UIImage OwnerPriceBack;
        public UIImage BuyerPriceBack;
        private List<UIImage> SpecificBtnBGs;

        //upgrade additions
        public UIImage UpgradeBack;
        public UIButton UpgradeButton;
        public UIUpgradeList UpgradeList;

        public event ButtonClickDelegate OnSellBackClicked;
        public event ButtonClickDelegate OnInventoryClicked;
        public event ButtonClickDelegate OnAsyncSaleClicked;
        public event ButtonClickDelegate OnAsyncBuyClicked;
        public event ButtonClickDelegate OnAsyncPriceClicked;
        public event ButtonClickDelegate OnAsyncSaleCancelClicked;

        //world required for drawing thumbnails
        public LotView.World World;
        public UIImage Thumbnail;
        public UI3DThumb Thumb3D;
        public bool Roommate = true;
        public int InInventory = 0;

        private VMEntity ActiveEntity;
        private int LastSalePrice;
        private bool CanSell = true;
        private bool IAmOwner;
        private bool Ghost;

        private bool _Active;
        public bool Active
        {
            set
            {
                if (_Active != value) HITVM.Get().PlaySoundEvent(UISounds.Whoosh);
                _Active = value;
            }
            get
            {
                return _Active;
            }
        }

        private int _Tab;
        public int Tab
        {
            set
            {
                //0:general, 1:specific, 2:trade
                DescriptionBackgroundImage.Visible = (value == 0);
                MotivesBackgroundImage.Visible = (value == 0);
                GeneralTabImage.Visible = (value == 0);

                DescriptionText.Visible = (value == 0);
                DescriptionSlider.Visible = (value == 0);
                DescriptionScrollUpButton.Visible = (value == 0);
                DescriptionScrollDownButton.Visible = (value == 0);

                MotivesText.Visible = (value == 0);
                MotivesSlider.Visible = (value == 0);
                MotivesScrollUpButton.Visible = (value == 0);
                MotivesScrollDownButton.Visible = (value == 0);

                GeneralTabButton.Selected = (value == 0);

                //specific value only
                SpecificTabImage.Visible = (value == 1);

                //TODO: detect these cases and display where relevant
                ObjectIsBrokenText.Visible = false;
                ObjectNeedsUpgradeText.Visible = false;
                ObjectIsDisabledText.Visible = false;

                SpecificTabButton.Selected = (value == 1);

                //null for some reason?
                WearValueText.Visible = (value == 1);
                WearLabelText.Visible = (value == 1);
                WearLabelText.Alignment = TextAlignment.Center | TextAlignment.Middle;

                ForSalePrice.Visible = (value == 1);

                var validator = LotParent.vm.PlatformState.Validator;
                SellBackButton.Visible = (value == 1);
                var delMode = validator.GetDeleteMode(DeleteMode.Delete, (VMAvatar)LotParent.ActiveEntity, ActiveEntity);
                SellBackButton.Disabled = delMode == DeleteMode.Disallowed || Ghost;
                InventoryButton.Visible = (value == 1);
                var sendbackMode = validator.GetDeleteMode(DeleteMode.Sendback, (VMAvatar)LotParent.ActiveEntity, ActiveEntity);
                InventoryButton.Disabled = sendbackMode != DeleteMode.Sendback || Ghost;

                ObjectNameText.Visible = (value == 1);
                ObjectOwnerText.Visible = (value == 1);
                ObjectValueText.Visible = (value == 1);
                ObjectCrafterText.Visible = (value == 1);

                WearProgressBar.Visible = (value == 1);
                
                AsyncSaleButton.Visible = (value == 1) && LastSalePrice < 0 && !Ghost && CanSell;
                AsyncCancelSaleButton.Visible = (value == 1) && IAmOwner && LastSalePrice > -1;
                AsyncCancelSaleButtonBG.Visible = AsyncCancelSaleButton.Visible;
                AsyncEditPriceButton.Visible = (value == 1) && IAmOwner && LastSalePrice > -1;
                AsyncBuyButton.Visible = (value == 1) && (!IAmOwner) && LastSalePrice > -1;

                foreach (var bg in SpecificBtnBGs)
                {
                    bg.Visible = (value == 1);
                }

                AsyncSaleButtonBG.Visible = AsyncSaleButton.Visible || LastSalePrice > -1;

                //uh..
                OwnerPriceBack.Visible = (value == 1) && IAmOwner && LastSalePrice > -1;
                BuyerPriceBack.Visible = (value == 1) && (!IAmOwner) && LastSalePrice > -1;
                _Tab = value;
                UpdateImagePosition();

                foreach (var elem in Children)
                {
                    var label = elem as UILabel;
                    if (label != null)
                    {
                        label.CaptionStyle = label.CaptionStyle.Clone();
                        label.CaptionStyle.Shadow = true;
                    }
                }

                ForSalePrice.TextStyle = ForSalePrice.TextStyle.Clone();
                ForSalePrice.TextStyle.Shadow = true;
            }
            get
            {
                return _Tab;
            }
        }

        private string[] AdStrings;
        private string[] CategoryStrings;

        private int _Mode;
        public int Mode
        {
            get
            {
                return _Mode;
            }
            set
            {
                if (value < 2)
                {
                    Size = QuerybackPanel.Size.ToVector2() + new Vector2(22, 42);
                    BackOffset = new Point(22, 0);
                }
                else
                {
                    Size = QuerybackTrade.Size.ToVector2() + new Vector2(22, 42);
                    BackOffset = new Point(40, 0);
                }
                this.Y = (value>0)?-114:0;
                this.X = (value==2)?-128:0;
                QuerybackPanel.Visible = (value == 0);
                QuerybackCatalog.Visible = (value == 1);
                QuerybackTrade.Visible = (value == 2);
                _Mode = value;
            }
        }

        public UIQueryPanel(UILotControl parent, LotView.World world)
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;
            LotParent = parent;
            World = world;
            Active = false;
            Opacity = 0;
            Visible = false;

            AdStrings = new string[14];
            for (int i = 0; i < 14; i++)
            {
                string str = GameFacade.Strings.GetString("206", (i + 4).ToString());
                AdStrings[i] = ((i < 7) ? str.Substring(0, str.Length - 2) + "{0}" : str) + "\r\n";
            }

            CategoryStrings = new string[11];
            for (int i = 0; i < 10; i++)
            {
                CategoryStrings[i] = GameFacade.Strings.GetString("f115", (i + 73).ToString());
            }
            CategoryStrings[10] = GameFacade.Strings.GetString("f115", "98");

            var useSmall = (GlobalSettings.Default.GraphicsWidth < 1024) || FSOEnvironment.UIZoomFactor > 1f;
            var script = this.RenderScript("querypanel" + (useSmall ? "" : "1024") + ".uis");

            //NOTE: the background and position of this element changes with the context it is used in.
            //other elements that are only used for certain modes will be flagged as such with comments.

            QuerybackPanel = new UIImage(BackgroundImagePanel);
            QuerybackPanel.Y = 0;
            this.AddAt(0, QuerybackPanel);

            ListenForMouse(new Rectangle(0, 0, QuerybackPanel.Texture.Width, QuerybackPanel.Texture.Height), (t, s) => { });

            Size = QuerybackPanel.Size.ToVector2() + new Vector2(22, 42);
            BackOffset = new Point(40, 0);

            QuerybackCatalog = new UIImage(BackgroundImageCatalog);
            QuerybackCatalog.Position = new Vector2(-22, 0);
            this.AddAt(1, QuerybackCatalog);

            QuerybackTrade = new UIImage(BackgroundImageTrade);
            QuerybackTrade.X = -40;
            QuerybackTrade.Y = 0;
            this.AddAt(2, QuerybackTrade);

            //init general tab specific backgrounds

            DescriptionBackgroundImage = new UIImage(ImageDescriptionBackground);
            DescriptionBackgroundImage.Position = new Microsoft.Xna.Framework.Vector2(119, 7);
            this.AddAt(3, DescriptionBackgroundImage);

            MotivesBackgroundImage = new UIImage(ImageMotivesBackground);
            MotivesBackgroundImage.Position = new Microsoft.Xna.Framework.Vector2(useSmall ? 395 : 619, 7);
            this.AddAt(3, MotivesBackgroundImage);

            GeneralTabImage = new UIImage(ImageGeneralTab);
            GeneralTabImage.Position = new Microsoft.Xna.Framework.Vector2(useSmall ? 563 : 787, 0);
            this.AddAt(3, GeneralTabImage);

            SpecificTabImage = new UIImage(ImageSpecificTab);
            SpecificTabImage.Position = new Microsoft.Xna.Framework.Vector2(useSmall ? 563 : 787, 0);
            this.AddAt(3, SpecificTabImage);

            OwnerPriceBack = script.Create<UIImage>("OwnerPriceBack");
            this.AddAt(3, OwnerPriceBack);

            BuyerPriceBack = script.Create<UIImage>("BuyerPriceBack");
            this.AddAt(3, BuyerPriceBack);

            OwnerPriceBack.X = ForSalePrice.X;
            BuyerPriceBack.X = ForSalePrice.X;
            ForSalePrice.Y += 2;
            ForSalePrice.SetSize(OwnerPriceBack.Width, ForSalePrice.Height);

            Thumbnail = new UIImage();
            Thumbnail.Position = new Vector2(24, 11);
            Thumbnail.SetSize(90, 90);
            this.Add(Thumbnail);

            DescriptionText.CurrentText = "No Object Selected"; //user should not see this.
            DescriptionSlider.AttachButtons(DescriptionScrollUpButton, DescriptionScrollDownButton, 1);
            DescriptionText.AttachSlider(DescriptionSlider);

            MotivesText.CurrentText = "";
            MotivesSlider.AttachButtons(MotivesScrollUpButton, MotivesScrollDownButton, 1);
            MotivesText.AttachSlider(MotivesSlider);

            GeneralTabButton.OnButtonClick += new ButtonClickDelegate(GeneralTabButton_OnButtonClick);
            SpecificTabButton.OnButtonClick += new ButtonClickDelegate(SpecificTabButton_OnButtonClick);
            SellBackButton.OnButtonClick += new ButtonClickDelegate(SellBackButton_OnButtonClick);

            AsyncBuyButton.OnButtonClick += (btn) => { OnAsyncBuyClicked?.Invoke(btn); };
            AsyncSaleButton.OnButtonClick += (btn) => { OnAsyncSaleClicked?.Invoke(btn); };
            AsyncEditPriceButton.OnButtonClick += (btn) => { OnAsyncPriceClicked?.Invoke(btn); };
            AsyncCancelSaleButton.OnButtonClick += (btn) => { OnAsyncSaleCancelClicked?.Invoke(btn); };

            InventoryButton.OnButtonClick += InventoryButton_OnButtonClick;

            WearProgressBar.CaptionStyle = TextStyle.DefaultLabel.Clone();
            WearProgressBar.CaptionStyle.Shadow = true;

            var btnBg = GetTexture(0x8A700000001); //buybuild_query_generalbuy_sellasyncback = 0x8A700000001
            SpecificBtnBGs = new List<UIImage>();
            SpecificBtnBGs.Add(AddButtonBackground(SellBackButton, btnBg));
            SpecificBtnBGs.Add(AddButtonBackground(InventoryButton, btnBg));
            AsyncSaleButtonBG = AddButtonBackground(AsyncSaleButton, btnBg);
            SpecificBtnBGs.Add(AsyncSaleButtonBG);
            AsyncCancelSaleButtonBG = AddButtonBackground(AsyncCancelSaleButton, btnBg);

            var progressBG = new UIImage(ImageWearBack);
            progressBG.Position = WearProgressBar.Position - new Vector2(3, 2);
            AddAt(3, progressBG);
            SpecificBtnBGs.Add(progressBG);

            //upgrade button
            UpgradeBack = new UIImage(ui.Get("up_button_seat.png").Get(gd));
            UpgradeBack.Position = new Vector2(useSmall ? 582 : 806, 0);
            Add(UpgradeBack);

            UpgradeList = new UIUpgradeList(LotParent);
            UpgradeList.Visible = false;
            DynamicOverlay.Add(UpgradeList);

            UpgradeButton = new UIButton(ui.Get("up_button.png").Get(gd));
            UpgradeButton.OnButtonClick += UpgradeButton_OnButtonClick;
            UpgradeButton.Tooltip = GameFacade.Strings.GetString("f125", "1");
            UpgradeButton.Position = UpgradeBack.Position + new Vector2(6, 7);
            DynamicOverlay.Add(UpgradeButton);
            UpgradeList.Position = UpgradeButton.Position + new Vector2(-542, 11);

            Mode = 1;
            Tab = 0;
        }

        private void SetUpgradeListVisible(bool visible)
        {
            UpgradeButton.Selected = visible;
            if (visible != UpgradeList.Shown)
            {
                if (!visible) UpgradeList.Hide();
                else
                {
                    //if upgrades are available, show the list
                    if (ActiveEntity == null) return;
                    UpgradeList.Show(ActiveEntity);
                }
            }
        }

        private void UpgradeButton_OnButtonClick(UIElement button)
        {
            SetUpgradeListVisible(!UpgradeList.Shown);
        }

        private UIImage AddButtonBackground(UIElement button, Texture2D img)
        {
            var bg = new UIImage(img);
            bg.Position = button.Position - new Vector2(3, 3);
            this.AddAt(3, bg);
            return bg;
        }

        private void InventoryButton_OnButtonClick(UIElement button)
        {
            if (OnInventoryClicked != null) OnInventoryClicked(button);
        }

        void SellBackButton_OnButtonClick(UIElement button)
        {
            if (OnSellBackClicked != null) OnSellBackClicked(button);
        }

        void SpecificTabButton_OnButtonClick(UIElement button)
        {
            Tab = 1;
        }

        void GeneralTabButton_OnButtonClick(UIElement button)
        {
            Tab = 0;
        }

        public void ReloadEntityInfo(bool bought)
        {
            if (ActiveEntity == null) return;
            var lastTab = Tab;
            SetInfo(LotParent.vm, ActiveEntity, bought);
            Tab = lastTab;
        }

        public override void Update(UpdateState state)
        {
            if (Active)
            {
                Visible = true;
                if (Opacity < 1f) Opacity += 1f / 20f;
                else
                {
                    Opacity = 1;
                }
                if (ActiveEntity != null && LastSalePrice != ActiveEntity.MultitileGroup.SalePrice && ActiveEntity.Thread != null)
                {
                    ReloadEntityInfo(true);
                }
                
            }
            else
            {
                SetUpgradeListVisible(false);
                if (Opacity > 0f) Opacity -= 1f / 20f;
                else
                {
                    Visible = false;
                    Opacity = 0f;
                }
            }
            base.Update(state);

            UpgradeButton.Opacity = Opacity;
            UpgradeList.Opacity = Opacity;

            if (Visible && Thumb3D != null) Invalidate();
        }

        private string DescProcess(VM vm, VMEntity ent, string desc, STR source)
        {
            var frame = new VMStackFrame()
            {
                Caller = ent,
                Callee = ent,
                CodeOwner = ent.Object,
                StackObject = ent,
                Routine = null,
                Args = new short[4],
                Thread = ent.Thread,
            };

            return VMDialogHandler.ParseDialogString(frame, desc, source);
        }

        public Tuple<string, string> GetObjName(VM vm, VMEntity entity)
        {
            string name = null;
            if (entity.Object.GUID == 0x3278BD34 || entity.Object.GUID == 0x5157DDF2)
            {
                //pet carrier name calculation
                int skinIndex = 0;
                if (entity.GetAttribute(24) == 0)
                {
                    //skin is not random - it has already been assigned
                    skinIndex = entity.GetAttribute(2) + 1;
                }
                name = entity.Object.Resource.Get<STR>(330).GetString(skinIndex);
            }
            else
            {
                if (entity.MultitileGroup.Name != null && entity.MultitileGroup.Name != "") name = entity.Name;
            }

            var obj = entity.Object;
            var def = entity.MasterDefinition;
            if (def == null) def = entity.Object.OBJ;

            var item = Content.Content.Get().WorldCatalog.GetItemByGUID(def.GUID);

            CTSS catString = obj.Resource.Get<CTSS>(def.CatalogStringsID);
            var desc = "";
            if (catString != null)
            {
                if (name == null) name = catString.GetString(0);
                desc = catString.GetString(1);
            }
            else
            {
                if (name == null) name = entity.ToString();
            }
            var tsoState = entity.PlatformState as VMTSOObjectState;
            if (tsoState != null && tsoState.UpgradeLevel > 0)
            {
                name += ' ';
                for (int i=0; i<tsoState.UpgradeLevel; i++)
                {
                    name += '★';
                }
            }
            desc = DescProcess(vm, entity, desc, catString);
            return new Tuple<string, string>(name, desc);
        }

        public void SetInfo(VM vm, VMEntity entity, bool bought)
        {
            var sameEntity = entity == ActiveEntity;
            ActiveEntity = entity;
            var obj = entity.Object;
            var def = entity.MasterDefinition;
            if (def == null) def = entity.Object.OBJ;

            var item = Content.Content.Get().WorldCatalog.GetItemByGUID(def.GUID);

            var ndesc = GetObjName(vm, entity);

            DescriptionText.CurrentText = ndesc.Item1 + "\r\n" + ndesc.Item2;
            ObjectNameText.Caption = ndesc.Item1;

            IAmOwner = ((entity.TSOState as VMTSOObjectState)?.OwnerID ?? 0) == vm.MyUID;
            Ghost = entity.GhostImage;
            LastSalePrice = entity.MultitileGroup.SalePrice;
            CanSell = 
                vm.PlatformState.Validator.CanManageAsyncSale((VMAvatar)LotParent.ActiveEntity, ActiveEntity as VMGameObject)
                && (item?.DisableLevel ?? 0) < 2;

            var upgrades = Content.Content.Get().Upgrades.GetFile(entity.Object.Resource.MainIff.Filename);
            if (!sameEntity) SetHasUpgrades(upgrades != null, bought);

            var upgradeLevel = (entity.PlatformState as VMTSOObjectState)?.UpgradeLevel ?? 0;
            int price = def.Price;
            int finalPrice = price;
            int dcPercent = 0;
            if (item != null)
            {
                price = (int)item.Value.Price;
                if (upgradeLevel > 0) price = entity.MultitileGroup.InitialPrice;
                dcPercent = VMBuildableAreaInfo.GetDiscountFor(item.Value, vm);
                finalPrice = (price * (100-dcPercent)) / 100;
                if (LotParent.ObjectHolder.DonateMode)
                {
                    finalPrice -= (finalPrice * 2) / 3;
                    dcPercent = 66;
                }
            }

            StringBuilder motivesString = new StringBuilder();
            if (dcPercent > 0)
            {
                motivesString.Append(GameFacade.Strings.GetString("206", "36", new string[] { dcPercent.ToString() + "%" }) + "\r\n");
                motivesString.AppendFormat(GameFacade.Strings.GetString("206", "37") + "${0}\r\n", finalPrice);
                motivesString.AppendFormat(GameFacade.Strings.GetString("206", "38") + "${0}\r\n", price);
            }
            else
            {
                motivesString.AppendFormat(GameFacade.Strings.GetString("206", "19") + "${0}\r\n", price);
            }

            var catFlags = def.LotCategories;
            for (int i = 1; i < 12; i++)
            {
                if ((catFlags & (1 << i)) > 0) motivesString.AppendLine(CategoryStrings[i - 1]);
            }

            if (def.RatingHunger != 0) { motivesString.AppendFormat(AdStrings[0], (short)def.RatingHunger); }
            if (def.RatingComfort != 0) { motivesString.AppendFormat(AdStrings[1], (short)def.RatingComfort); }
            if (def.RatingHygiene != 0) { motivesString.AppendFormat(AdStrings[2], (short)def.RatingHygiene); }
            if (def.RatingBladder != 0) { motivesString.AppendFormat(AdStrings[3], (short)def.RatingBladder); }
            if (def.RatingEnergy != 0) { motivesString.AppendFormat(AdStrings[4], (short)def.RatingEnergy); }
            if (def.RatingFun != 0) { motivesString.AppendFormat(AdStrings[5], (short)def.RatingFun); }
            if (def.RatingRoom != 0) { motivesString.AppendFormat(AdStrings[6], (short)def.RatingRoom); }

            var sFlags = def.RatingSkillFlags;
            for (int i = 0; i < 7; i++)
            {
                if ((sFlags & (1 << i)) > 0) motivesString.Append(AdStrings[i + 7]);
            }

            MotivesText.CurrentText = motivesString.ToString();

            string owner = "Nobody";
            var ownerTable = "206";
            var ownerEntry = "24";
            if (entity is VMGameObject && ((VMTSOObjectState)entity.TSOState).OwnerID > 0)
            {
                var ownerID = ((VMTSOObjectState)entity.TSOState).OwnerID;
                owner = (vm.TSOState.Names.GetNameForID(vm, ownerID));
                if (((VMTSOObjectState)entity.TSOState).ObjectFlags.HasFlag(VMTSOObjectFlags.FSODonated))
                {
                    ownerTable = "f114";
                    ownerEntry = "1";
                }
            }

            ObjectOwnerText.Caption = (!entity.GhostImage)?GameFacade.Strings.GetString(ownerTable, ownerEntry, new string[] { owner }):"";

            SpecificTabButton.Disabled = !bought;

            if (bought)
            {
                ObjectValueText.Caption = GameFacade.Strings.GetString("206", "25", new string[] { " $" + entity.MultitileGroup.Price });
                ObjectValueText.Alignment = TextAlignment.Center;
                ObjectValueText.X = ObjectNameText.X;
                ObjectValueText.Size = new Vector2(260, 1);
            }

            if (LastSalePrice > -1)
            {
                ForSalePrice.CurrentText = "$" + entity.MultitileGroup.SalePrice;
                ForSalePrice.Alignment = TextAlignment.Center;
                //ForSalePrice.SetSize(250, ForSalePrice.Height);
                SellBackButton.Disabled = (entity.PersistID == 0);
            } else
            {
                ForSalePrice.CurrentText = "";
            }

            if (entity is VMGameObject) {
                WearProgressBar.Value = 100-((VMTSOObjectState)entity.TSOState).Wear/4;
                WearProgressBar.Caption = ((VMTSOObjectState)entity.MultitileGroup.BaseObject.TSOState).Broken? GameFacade.Strings.GetString("206", "34") : null;
                WearValueText.Caption = ((VMTSOObjectState)entity.TSOState).Wear / 4 + "%";
                var objects = entity.MultitileGroup.Objects;
                ObjectComponent[] objComps = new ObjectComponent[objects.Count];
                for (int i=0; i<objects.Count; i++) {
                    objComps[i] = (ObjectComponent)objects[i].WorldUI;
                }

                if (Thumbnail.Texture != null) Thumbnail.Texture.Dispose();
                if (Thumb3D != null) Thumb3D.Dispose();
                Thumb3D = null; Thumbnail.Texture = null;
                if (World.State.CameraMode == LotView.Model.CameraRenderMode._3D)
                {
                    Thumb3D = new UI3DThumb(entity);
                }
                else
                {
                    var thumb = World.GetObjectThumb(objComps, entity.MultitileGroup.GetBasePositions(), GameFacade.GraphicsDevice);
                    Thumbnail.Texture = thumb;
                }
                UpdateImagePosition();
            } else
            {
                WearProgressBar.Value = 0;
                WearValueText.Caption = "0%";
                if (Thumbnail.Texture != null) Thumbnail.Texture.Dispose();
                if (Thumb3D != null) Thumb3D.Dispose();
                Thumb3D = null;
                Thumbnail.Texture = null;
            }

            if (bought) FSOFacade.Hints.TriggerHint("ui:querypanel");
        }

        public void SetInfo(Texture2D thumb, string name, string description, int price)
        {
            ActiveEntity = null;
            Ghost = false;
            DescriptionText.CurrentText = name + "\r\n" + description;
            ObjectNameText.Caption = name;

            StringBuilder motivesString = new StringBuilder();
            motivesString.AppendFormat(GameFacade.Strings.GetString("206", "19") + "${0}\r\n", price);
            MotivesText.CurrentText = motivesString.ToString();

            SpecificTabButton.Disabled = true;
            SellBackButton.Disabled = true;
            SetHasUpgrades(null, false);

            if (Thumbnail.Texture != null) Thumbnail.Texture.Dispose();
            if (Thumb3D != null) Thumb3D.Dispose();
            Thumb3D = null;
            Thumbnail.Texture = thumb;
            UpdateImagePosition();
        }
        
        private void SetHasUpgrades(bool? hasUpgrades, bool bought)
        {
            if (hasUpgrades == null || (InInventory == 1 && !bought))
            {
                UpgradeBack.Visible = false;
                UpgradeButton.Visible = false;
            } else
            {
                UpgradeBack.Visible = true;
                UpgradeButton.Visible = true;
                UpgradeButton.Disabled = !hasUpgrades.Value;
            }
            UpgradeList.ReadOnly = InInventory == 2;
            SetUpgradeListVisible(false);
        }

        private void UpdateImagePosition() {
            var thumb = Thumbnail.Texture;
            if (thumb == null) return;
            float scale = Math.Min(1, 90f / Math.Max(thumb.Height, thumb.Width));
            Thumbnail.SetSize(thumb.Width * scale, thumb.Height * scale);
            var baseLoc = (Tab == 0) ? new Vector2(24, 11) : new Vector2(64, 11);
            Thumbnail.Position = baseLoc + new Vector2(45 - (thumb.Width * scale / 2), 45 - (thumb.Height * scale / 2));
        }

        public override void InternalDraw(UISpriteBatch batch)
        {
            base.InternalDraw(batch);

            float scale = 0.7f;
            Texture2D thumb = null;
            if (Thumb3D != null)
            {
                Thumb3D.Draw();
                thumb = Thumb3D.Tex;
            }

            var baseLoc = (Tab == 0) ? new Vector2(24, 11) : new Vector2(64, 11);
            baseLoc += new Vector2(45, 45);

            if (thumb != null)
            {
                var pos = baseLoc + (new Vector2(thumb.Width * scale / -2, thumb.Height * scale / -2));
                DrawLocalTexture(batch, thumb, null, pos, new Vector2(scale));
            }
        }
    }
    
}
