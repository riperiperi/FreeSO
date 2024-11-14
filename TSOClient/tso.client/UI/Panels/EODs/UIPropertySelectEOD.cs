using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.Neighborhoods;
using FSO.Client.Utils;
using FSO.Common.DatabaseService.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIPropertySelectEOD : UIEOD
    {
        private uint LotID;
        private string Name;

        public UISlider SearchSlider { get; set; }
        public UIButton WideSearchUpButton { get; set; }
        public UIButton NarrowSearchButton { get; set; }
        public UIButton SearchScrollUpButton { get; set; }
        public UIButton SearchScrollDownButton { get; set; }
        public UIListBox SearchResult { get; set; }
        public UITextEdit SearchText { get; set; }
        public UILabel NoSearchResultsText { get; set; }
        public UIListBoxTextStyle ListBoxColors { get; set; }

        private UIImage Background;

        private bool PendingLotSearch;

        private List<GizmoLotSearchResult> LotResults;

        private UIImage PropertyButtonBG;
        private UILabel PropertyButtonName;
        private UILotThumbButtonAuto LotThumbButton;

        public UIPropertySelectEOD(UIEODController controller) : base(controller)
        {
            var gd = GameFacade.GraphicsDevice;

            Texture2D BackgroundTexture = GetTexture((ulong)0x0000085A00000001); // ./uigraphics/gizmo/gizmo_searchbackground.bmp
            Texture2D NarrowSearchButtonTexture = GetTexture((ulong)0x0000030B00000001); // ./uigraphics/gizmo/gizmo_narrowsearchbtn.bmp
            Texture2D WideSearchUpButtonTexture = GetTexture((ulong)0x0000031800000001); // ./uigraphics/gizmo/gizmo_widesearchbtn.bmp
            Texture2D SearchScrollUpButtonTexture = GetTexture((ulong)0x0000031200000001); // ./uigraphics/gizmo/gizmo_scrollupbtn.bmp
            Texture2D SearchScrollDownButtonTexture = GetTexture((ulong)0x0000031100000001); // ./uigraphics/gizmo/gizmo_scrolldownbtn.bmp
            Texture2D ScrollbarTexture = GetTexture((ulong)0x0000031000000001); // ./uigraphics/gizmo/gizmo_scrollbarimg.bmp

            var ui = Content.Content.Get().CustomUI;
            Texture2D ButtonSeatTexture = ui.Get("neighp_btab_seat.png").Get(gd);

            var searchOffset = new Vector2(72, 63);
            int extraHeight = 80;

            Background = new UIImage(BackgroundTexture);
            Background.With9Slice(0, 0, 70, 30);
            Background.Position = new Vector2(157, 28) + searchOffset;
            Background.SetSize(BackgroundTexture.Width, BackgroundTexture.Height + extraHeight);
            Add(Background);

            var whitePx = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
            var backgroundTouchup = new UIImage(whitePx);
            backgroundTouchup.SetSize(76, 43);
            backgroundTouchup.Position = new Vector2(376, 91);
            backgroundTouchup.BlendColor = new Color(80, 119, 163);
            Add(backgroundTouchup);

            NarrowSearchButton = new UIButton(NarrowSearchButtonTexture);
            NarrowSearchButton.OnButtonClick += SendSearch;
            NarrowSearchButton.Position = new Vector2(309, 39) + searchOffset;

            WideSearchUpButton = new UIButton(WideSearchUpButtonTexture);
            WideSearchUpButton.OnButtonClick += SendSearch;
            WideSearchUpButton.Position = new Vector2(349, 39) + searchOffset;

            var seat1 = new UIImage(ButtonSeatTexture);
            seat1.SetSize(34, 34);
            seat1.Position = NarrowSearchButton.Position - new Vector2(3, 3);

            var seat2 = new UIImage(ButtonSeatTexture);
            seat2.SetSize(34, 34);
            seat2.Position = WideSearchUpButton.Position - new Vector2(3, 3);

            Add(seat1);
            Add(seat2);
            Add(NarrowSearchButton);
            Add(WideSearchUpButton);

            SearchScrollUpButton = new UIButton(SearchScrollUpButtonTexture);
            SearchScrollUpButton.Position = new Vector2(372, 75) + searchOffset;
            Add(SearchScrollUpButton);

            SearchScrollDownButton = new UIButton(SearchScrollDownButtonTexture);
            SearchScrollDownButton.Position = new Vector2(372, 175) + searchOffset + new Vector2(0, extraHeight);
            Add(SearchScrollDownButton);

            SearchSlider = new UISlider();
            SearchSlider.MinValue = 0;
            SearchSlider.MaxValue = 10;
            SearchSlider.Texture = ScrollbarTexture;
            SearchSlider.AttachButtons(SearchScrollUpButton, SearchScrollDownButton, 1);
            SearchSlider.Position = new Vector2(376, 84) + searchOffset;
            SearchSlider.Orientation = 1;
            SearchSlider.SetSize(10, 90 + extraHeight);

            Add(SearchSlider);

            SearchResult = new UIListBox();
            SearchResult.AttachSlider(SearchSlider);
            SearchResult.OnDoubleClick += SearchResult_OnDoubleClick;
            SearchResult.Size = new Vector2(188, 108 + extraHeight);
            SearchResult.Mask = true;
            SearchResult.Position = new Vector2(176, 79) + searchOffset;
            SearchResult.VisibleRows = 12;
            SearchResult.Columns = new UIListBoxColumnCollection()
            {
                new UIListBoxColumn() { Width = 25, Alignment = TextAlignment.Left },
                new UIListBoxColumn() { Width = 163, Alignment = TextAlignment.Left }
            };
            var searchFont = TextStyle.DefaultLabel.Clone();
            searchFont.Size = 8;
            SearchResult.FontStyle = searchFont;
            SearchResult.SelectionFillColor = new Color(250, 200, 140);
            Add(SearchResult);

            SearchText = new UITextEdit();
            SearchText.OnEnterPress += (elem) => { SendSearch(WideSearchUpButton); };
            SearchText.Position = new Vector2(164, 45) + searchOffset;
            SearchText.SetSize(128, 17);
            SearchText.MaxLines = 1;
            SearchText.MaxChars = 32;
            SearchText.FrameColor = new Color(255, 249, 157);
            SearchText.TextStyle = searchFont.Clone();
            Add(SearchText);

            ListBoxColors = new UIListBoxTextStyle(searchFont)
            {
                NormalColor = new Color(247, 232, 145),
                SelectedColor = new Color(0, 0, 0),
                HighlightedColor = new Color(255, 255, 255),
                DisabledColor = new Color(150, 150, 150)
            };

            NoSearchResultsText = new UILabel()
            {
                Position = new Vector2(176, 79) + searchOffset,
                Size = new Vector2(188, 90),
                CaptionStyle = TextStyle.DefaultLabel.Clone(),
            };

            NoSearchResultsText.CaptionStyle.Size = 9;
            NoSearchResultsText.CaptionStyle.Color = new Color(255, 249, 157);
            Add(NoSearchResultsText);

            var whiteText = TextStyle.DefaultLabel.Clone();
            whiteText.Color = Color.White;
            whiteText.Shadow = true;

            Add(PropertyButtonName = new UILabel()
            {
                Position = new Vector2(48, 254),
                Size = new Vector2(151, 1),
                Alignment = TextAlignment.Center | TextAlignment.Top,
                CaptionStyle = whiteText,
                Caption = "",
                Wrapped = true,
                MaxLines = 4
            });
            PropertyButtonName.Caption = GameFacade.Strings.GetString("f120", "28");

            Add(PropertyButtonBG = new UIImage(ui.Get("bulletin_post_lot_bg.png").Get(gd))
            {
                Position = new Vector2(53, 153)
            });

            Add(LotThumbButton = new UILotThumbButtonAuto()
            {
                Position = new Vector2(59, 159)
            });
            LotThumbButton.OnNameChange += (id, name) =>
            {
                if (id == 0)
                {
                    Name = null;
                    PropertyButtonName.Caption = GameFacade.Strings.GetString("f120", "28");
                }
                else
                {
                    Name = name;
                    PropertyButtonName.Caption = name;
                }
            };
            LotThumbButton.OnLotClick += PropertyButtonClick;
            LotThumbButton.Init(GetTexture(0x0000079300000001), GetTexture(0x0000079300000001));

            ControllerUtils.BindController<PropertySelectController>(this);

            BinaryHandlers["property_show"] = B_Show;
        }

        private void PropertyButtonClick(UIElement button)
        {
            FindController<CoreGameScreenController>()?.ShowLotPage(LotThumbButton.LotId);
        }

        private void UpdateSelectedLot(uint lotID)
        {
            LotID = lotID;

            LotThumbButton.LotId = lotID;
        }

        private void SearchResult_OnDoubleClick(UIElement button)
        {
            if (SearchResult.SelectedItem == null) { return; }
            var item = SearchResult.SelectedItem.Data as SearchResponseItem;
            if (item == null) { return; }

            UpdateSelectedLot(item.EntityId);

            Name = item.Name;
        }

        private void SendSearch(UIElement button)
        {
            var exact = button == NarrowSearchButton;

            PendingLotSearch = true;

            UpdateUI();
            ((PropertySelectController)Controller).Search(SearchText.CurrentText, exact);
        }

        public void SetResults(List<GizmoLotSearchResult> results)
        {
            PendingLotSearch = false;
            LotResults = results;
            UpdateUI();
        }

        private void UpdateUI()
        {
            SearchResult.Items.Clear();

            var rank = 1;

            NarrowSearchButton.Disabled = WideSearchUpButton.Disabled = PendingLotSearch;

            if (LotResults != null)
            {
                SearchResult.Items.AddRange(LotResults.Select(x =>
                {
                    return new UIListBoxItem(x.Result, new object[] { (rank++).ToString(), x.Result.Name })
                    {
                        CustomStyle = ListBoxColors,
                        UseDisabledStyleByDefault = new ValuePointer(x, "IsOffline")
                    };
                }));
            }

            NoSearchResultsText.Visible = SearchResult.Items.Count == 0;
            SearchResult.Items = SearchResult.Items;
        }


        public void B_Show(string evt, byte[] data)
        {
            EODController.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Expandable = false,
                Expanded = true,
                Height = EODHeight.ExtraTall,
                Length = EODLength.Medium,
                Timer = EODTimer.None,
                Tips = EODTextTips.Short
            });

            SetTip(GameFacade.Strings.GetString("f120", "40"));

            if (data.Length != 4)
            {
                return;
            }

            UpdateSelectedLot(BitConverter.ToUInt32(data, 0));
        }

        public override void OnClose()
        {
            if (Name != null || LotID == 0)
            {
                var nameString = Encoding.ASCII.GetBytes((Name != null && LotID != 0) ? Name : "");
                var lotId = BitConverter.GetBytes(LotID);

                var result = new byte[nameString.Length + lotId.Length];
                lotId.CopyTo(result, 0);
                if (nameString.Length > 0)
                {
                    nameString.CopyTo(result, 4);
                }

                Send("property_select", result);
            }
            Send("close", "");
            base.OnClose();
        }
    }


}
