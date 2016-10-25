/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework.Parser;
using Microsoft.Xna.Framework;
using FSO.Common.Utils;
using FSO.Vitaboy;
using FSO.Content;
using FSO.Client.Controllers;
using System.Text.RegularExpressions;
using FSO.Client.GameContent;
using FSO.Client.UI.Model;
using FSO.Common;

namespace FSO.Client.UI.Screens
{
    public class PersonSelectionEdit : GameScreen
    {
        /// <summary>
        /// Must not start with whitespace
        /// May not contain numbers or special characters
        /// At least 3 characters
        /// No more than 24 characters
        /// </summary>
        private static Regex NAME_VALIDATION = new Regex("^([a-zA-Z]){1}([a-zA-Z ]){2,23}$");

        /// <summary>
        /// Only printable ascii characters
        /// Minimum 0 characters
        /// Maximum 499 characters
        /// </summary>
        private static Regex DESC_VALIDATION = new Regex("^([a-zA-Z0-9\\s\\x20-\\x7F]){0,499}$");

        /** UI created by script **/
        public Texture2D BackgroundImage { get; set; }
        public Texture2D BackgroundImageDialog { get; set; }
        public UIButton CancelButton { get; set; }
        public UIButton AcceptButton { get; set; }
        public UIButton SkinLightButton { get; set; }
        public UIButton SkinMediumButton { get; set; }
        public UIButton SkinDarkButton { get; set; }
        public UIButton FemaleButton { get; set; }
        public UIButton MaleButton { get; set; }

        public UITextEdit NameTextEdit { get; set; }
        public UITextEdit DescriptionTextEdit { get; set; }
        public UIButton DescriptionScrollUpButton { get; set; }
        public UIButton DescriptionScrollDownButton { get; set; }
        public UISlider DescriptionSlider { get; set; }
        private UIButton m_ExitButton;

        private UICollectionViewer m_HeadSkinBrowser;
        private UICollectionViewer m_BodySkinBrowser;
        
        /** Data **/
        private Collection MaleHeads;
        private Collection MaleOutfits;
        private Collection FemaleHeads;
        private Collection FemaleOutfits;

        /** State **/
        public AppearanceType AppearanceType { get; internal set; } = AppearanceType.Light;
        private UIButton SelectedAppearanceButton;
        public Gender Gender { get; internal set; } = Gender.Female;
        
        public UISim SimBox;

        /** Strings **/
        public string ProgressDialogTitle { get; set; }
        public string ProgressDialogMessage { get; set; }
        public string DefaultAvatarDescription { get; set; }

        public PersonSelectionEdit() : base()
        {
            /**
            * Data
            */
            var content = Content.Content.Get();
            MaleHeads = content.AvatarCollections.Get("ea_male_heads.col");
            MaleOutfits = content.AvatarCollections.Get("ea_male.col");

            FemaleHeads = content.AvatarCollections.Get("ea_female_heads.col");
            FemaleOutfits = content.AvatarCollections.Get("ea_female.col");

            /**
             * UI
             */

            UIScript ui = this.RenderScript("personselectionedit1024.uis");

            Position = new Vector2((GlobalSettings.Default.GraphicsWidth-1024)/2, (GlobalSettings.Default.GraphicsHeight-768)/2) * FSOEnvironment.DPIScaleFactor;

            m_ExitButton = (UIButton)ui["ExitButton"];
            m_ExitButton.OnButtonClick += new ButtonClickDelegate(m_ExitButton_OnButtonClick);

            CancelButton = (UIButton)ui["CancelButton"];
            CancelButton.OnButtonClick += new ButtonClickDelegate(CancelButton_OnButtonClick);
            //CancelButton.Disabled = true;

            DescriptionTextEdit.CurrentText = ui.GetString("DefaultAvatarDescription");
            DescriptionSlider.AttachButtons(DescriptionScrollUpButton, DescriptionScrollDownButton, 1);
            DescriptionTextEdit.AttachSlider(DescriptionSlider);
            DescriptionTextEdit.CurrentText = DefaultAvatarDescription;
            DescriptionTextEdit.OnChange += DescriptionTextEdit_OnChange;

            NameTextEdit.OnChange += new ChangeDelegate(NameTextEdit_OnChange);
            NameTextEdit.CurrentText = GlobalSettings.Default.LastUser;

            AcceptButton.Disabled = NameTextEdit.CurrentText.Length == 0;
            AcceptButton.OnButtonClick += new ButtonClickDelegate(AcceptButton_OnButtonClick);

            /** Appearance **/
            SkinLightButton.OnButtonClick += new ButtonClickDelegate(SkinButton_OnButtonClick);
            SkinMediumButton.OnButtonClick += new ButtonClickDelegate(SkinButton_OnButtonClick);
            SkinDarkButton.OnButtonClick += new ButtonClickDelegate(SkinButton_OnButtonClick);
            SelectedAppearanceButton = SkinLightButton;

            m_HeadSkinBrowser = ui.Create<UICollectionViewer>("HeadSkinBrowser");
            m_HeadSkinBrowser.OnChange += new ChangeDelegate(HeadSkinBrowser_OnChange);
            m_HeadSkinBrowser.Init();
            this.Add(m_HeadSkinBrowser);

            m_BodySkinBrowser = ui.Create<UICollectionViewer>("BodySkinBrowser");
            m_BodySkinBrowser.OnChange += new ChangeDelegate(BodySkinBrowser_OnChange);
            m_BodySkinBrowser.Init();
            this.Add(m_BodySkinBrowser);

            FemaleButton.OnButtonClick += new ButtonClickDelegate(GenderButton_OnButtonClick);
            MaleButton.OnButtonClick += new ButtonClickDelegate(GenderButton_OnButtonClick);

            /** Backgrounds **/
            var bg = new UIImage(BackgroundImage).With9Slice(128,128, 84, 84);
            this.AddAt(0, bg);
            bg.SetSize(GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight);
            bg.Position = new Vector2((GlobalSettings.Default.GraphicsWidth - 1024) / -2, (GlobalSettings.Default.GraphicsHeight - 768) / -2);

            var offset = new Vector2(0, 0);
            if (BackgroundImageDialog != null)
            {
                offset = new Vector2(112, 84);

                this.AddAt(1, new UIImage(BackgroundImageDialog)
                {
                    X = 112,
                    Y = 84
                });
            }

            /**
             * Music
             */
            HIT.HITVM.Get().PlaySoundEvent(UIMusic.CAS);

            SimBox = new UISim();
            SimBox.Position = new Vector2(offset.X + 70, offset.Y + 88);
            SimBox.Size = new Vector2(140,200);
            SimBox.AutoRotate = true;
            this.Add(SimBox);

            /**
             * Init state
             */

            if (GlobalSettings.Default.DebugGender)
            {
                Gender = Gender.Male;
                MaleButton.Selected = true;
                FemaleButton.Selected = false;
            }
            else
            {
                Gender = Gender.Female;
                MaleButton.Selected = false;
                FemaleButton.Selected = true;
            }

            AppearanceType = (AppearanceType)GlobalSettings.Default.DebugSkin;

            SkinLightButton.Selected = false;
            SkinMediumButton.Selected = false;
            SkinDarkButton.Selected = false;

            switch (AppearanceType)
            {
                case AppearanceType.Light:
                    SkinLightButton.Selected = true; break;
                case AppearanceType.Medium:
                    SkinMediumButton.Selected = true; break;
                case AppearanceType.Dark:
                    SkinDarkButton.Selected = true; break;
            }

            RefreshCollections();

            SearchCollectionForInitID(GlobalSettings.Default.DebugHead, GlobalSettings.Default.DebugBody);
        }

        private UIAlert _ProgressAlert;

        public void ShowCreationProgressBar(bool show)
        {
            if (show)
            {
                if (_ProgressAlert == null)
                {
                    _ProgressAlert = GlobalShowAlert(new UIAlertOptions
                    {
                        Message = ProgressDialogMessage,
                        Title = ProgressDialogTitle,
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

        public override void DeviceReset(GraphicsDevice Device)
        {
            CalculateMatrix();
        }


        public string Name
        {
            get { return NameTextEdit.CurrentText; }
        }

        public string Description
        {
            get { return DescriptionTextEdit.CurrentText; }
        }

        public ulong HeadOutfitId
        {
            get
            {
                var selectedHead = (CollectionItem)((UIGridViewerItem)m_HeadSkinBrowser.SelectedItem).Data;
                var headPurchasable = Content.Content.Get().AvatarPurchasables.Get(selectedHead.PurchasableOutfitId);
                return headPurchasable.OutfitID;
            }
        }

        public ulong BodyOutfitId
        {
            get
            {
                var selectedBody = (CollectionItem)((UIGridViewerItem)m_BodySkinBrowser.SelectedItem).Data;
                var bodyPurchasable = Content.Content.Get().AvatarPurchasables.Get(selectedBody.PurchasableOutfitId);
                return bodyPurchasable.OutfitID;
            }
        }

        private void m_ExitButton_OnButtonClick(UIElement button)
        {
            GameFacade.Kill();
        }

        private void CancelButton_OnButtonClick(UIElement button)
        {
            ((PersonSelectionEditController)Controller).Cancel();
        }

        private void AcceptButton_OnButtonClick(UIElement button)
        {
            ((PersonSelectionEditController)Controller).Create();
        }

        private void HeadSkinBrowser_OnChange(UIElement element)
        {
            RefreshSim();
        }

        private void BodySkinBrowser_OnChange(UIElement element)
        {
            RefreshSim();
        }

        private void RefreshSim()
        {
            var selectedHead = (CollectionItem)((UIGridViewerItem)m_HeadSkinBrowser.SelectedItem).Data;
            var selectedBody = (CollectionItem)((UIGridViewerItem)m_BodySkinBrowser.SelectedItem).Data;

            var headPurchasable = Content.Content.Get().AvatarPurchasables.Get(selectedHead.PurchasableOutfitId);
            var bodyPurchasable = Content.Content.Get().AvatarPurchasables.Get(selectedBody.PurchasableOutfitId);
            
            var headOutfit = Content.Content.Get().AvatarOutfits.Get(headPurchasable.OutfitID);
            var bodyOutfit = Content.Content.Get().AvatarOutfits.Get(bodyPurchasable.OutfitID);

            SimBox.Avatar.Appearance = AppearanceType;
            SimBox.Avatar.Head = headOutfit;
            SimBox.Avatar.Body = bodyOutfit;
            SimBox.Avatar.Handgroup = bodyOutfit;
        }

        private void NameTextEdit_OnChange(UIElement element)
        {
            UpdateAcceptButtonState();
        }

        private void DescriptionTextEdit_OnChange(UIElement element)
        {
            UpdateAcceptButtonState();
        }

        private void UpdateAcceptButtonState()
        {
            var enabled = true;
            if (!NAME_VALIDATION.IsMatch(NameTextEdit.CurrentText))
            {
                enabled = false;
            }

            if (!DESC_VALIDATION.IsMatch(DescriptionTextEdit.CurrentText))
            {
                enabled = false;
            }

            AcceptButton.Disabled = !enabled;
        }

        private void GenderButton_OnButtonClick(UIElement button)
        {
            if (button == MaleButton)
            {
                Gender = Gender.Male;
                MaleButton.Selected = true;
                FemaleButton.Selected = false;
            }
            else if (button == FemaleButton)
            {
                Gender = Gender.Female;
                MaleButton.Selected = false;
                FemaleButton.Selected = true;
            }
            RefreshCollections();
        }

        private void RefreshCollections()
        {
            var oldHeadIndex = m_HeadSkinBrowser.SelectedIndex;
            var oldBodyIndex = m_BodySkinBrowser.SelectedIndex;

            if (Gender == Gender.Male)
            {
                m_HeadSkinBrowser.DataProvider = CollectionToDataProvider(MaleHeads);
                m_BodySkinBrowser.DataProvider = CollectionToDataProvider(MaleOutfits);
            }
            else
            {
                m_HeadSkinBrowser.DataProvider = CollectionToDataProvider(FemaleHeads);
                m_BodySkinBrowser.DataProvider = CollectionToDataProvider(FemaleOutfits);
            }

            m_HeadSkinBrowser.SelectedIndex = Math.Min(oldHeadIndex, m_HeadSkinBrowser.DataProvider.Count);
            m_BodySkinBrowser.SelectedIndex = Math.Min(oldBodyIndex, m_BodySkinBrowser.DataProvider.Count);
            RefreshSim();
        }

        private void SearchCollectionForInitID(ulong headID, ulong bodyID)
        {
            var purchs = Content.Content.Get().AvatarPurchasables;

            int index = m_BodySkinBrowser.DataProvider.FindIndex(x =>
                purchs.Get(
                ((CollectionItem)(((UIGridViewerItem)x).Data)).PurchasableOutfitId
                ).OutfitID == bodyID
            );

            if (index == -1) index = 0;
            m_BodySkinBrowser.SelectedIndex = index;

            index = m_HeadSkinBrowser.DataProvider.FindIndex(x =>
                purchs.Get(
                ((CollectionItem)(((UIGridViewerItem)x).Data)).PurchasableOutfitId
                ).OutfitID == headID
            );

            if (index == -1) index = 0;
            m_HeadSkinBrowser.SelectedIndex = index;

            RefreshSim();
        }
        
        private List<object> CollectionToDataProvider(Collection collection)
        {
            var dataProvider = new List<object>();
            foreach (var outfit in collection)
            {
                var purchasable = Content.Content.Get().AvatarPurchasables.Get(outfit.PurchasableOutfitId);
                Outfit TmpOutfit = Content.Content.Get().AvatarOutfits.Get(purchasable.OutfitID);
                Appearance TmpAppearance = Content.Content.Get().AvatarAppearances.Get(TmpOutfit.GetAppearance(AppearanceType));
                FSO.Common.Content.ContentID thumbID = TmpAppearance.ThumbnailID;

                dataProvider.Add(new UIGridViewerItem {
                    Data = outfit,
                    Thumb = new Promise<Texture2D>(x => Content.Content.Get().AvatarThumbnails.Get(thumbID).Get(GameFacade.GraphicsDevice))
                });
            }
            return dataProvider;
        }

        private void SkinButton_OnButtonClick(UIElement button)
        {
            SelectedAppearanceButton.Selected = false;
            SelectedAppearanceButton = (UIButton)button;
            SelectedAppearanceButton.Selected = true;

            var type = AppearanceType.Light;

            if (button == SkinMediumButton)
            {
                type = AppearanceType.Medium;
            }
            else if (button == SkinDarkButton)
            {
                type = AppearanceType.Dark;
            }

            this.AppearanceType = type;
            RefreshCollections();
        }
    }

    public enum Gender
    {
        Male,
        Female
    }
}
