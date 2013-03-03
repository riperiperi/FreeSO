using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.Data.Model;
using TSOClient.LUI;

namespace TSOClient.Code.UI.Screens
{
    public class PersonSelectionEdit : GameScreen
    {
        /** UI created by script **/
        public Texture2D BackgroundImage { get; set; }
        public UIButton SkinLightButton { get; set; }
        public UIButton SkinMediumButton { get; set; }
        public UIButton SkinDarkButton { get; set; }

        public UITextEdit DescriptionTextEdit { get; set; }

        private UICollectionViewer HeadSkinBrowser;
        private UICollectionViewer BodySkinBrowser;

        private Collection MaleHeads;
        private Collection MaleOutfits;

        private AppearanceType AppearanceType = AppearanceType.Light;


        public PersonSelectionEdit()
        {
            /**
             * Data
             */
            MaleHeads = new Collection(ContentManager.GetResourceFromLongID((ulong)FileIDs.CollectionsFileIDs.ea_male_heads));
            MaleOutfits = new Collection(ContentManager.GetResourceFromLongID((ulong)FileIDs.CollectionsFileIDs.ea_male));

            /**
             * UI
             */
            var ui = this.RenderScript("personselectionedit.uis");


            DescriptionTextEdit.CurrentText = ui.GetString("DefaultAvatarDescription");

            SkinLightButton.OnButtonClick += new ButtonClickDelegate(SkinButton_OnButtonClick);
            SkinMediumButton.OnButtonClick += new ButtonClickDelegate(SkinButton_OnButtonClick);
            SkinDarkButton.OnButtonClick += new ButtonClickDelegate(SkinButton_OnButtonClick);

            HeadSkinBrowser = ui.Create<UICollectionViewer>("HeadSkinBrowser");
            HeadSkinBrowser.Init();
            this.Add(HeadSkinBrowser);

            BodySkinBrowser = ui.Create<UICollectionViewer>("BodySkinBrowser");
            BodySkinBrowser.Init();
            this.Add(BodySkinBrowser);

            SetHeads(MaleHeads);
            SetOutfits(MaleOutfits);

            this.AddAt(0, new UIImage(BackgroundImage));


            /**
             * Music
             */
            GameFacade.SoundManager.PlayBackgroundMusic(
                GameFacade.GameFilePath("music\\modes\\create\\tsocas1_v2.mp3")
            );
        }

        
        private void SetHeads(Collection collection)
        {
            var dataProvider = new List<object>();
            foreach (var outfit in collection)
            {
                var thumbID = outfit.PurchasableObject.Outfit.GetAppearance(AppearanceType).ThumbnailID;
                var thumbTexture = GetTexture(thumbID);

                dataProvider.Add(new UIGridViewerItem {
                    Data = outfit,
                    Thumb = thumbTexture
                });
            }

            HeadSkinBrowser.DataProvider = dataProvider;
        }


        private void SetOutfits(Collection collection)
        {
            var dataProvider = new List<object>();
            foreach (var outfit in collection)
            {
                var thumbID = outfit.PurchasableObject.Outfit.GetAppearance(AppearanceType).ThumbnailID;
                var thumbTexture = GetTexture(thumbID);

                dataProvider.Add(new UIGridViewerItem
                {
                    Data = outfit,
                    Thumb = thumbTexture
                });
            }

            BodySkinBrowser.DataProvider = dataProvider;
        }


        void SkinButton_OnButtonClick(UIElement button)
        {
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
            this.SetHeads(MaleHeads);
            this.SetOutfits(MaleOutfits);
        }

        


    }
}
