using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Common.Utils;
using Microsoft.Xna.Framework.Graphics;
using FSO.Vitaboy;
using FSO.SimAntics;

namespace FSO.Client.UI.Panels.EODs
{
    class UITrunkEOD : UIEOD
    {
        private Collection TrunkOutfits { get; set; }
        private CollectionItem SelectedOutfit { get; set; }
        private ulong SelectedOutfitID;
        private UICollectionViewer CostumeOptions { get; set; }
        private UIScript Script;

        public AppearanceType UserAppearanceType { get; internal set; } = AppearanceType.Light;

        public UIButton AcceptButton { get; set; }
        public UIImage LargeThumbnail { get; set; }
        public UIImage SubpanelBackground { get; set; }

        public UITrunkEOD(UIEODController controller) : base(controller)
        {
            InitUI();
            AddListeners();
            PlaintextHandlers["trunk_fill_UI"] = FillUIHandler;
        }
        public override void OnClose()
        {
            Send("trunk_close_UI", "");
            CloseInteraction();
            base.OnClose();
        }
        private void FillUIHandler(string evt, string collectionPath)
        {
            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 1,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.None,
                Timer = EODTimer.None,
                Expandable = false
            });
            // get the collection using the directory
            var content = Content.Content.Get();
            TrunkOutfits = content.AvatarCollections.Get(collectionPath);

            // get the skin color of the user
            var avatar = (VMAvatar)LotController.ActiveEntity;
            UserAppearanceType = avatar.Avatar.Appearance;

            // setup the collection view
            CostumeOptions.DataProvider = CollectionToDataProvider(TrunkOutfits);
        }
        private void InitUI()
        {
            Script = this.RenderScript("trunkeod.uis");
            // add background image and thumbnail
            SubpanelBackground = Script.Create<UIImage>("SubpanelBackground");
            AddAt(0, SubpanelBackground);
            LargeThumbnail = Script.Create<UIImage>("LargeThumbnail");
            Add(LargeThumbnail);

            CostumeOptions = Script.Create<UICollectionViewer>("BodySkinBrowser");
            CostumeOptions.Init();
            this.Add(CostumeOptions);
        }
        private void CostumeOptionsChangeHandler(UIElement element)
        {
            SelectedOutfit = (CollectionItem)((UIGridViewerItem)CostumeOptions.SelectedItem).Data;
            var costumePurchasable = Content.Content.Get().AvatarPurchasables.Get(SelectedOutfit.PurchasableOutfitId);
            SelectedOutfitID = costumePurchasable.OutfitID;
            LargeThumbnail.Texture = ((UIGridViewerItem)CostumeOptions.SelectedItem).Thumb.Get();
        }
        private void AddListeners()
        {
            AcceptButton.OnButtonClick += clickedButton => { Send("trunk_wear_costume", SelectedOutfitID + ""); };
            CostumeOptions.OnChange += new ChangeDelegate(CostumeOptionsChangeHandler);
        }
        /*
         * Shamelessly copied from Fso.Client.UI.Screens.PersonSelectionEdit.cs
         */
        private List<object> CollectionToDataProvider(Collection collection)
        {
            var dataProvider = new List<object>();
            foreach (var outfit in collection)
            {
                var purchasable = Content.Content.Get().AvatarPurchasables.Get(outfit.PurchasableOutfitId);
                Outfit TmpOutfit = Content.Content.Get().AvatarOutfits.Get(purchasable.OutfitID);
                Appearance TmpAppearance = Content.Content.Get().AvatarAppearances.Get(TmpOutfit.GetAppearance(UserAppearanceType));
                FSO.Common.Content.ContentID thumbID = TmpAppearance.ThumbnailID;

                dataProvider.Add(new UIGridViewerItem
                {
                    Data = outfit,
                    Thumb = new Promise<Texture2D>(x => Content.Content.Get().AvatarThumbnails.Get(thumbID).Get(GameFacade.GraphicsDevice))
                });
            }
            return dataProvider;
        }
    }
}
