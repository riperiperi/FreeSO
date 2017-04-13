using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework.Parser;
using FSO.Common.Serialization;
using FSO.Common.Utils;
using FSO.Content.Model;
using FSO.SimAntics;
using FSO.SimAntics.Engine.TSOGlobalLink.Model;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.Vitaboy;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public abstract class UIAbstractRackEOD : UIEOD
    {
        private string UIScriptPath;
        protected UIScript Script;
        protected RackType RackType { get; set; }
        protected UICollectionViewer OutfitBrowser;
        public UITextEdit RackName { get; set; }
        /**
         * Data
         */
        protected VMGLOutfit[] Stock;

        /**
         * Outfit prices
         */
        public UITextEdit Outfit1Price { get; set; }
        public UITextEdit Outfit2Price { get; set; }
        public UITextEdit Outfit3Price { get; set; }
        public UITextEdit Outfit4Price { get; set; }
        public UITextEdit Outfit5Price { get; set; }
        public UITextEdit Outfit6Price { get; set; }
        public UITextEdit Outfit7Price { get; set; }
        public UITextEdit Outfit8Price { get; set; }
        protected UITextEdit[] OutfitPrices;

        public UIAbstractRackEOD(UIEODController controller, string uiScript) : base(controller)
        {
            UIScriptPath = uiScript;

            InitUI();
            InitEOD();
        }

        protected virtual void InitUI()
        {
            Script = this.RenderScript(UIScriptPath);
            AddAt(0, Script.Create<UIImage>("controlBackground"));

            OutfitBrowser = Script.Create<UICollectionViewer>("OutfitBrowser");
            OutfitBrowser.PaginationStyle = UIPaginationStyle.LEFT_RIGHT_ARROWS;
            OutfitBrowser.Init();
            OutfitBrowser.OnSelectedPageChanged += x => UpdateUIState();
            OutfitBrowser.OnChange += x => UpdateUIState();
            Add(OutfitBrowser);

            OutfitPrices = new UITextEdit[] { Outfit1Price, Outfit2Price, Outfit3Price, Outfit4Price, Outfit5Price, Outfit6Price, Outfit7Price, Outfit8Price };
        }

        protected virtual void InitEOD()
        {
            PlaintextHandlers["rack_show"] = ShowEOD;
            BinaryHandlers["set_outfits"] = ShowStock;
        }

        protected virtual void UpdateUIState()
        {
            //Align pricing with scroll position
            for (var i = 0; i < 8; i++)
            {
                var priceField = OutfitPrices[i];
                var priceIndex = GetPriceIndex(i);

                if (priceIndex != -1){
                    priceField.CurrentText = Stock[priceIndex].sale_price.ToString();
                    priceField.Mode = UITextEditMode.Editor;
                }else{
                    priceField.CurrentText = "";
                    priceField.Mode = UITextEditMode.ReadOnly;
                }
            }
        }

        protected virtual void ShowStock(string evt, byte[] body)
        {
            var packet = IoBufferUtils.Deserialize<VMEODRackStockResponse>(body, null);
            Stock = packet.Outfits;
            var appearanceType = GetAppearanceType();

            var dataProvider = new List<object>();

            foreach (var outfit in Stock){
                //TODO: Use current avatars appearance type
                Outfit TmpOutfit = Content.Content.Get().AvatarOutfits.Get(outfit.asset_id);
                Appearance TmpAppearance = Content.Content.Get().AvatarAppearances.Get(TmpOutfit.GetAppearance(appearanceType));
                FSO.Common.Content.ContentID thumbID = TmpAppearance.ThumbnailID;

                dataProvider.Add(new UIGridViewerItem {
                    Data = outfit,
                    Thumb = new Promise<Texture2D>(x => Content.Content.Get().AvatarThumbnails.Get(thumbID).Get(GameFacade.GraphicsDevice))
                });
            }
            OutfitBrowser.DataProvider = dataProvider;
            UpdateUIState();
        }

        protected virtual void SetRackType(RackType type)
        {
            RackType = type;
        }

        protected virtual void ShowEOD(string evt, string txt)
        {
            SetRackType((RackType)short.Parse(txt));
            var options = GetEODOptions();
            UpdateUIState();
            Controller.ShowEODMode(options);
        }

        public override void OnClose()
        {
            Send("close", "");
            base.OnClose();
        }



        /**
         * Data
         */
        public bool IsFull
        {
            get
            {
                if(Stock != null && Stock.Length >= VMEODRackOwnerPlugin.MAX_OUTFITS){
                    return true;
                }
                return false;
            }
        }

        public VMGLOutfit GetSelectedOutfit()
        {
            var selectedItem = OutfitBrowser.SelectedItem as UIGridViewerItem;
            if(selectedItem != null)
            {
                return (VMGLOutfit)selectedItem.Data;
            }
            return null;
        }

        public AppearanceType GetAppearanceType()
        {
            if (LotController != null && LotController.ActiveEntity is VMAvatar)
            {
                var avatar = (VMAvatar)LotController.ActiveEntity;
                return avatar.Avatar.Appearance;
            }
            return AppearanceType.Light;
        }

        /**
         * UI
         */

        protected abstract EODLiveModeOpt GetEODOptions();

        protected int GetPriceIndex(int i)
        {
            var offset = OutfitBrowser.SelectedPage * OutfitBrowser.ItemsPerPage;
            var stockLength = Stock != null ? Stock.Length : 0;

            if (offset + i < stockLength)
            {
                return offset + i;
            }
            else
            {
                return -1;
            }
        }
    }
}
