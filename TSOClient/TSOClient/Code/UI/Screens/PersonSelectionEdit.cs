/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Controls;
using TSOClient.LUI;
using TSOClient.Network;
using TSOClient.Code.UI.Framework.Parser;
using TSOClient.VM;
using TSOClient.Network.Events;
using Microsoft.Xna.Framework;
using ProtocolAbstractionLibraryD;
using tso.common.utils;
using tso.vitaboy;
using tso.content;

namespace TSOClient.Code.UI.Screens
{
    public class PersonSelectionEdit : GameScreen
    {
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
        private AppearanceType AppearanceType = AppearanceType.Light;
        private UIButton SelectedAppearanceButton;
        private Gender Gender = Gender.Female;

        public CityInfo SelectedCity;
        public UISim SimBox;
        private Sim Sim;

        public PersonSelectionEdit()
        {
            /**
            * Data
            */
            MaleHeads = new Collection(ContentManager.GetResourceFromLongID((ulong)FileIDs.CollectionsFileIDs.ea_male_heads));
            MaleOutfits = new Collection(ContentManager.GetResourceFromLongID((ulong)FileIDs.CollectionsFileIDs.ea_male));

            FemaleHeads = new Collection(ContentManager.GetResourceFromLongID((ulong)FileIDs.CollectionsFileIDs.ea_female_heads));
            FemaleOutfits = new Collection(ContentManager.GetResourceFromLongID((ulong)FileIDs.CollectionsFileIDs.ea_female));

            /**
             * UI
             */

            UIScript ui = null;
            if (GlobalSettings.Default.ScaleUI)
            {
                ui = this.RenderScript("personselectionedit.uis");
                this.Scale800x600 = true;
            }
            else
            {
                ui = this.RenderScript("personselectionedit" + (ScreenWidth == 1024 ? "1024" : "") + ".uis");
            }

            m_ExitButton = (UIButton)ui["ExitButton"];
            m_ExitButton.OnButtonClick += new ButtonClickDelegate(m_ExitButton_OnButtonClick);

            DescriptionTextEdit.CurrentText = ui.GetString("DefaultAvatarDescription");
            DescriptionSlider.AttachButtons(DescriptionScrollUpButton, DescriptionScrollDownButton, 1);
            DescriptionTextEdit.AttachSlider(DescriptionSlider);
            NameTextEdit.OnChange += new ChangeDelegate(NameTextEdit_OnChange);

            AcceptButton.Disabled = true;
            AcceptButton.OnButtonClick += new ButtonClickDelegate(AcceptButton_OnButtonClick);

            /** Appearance **/
            SkinLightButton.OnButtonClick += new ButtonClickDelegate(SkinButton_OnButtonClick);
            SkinMediumButton.OnButtonClick += new ButtonClickDelegate(SkinButton_OnButtonClick);
            SkinDarkButton.OnButtonClick += new ButtonClickDelegate(SkinButton_OnButtonClick);
            SelectedAppearanceButton = SkinLightButton;
            SkinLightButton.Selected = true;

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
            var bg = new UIImage(BackgroundImage);
            this.AddAt(0, bg);

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
            PlayBackgroundMusic(
                new string[] { GlobalSettings.Default.StartupPath + "\\music\\modes\\create\\tsocas1_v2.mp3" }
            );

            SimBox = new UISim();

            if (GlobalSettings.Default.ScaleUI)
            {
                SimBox.SimScale = 0.8f;
                SimBox.Position = new Microsoft.Xna.Framework.Vector2(offset.X + 140, offset.Y + 130);
            }
            else
            {
                SimBox.SimScale = 0.5f;
                SimBox.Position = new Microsoft.Xna.Framework.Vector2(offset.X + 140, offset.Y + 260);
            }

            SimBox.AutoRotate = true;
            this.Add(SimBox);

            /**
             * Init state
             */
            RefreshCollections();

            m_HeadSkinBrowser.SelectedIndex = 0;
            m_BodySkinBrowser.SelectedIndex = 0;
            FemaleButton.Selected = true;

            NetworkFacade.Controller.OnCharacterCreationProgress += new OnCharacterCreationProgressDelegate(Controller_OnCharacterCreationStatus);
        }

        public override void DeviceReset(GraphicsDevice Device)
        {
            //TODO: Rejiggle camera...
            CalculateMatrix();
        }

        /// <summary>
        /// Received status of character creation from LoginServer.
        /// </summary>
        private void Controller_OnCharacterCreationStatus(CharacterCreationStatus CCStatus)
        {
            UIAlertOptions Options = new UIAlertOptions();

            switch (CCStatus)
            {
                case CharacterCreationStatus.Success:
                    GameFacade.Controller.ShowCityTransition(SelectedCity, true);
                    break;
                case CharacterCreationStatus.NameAlreadyExisted:
                    Options.Message = "Character's name already existed!";
                    Options.Title = "Name Already Existed";
                    Options.Buttons = UIAlertButtons.OK;
                    UI.Framework.UIScreen.ShowAlert(Options, true);
                    break;
                case CharacterCreationStatus.ExceededCharacterLimit:
                    Options.Message = "Character's name exceeded 24 characters!";
                    Options.Title = "Name Too Long";
                    Options.Buttons = UIAlertButtons.OK;
                    UI.Framework.UIScreen.ShowAlert(Options, true);
                    break;
            }
        }

        private void m_ExitButton_OnButtonClick(UIElement button)
        {
            GameFacade.Kill();
        }

        private void AcceptButton_OnButtonClick(UIElement button)
        {
            var sim = new Sim(Guid.NewGuid());

            sim.Name = NameTextEdit.CurrentText;
            sim.Sex = System.Enum.GetName(typeof(Gender), Gender);
            sim.Description = DescriptionTextEdit.CurrentText;
            sim.Timestamp = DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss");
            sim.ResidingCity = SelectedCity;


            var selectedHead = (CollectionItem)((UIGridViewerItem)m_HeadSkinBrowser.SelectedItem).Data;
            var selectedBody = (CollectionItem)((UIGridViewerItem)m_BodySkinBrowser.SelectedItem).Data;
            var headPurchasable = Content.Get().AvatarPurchasables.Get(selectedHead.PurchasableOutfitId);
            var bodyPurchasable = Content.Get().AvatarPurchasables.Get(selectedBody.PurchasableOutfitId);

            sim.HeadOutfitID = headPurchasable.OutfitID;
            sim.BodyOutfitID = bodyPurchasable.OutfitID;
            sim.AppearanceType = this.AppearanceType;

            //GameFacade.Controller.ShowCity();
            PlayerAccount.CurrentlyActiveSim = sim;

            if (PlayerAccount.Sims.Count == 0)
                PlayerAccount.Sims.Add(sim);
            else if (PlayerAccount.Sims.Count == 2)
                PlayerAccount.Sims[1] = sim;
            else if (PlayerAccount.Sims.Count == 3)
                PlayerAccount.Sims[2] = sim;

            UIPacketSenders.SendCharacterCreate(sim, DateTime.Now.ToString());
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
            var headPurchasable = Content.Get().AvatarPurchasables.Get(selectedHead.PurchasableOutfitId);
            var bodyPurchasable = Content.Get().AvatarPurchasables.Get(selectedBody.PurchasableOutfitId);


            System.Diagnostics.Debug.WriteLine("Head = " + selectedHead.PurchasableOutfitId);
            System.Diagnostics.Debug.WriteLine("Body = " + selectedHead.PurchasableOutfitId);

            var headOutfit = Content.Get().AvatarOutfits.Get(headPurchasable.OutfitID);
            var bodyOutfit = Content.Get().AvatarOutfits.Get(bodyPurchasable.OutfitID);


            SimBox.Avatar.Appearance = AppearanceType;
            SimBox.Avatar.Head = headOutfit;
            SimBox.Avatar.Body = bodyOutfit;
        }

        private void NameTextEdit_OnChange(UIElement element)
        {
            AcceptButton.Disabled = NameTextEdit.CurrentText.Length == 0;
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
        
        private List<object> CollectionToDataProvider(Collection collection)
        {
            var dataProvider = new List<object>();
            foreach (var outfit in collection)
            {
                var purchasable = Content.Get().AvatarPurchasables.Get(outfit.PurchasableOutfitId);
                Outfit TmpOutfit = Content.Get().AvatarOutfits.Get(purchasable.OutfitID);
                Appearance TmpAppearance = Content.Get().AvatarAppearances.Get(TmpOutfit.GetAppearance(AppearanceType));
                ulong thumbID = TmpAppearance.ThumbnailID.Shift();

                dataProvider.Add(new UIGridViewerItem {
                    Data = outfit,
                    Thumb = new Promise<Texture2D>(x => GetTexture(thumbID))
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
