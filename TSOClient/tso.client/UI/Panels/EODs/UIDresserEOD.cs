using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework.Parser;
using Microsoft.Xna.Framework.Graphics;
using FSO.SimAntics.Engine.Scopes;
using FSO.Common.Serialization;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.Engine.TSOGlobalLink.Model;
using FSO.Vitaboy;
using FSO.Common.Utils;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIDresserEOD : UIEOD
    {
        protected UIScript Script;
        
        /** Script ui **/
        public Texture2D imageBackgroundDay { get; set; }
        public Texture2D imageBackgroundSleep { get; set; }
        public Texture2D imageBackgroundSwim { get; set; }

        public UIButton btnDay { get; set; }
        public UIButton btnSleep { get; set; }
        public UIButton btnSwim { get; set; }
        public UIButton btnDecorHead { get; set; }
        public UIButton btnDecorBack { get; set; }
        public UIButton btnDecorShoes { get; set; }
        public UIButton btnDecorTail { get; set; }

        public UIButton btnDelete { get; set; }
        public UIButton btnAccept { get; set; }

        private UIImage Background;
        private VMPersonSuits SelectedTab = VMPersonSuits.DefaultDaywear;

        protected UICollectionViewer OutfitBrowser;
        private VMGLOutfit[] Outfits;

        public UIDresserEOD(UIEODController controller) : base(controller)
        {
            InitUI();
            InitEOD();
        }

        private void BtnAccept_OnButtonClick(Framework.UIElement button)
        {
            var outfit = GetSelectedOutfit();
            if (outfit == null) { return; }
            Send("dresser_change_outfit", outfit.outfit_id.ToString());
        }

        private void UpdateUIState()
        {
            var selected = GetSelectedOutfit() != null;
            btnAccept.Disabled = !selected;
            btnDelete.Disabled = !selected;
        }

        private void UpdateDataProvider()
        {
            var dataProvider = new List<object>();

            foreach (var outfit in Outfits.Where(x => x.outfit_type == (byte)SelectedTab))
            {
                //TODO: Use current avatars appearance type
                Outfit TmpOutfit = Content.Content.Get().AvatarOutfits.Get(outfit.asset_id);
                Appearance TmpAppearance = Content.Content.Get().AvatarAppearances.Get(TmpOutfit.GetAppearance(AppearanceType.Light));
                FSO.Common.Content.ContentID thumbID = TmpAppearance.ThumbnailID;

                dataProvider.Add(new UIGridViewerItem
                {
                    Data = outfit,
                    Thumb = new Promise<Texture2D>(x => Content.Content.Get().AvatarThumbnails.Get(thumbID).Get(GameFacade.GraphicsDevice))
                });
            }
            OutfitBrowser.DataProvider = dataProvider;
        }

        private void InitUI()
        {
            Script = this.RenderScript("dressereod.uis");
            Background = Script.Create<UIImage>("controlBackgroundPos");
            Background.Texture = imageBackgroundDay;
            AddAt(0, Background);

            btnDay.OnButtonClick += SetTab;
            btnSleep.OnButtonClick += SetTab;
            btnSwim.OnButtonClick += SetTab;
            btnDecorBack.OnButtonClick += SetTab;
            btnDecorHead.OnButtonClick += SetTab;
            btnDecorShoes.OnButtonClick += SetTab;
            btnDecorTail.OnButtonClick += SetTab;

            OutfitBrowser = Script.Create<UICollectionViewer>("OutfitBrowser");
            OutfitBrowser.PaginationStyle = UIPaginationStyle.NONE;
            OutfitBrowser.Init();
            OutfitBrowser.OnSelectedPageChanged += x => UpdateUIState();
            OutfitBrowser.OnChange += x => UpdateUIState();
            Add(OutfitBrowser);

            btnAccept.OnButtonClick += BtnAccept_OnButtonClick;
        }

        private void SetTab(Framework.UIElement button)
        {
            if(button == btnDay){
                SelectedTab = VMPersonSuits.DefaultDaywear;
            }else if(button == btnSleep)
            {
                SelectedTab = VMPersonSuits.DefaultSleepwear;
            }else if(button == btnSwim)
            {
                SelectedTab = VMPersonSuits.DefaultSwimwear;
            }else if(button == btnDecorHead)
            {
                SelectedTab = VMPersonSuits.DecorationHead;
            }else if(button == btnDecorBack)
            {
                SelectedTab = VMPersonSuits.DecorationBack;
            }else if(button == btnDecorShoes)
            {
                SelectedTab = VMPersonSuits.DecorationShoes;
            }else if(button == btnDecorTail)
            {
                SelectedTab = VMPersonSuits.DecorationTail;
            }

            UpdateDataProvider();
            UpdateUIState();
        }

        protected void InitEOD()
        {
            PlaintextHandlers["dresser_show"] = ShowEOD;
            BinaryHandlers["set_outfits"] = SetOutfits;
        }

        protected virtual void SetOutfits(string evt, byte[] body)
        {
            var packet = IoBufferUtils.Deserialize<VMEODRackStockResponse>(body, null);
            Outfits = packet.Outfits;
            UpdateDataProvider();
            UpdateUIState();
        }

        protected virtual void ShowEOD(string evt, string txt)
        {
            var options = GetEODOptions();
            UpdateUIState();
            Controller.ShowEODMode(options);
        }

        protected EODLiveModeOpt GetEODOptions()
        {
            return new EODLiveModeOpt
            {
                Buttons = 2,
                Expandable = false,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Timer = EODTimer.None,
                Tips = EODTextTips.None
            };
        }

        public override void OnClose()
        {
            Send("close", "");
            base.OnClose();
        }

        public VMGLOutfit GetSelectedOutfit()
        {
            var selectedItem = OutfitBrowser.SelectedItem as UIGridViewerItem;
            if (selectedItem != null)
            {
                return (VMGLOutfit)selectedItem.Data;
            }
            return null;
        }
    }
    
}
