using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client.UI.Framework;
using FSO.Common.DataService.Model;
using System.Collections.Immutable;
using FSO.Client.Controllers;
using FSO.Common.Utils;
using FSO.Client.Controllers.Panels;

namespace FSO.Client.UI.Panels
{
    public class UIRelationshipDialog : UIDialog
    {
        private UIImage InnerBackground;
        private UIButton IncomingButton;
        private UIButton OutgoingButton;
        private UILabel SortLabel;
        private UILabel SearchLabel;
        private UITextBox SearchBox;

        private UIButton SortFriendButton;
        private UIButton SortEnemyButton;
        private UIButton SortAlmostFriendButton;
        private UIButton SortAlmostEnemyButton;
        private UIButton SortRoommateButton;

        private UISlider ResultsSlider;
        private UIButton SliderUpButton;
        private UIButton SliderDownButton;

        private UILabel FriendLabel;
        private UILabel IncomingLabel;

        private UIListBox ResultsBox;
        private UIPersonButton TargetIcon;

        private HashSet<uint> Filter;
        private HashSet<uint> Roommates;
        private bool OutgoingMode = true;
        private ImmutableList<Relationship> Rels;
        private Func<Relationship, int> OrderFunction;
        private string LastVal = "";
        private string LastName = "";

        public UIRelationshipDialog()
            : base(UIDialogStyle.Standard | UIDialogStyle.Close, true)
        {
            this.Caption = GameFacade.Strings.GetString("f106", "10");
            //f_web_inbtn = 0x1972454856DDBAC,
            //f_web_outbtn = 0x3D3AEF0856DDBAC,

            InnerBackground = new UIImage(GetTexture((ulong)0x7A400000001)).With9Slice(13, 13, 13, 13);
            InnerBackground.Position = new Vector2(15, 65);
            InnerBackground.SetSize(510, 230);
            AddAt(3, InnerBackground);

            ResultsBox = new UIListBox();
            ResultsBox.Columns = new UIListBoxColumnCollection();
            for (int i = 0; i < 3; i++) ResultsBox.Columns.Add(new UIListBoxColumn() { Width = 170 });
            ResultsBox.Position = new Vector2(25, 82);
            ResultsBox.SetSize(510, 230);
            ResultsBox.RowHeight = 40;
            ResultsBox.NumVisibleRows = 6;
            ResultsBox.SelectionFillColor = Color.TransparentBlack;
            Add(ResultsBox);

            var seat = new UIImage(GetTexture(0x19700000002));
            seat.Position = new Vector2(28, 28);
            Add(seat);

            IncomingButton = new UIButton(GetTexture((ulong)0x1972454856DDBAC));
            IncomingButton.Position = new Vector2(33, 33);
            IncomingButton.Tooltip = GameFacade.Strings.GetString("f106", "12");
            Add(IncomingButton);
            OutgoingButton = new UIButton(GetTexture((ulong)0x3D3AEF0856DDBAC));
            OutgoingButton.Position = new Vector2(33, 33);
            OutgoingButton.Tooltip = GameFacade.Strings.GetString("f106", "13");
            Add(OutgoingButton);

            SearchBox = new UITextBox();
            SearchBox.Position = new Vector2(550 - 170, 37);
            SearchBox.SetSize(150, 25);
            SearchBox.OnEnterPress += SearchBox_OnEnterPress;
            Add(SearchBox);

            SortLabel = new UILabel();
            SortLabel.Caption = GameFacade.Strings.GetString("f106", "1");
            SortLabel.Position = new Vector2(95, 30);
            SortLabel.CaptionStyle = SortLabel.CaptionStyle.Clone();
            SortLabel.CaptionStyle.Size = 8;
            Add(SortLabel);

            SearchLabel = new UILabel();
            SearchLabel.Caption = GameFacade.Strings.GetString("f106", "14");
            SearchLabel.Alignment = Framework.TextAlignment.Right;
            SearchLabel.Position = new Vector2(550 - 230, 38);
            SearchLabel.Size = new Vector2(50, 1);
            Add(SearchLabel);

            SortFriendButton = new UIButton(GetTexture((ulong)0xCE300000001)); //gizmo_friendliestthumb = 0xCE300000001,
            SortFriendButton.Tooltip = GameFacade.Strings.GetString("f106", "2");
            SortFriendButton.Position = new Vector2(95, 47);
            Add(SortFriendButton);

            SortEnemyButton = new UIButton(GetTexture((ulong)0xCE600000001)); //gizmo_meanestthumb = 0xCE600000001,
            SortEnemyButton.Tooltip = GameFacade.Strings.GetString("f106", "3");
            SortEnemyButton.Position = new Vector2(115, 47) + (new Vector2(17 / 2f, 14) - new Vector2(SortEnemyButton.Texture.Width / 8, SortEnemyButton.Texture.Height));
            Add(SortEnemyButton);

            SortAlmostFriendButton = new UIButton(GetTexture((ulong)0x31600000001)); //gizmo_top100defaultthumb = 0x31600000001,
            SortAlmostFriendButton.Tooltip = GameFacade.Strings.GetString("f106", "4");
            SortAlmostFriendButton.Position = new Vector2(135, 47)
                + (new Vector2(17 / 2f, 14) - new Vector2(SortAlmostFriendButton.Texture.Width / 8, SortAlmostFriendButton.Texture.Height));
            Add(SortAlmostFriendButton);

            SortAlmostEnemyButton = new UIButton(GetTexture((ulong)0xCE400000001)); //gizmo_infamousthumb = 0xCE400000001,
            SortAlmostEnemyButton.Tooltip = GameFacade.Strings.GetString("f106", "5");
            SortAlmostEnemyButton.Position = new Vector2(155, 47)
                + (new Vector2(17 / 2f, 14) - new Vector2(SortAlmostEnemyButton.Texture.Width / 8, SortAlmostEnemyButton.Texture.Height));
            Add(SortAlmostEnemyButton);

            SortRoommateButton = new UIButton(GetTexture((ulong)0x4B700000001)); //ucp far zoom
            SortRoommateButton.Tooltip = GameFacade.Strings.GetString("f106", "6");
            SortRoommateButton.Position = new Vector2(175, 47)
                + (new Vector2(17 / 2f, 14) - new Vector2(SortRoommateButton.Texture.Width / 8, SortRoommateButton.Texture.Height));
            Add(SortRoommateButton);

            //gizmo_scrollbarimg = 0x31000000001,
            //gizmo_scrolldownbtn = 0x31100000001,
            //gizmo_scrollupbtn = 0x31200000001,

            ResultsSlider = new UISlider();
            ResultsSlider.Orientation = 1;
            ResultsSlider.Texture = GetTexture(0x31000000001);
            ResultsSlider.MinValue = 0;
            ResultsSlider.MaxValue = 2;

            ResultsSlider.X = 529;
            ResultsSlider.Y = 72;
            ResultsSlider.SetSize(0, 214f);
            Add(ResultsSlider);

            SliderUpButton = new UIButton(GetTexture(0x31200000001));
            SliderUpButton.Position = new Vector2(526, 65);
            Add(SliderUpButton);
            SliderDownButton = new UIButton(GetTexture(0x31100000001));
            SliderDownButton.Position = new Vector2(526, 287);
            Add(SliderDownButton);

            ResultsSlider.AttachButtons(SliderUpButton, SliderDownButton, 1f);
            ResultsBox.AttachSlider(ResultsSlider);

            SetSize(560, 320);

            SortFriendButton.OnButtonClick += (btn) => ChangeOrderFunc(OrderFriendly);
            SortEnemyButton.OnButtonClick += (btn) => ChangeOrderFunc(OrderEnemy);
            SortAlmostFriendButton.OnButtonClick += (btn) => ChangeOrderFunc(OrderAlmostFriendly);
            SortAlmostEnemyButton.OnButtonClick += (btn) => ChangeOrderFunc(OrderAlmostEnemy);
            SortRoommateButton.OnButtonClick += (btn) => ChangeOrderFunc(OrderRoommate);

            ChangeOrderFunc(OrderFriendly);

            IncomingButton.OnButtonClick += (btn) => SetOutgoing(false);
            OutgoingButton.OnButtonClick += (btn) => SetOutgoing(true);

            TargetIcon = new UIPersonButton();
            TargetIcon.FrameSize = UIPersonButtonSize.SMALL;
            TargetIcon.Position = new Vector2(72, 35);
            Add(TargetIcon);

            CloseButton.OnButtonClick += CloseButton_OnButtonClick;

            FriendLabel = new UILabel();
            FriendLabel.Position = new Vector2(35, 292);
            Add(FriendLabel);

            IncomingLabel = new UILabel();
            IncomingLabel.Position = new Vector2(540 - 36, 292);
            IncomingLabel.Size = new Vector2(1, 1);
            IncomingLabel.Alignment = TextAlignment.Right;
            Add(IncomingLabel);

            SetOutgoing(true);
        }

        private void CloseButton_OnButtonClick(UIElement button)
        {
            FindController<RelationshipDialogController>()?.Close();
        }

        private void SearchBox_OnEnterPress(UIElement element)
        {
            if (SearchBox.CurrentText.Length < 2)
            {
                HIT.HITVM.Get().PlaySoundEvent(Model.UISounds.Error);
                return;
            }
            FindController<RelationshipDialogController>().Search(SearchBox.CurrentText);
        }

        public void SetRoommates(IEnumerable<uint> roommates)
        {
            if (roommates == null) Roommates = new HashSet<uint>();
            Roommates = new HashSet<uint>(roommates);
            RedrawRels();
        }

        public void SetPersonID(uint id)
        {
            TargetIcon.Position = new Vector2(68, 37);
            TargetIcon.AvatarId = id;
        }

        private void SetOutgoing(bool mode)
        {
            IncomingButton.Visible = mode;
            OutgoingButton.Visible = !mode;

            IncomingLabel.Caption = GameFacade.Strings.GetString("f106", ((mode)?"9":"8"));
            OutgoingMode = mode;
            RedrawRels();
        }

        private void ChangeOrderFunc(Func<Relationship, int> order)
        {
            OrderFunction = order;

            SortFriendButton.Selected = order == OrderFriendly;
            SortEnemyButton.Selected = order == OrderEnemy;
            SortAlmostFriendButton.Selected = order == OrderAlmostFriendly;
            SortAlmostEnemyButton.Selected = order == OrderAlmostEnemy;
            SortRoommateButton.Selected = order == OrderRoommate;

            RedrawRels();
        }

        private int OrderFriendly(Relationship rel)
        {
            return -rel.Relationship_LTR;
        }

        private int OrderEnemy(Relationship rel)
        {
            return rel.Relationship_LTR;
        }

        private int OrderAlmostFriendly(Relationship rel)
        {
            return Math.Abs(60-rel.Relationship_LTR);
        }

        private int OrderAlmostEnemy(Relationship rel)
        {
            return Math.Abs((-60) - rel.Relationship_LTR);
        }

        private int OrderRoommate(Relationship rel)
        {
            return -rel.Relationship_LTR;
        }

        public void UpdateRelationships(ImmutableList<Relationship> rels)
        {
            if (rels == null) rels = ImmutableList<Relationship>.Empty;
            Rels = rels;
            FriendLabel.Caption = GameFacade.Strings.GetString("f106", "7", new string[] {
                rels.Count(x => x.Relationship_IsOutgoing && x.Relationship_LTR >= 60).ToString(),
                rels.Count(x => x.Relationship_IsOutgoing && x.Relationship_LTR <= -60).ToString()
            });
            RedrawRels();
        }

        public void RedrawRels()
        {
            if (Rels == null) return;
            IEnumerable<Relationship> query = Rels.Where(x => x.Relationship_IsOutgoing == OutgoingMode).OrderBy(OrderFunction);
            if (OrderFunction == OrderRoommate)
                query = query.Where(x => Roommates?.Contains(x.Relationship_TargetID) == true);
            if (Filter != null) query = query.Where(x => Filter.Contains(x.Relationship_TargetID));

            var oldItems = ResultsBox.Items;
            if (oldItems != null)
            {
                foreach (var row in oldItems)
                {
                    foreach (var column in row.Columns)
                    {
                        ((IDisposable)column)?.Dispose();
                    }
                }
            }

            var rels = query.ToList();
            var items = new List<UIListBoxItem>();

            var core = FindController<CoreGameScreenController>();
            var c = rels.Count;
            for (int i = 0; i < c; i += 3)
            {
                items.Add(new UIListBoxItem(new { },
                    new UIRelationshipElement(rels[i], core),
                    (i + 1 >= c) ? null : (new UIRelationshipElement(rels[i + 1], core)),
                    (i + 2 >= c) ? null : (new UIRelationshipElement(rels[i + 2], core)))
                    );
            }

            ResultsBox.Items = items;
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            if (LastVal != SearchBox.CurrentText)
            {
                LastVal = SearchBox.CurrentText;
                if (Filter != null)
                {
                    Filter = null;
                    RedrawRels();
                }
            }

            if (LastName != TargetIcon.Tooltip)
            {
                LastName = TargetIcon.Tooltip;
                Caption = GameFacade.Strings.GetString("f106", "11", new string[] { LastName });
            }

            Invalidate(); //for now invalidate friendship web every frame, because its listbox is pretty complicated.
            //buttonseat_transparent = 0x19700000002,
        }

        public void SetFilter(HashSet<uint> filter)
        {
            Filter = filter;
            RedrawRels();
        }
    }

    public class UIRelationshipElement : UIContainer, IDisposable
    {
        private UIPersonButton Icon;
        private Relationship Rel;
        private Microsoft.Xna.Framework.Graphics.Texture2D Indicator;

        public UIRelationshipElement(Relationship rel, object controller)
        {
            Rel = rel;
            Controller = controller;
            if (rel.Relationship_LTR >= 60) Indicator = GetTexture(0xCE300000001);
            else if (rel.Relationship_LTR <= -60) Indicator = GetTexture(0xCE600000001);
        }

        public void Init()
        {
            if (Icon != null) return;
            Icon = new UIPersonButton();
            Icon.FrameSize = UIPersonButtonSize.LARGE;
            Icon.AvatarId = Rel.Relationship_TargetID;
            Add(Icon);
        }

        private void DrawRel(UISpriteBatch batch, int x, int y, int value)
        {
            double p = (value + 100) / 200.0;
            Color barcol = new Color((byte)(57 * (1 - p)), (byte)(213 * p + 97 * (1 - p)), (byte)(49 * p + 90 * (1 - p)));
            Color bgcol = new Color((byte)(57 * p + 214 * (1 - p)), (byte)(97 * p), (byte)(90 * p));

            var Filler = TextureGenerator.GetPxWhite(batch.GraphicsDevice);
            batch.Draw(Filler, LocalRect(x+1, y+1, 80, 6), new Color(23,38,55));
            batch.Draw(Filler, LocalRect(x, y, 80, 6), bgcol);
            batch.Draw(Filler, LocalRect(x, y, (int)(80 * p), 6), barcol);
            batch.Draw(Filler, LocalRect(x + (int)(80 * p), y, 1, 6), Color.Black);

            var style = TextStyle.DefaultLabel.Clone();
            style.Size = 7;
            style.Shadow = true;

            DrawLocalString(batch, value.ToString(), new Vector2(x + 84, y - 5), style, new Rectangle(0, 0, 1, 1), TextAlignment.Left);
        }

        public override void Draw(UISpriteBatch batch)
        {
            Init(); //init if we haven't been drawn til now
            base.Draw(batch);

            DrawRel(batch, 40, 18, Rel.Relationship_STR);
            DrawRel(batch, 40, 26, Rel.Relationship_LTR);
            if (Indicator != null) DrawLocalTexture(batch, Indicator, new Rectangle(Indicator.Width / 4, 0, Indicator.Width/4, Indicator.Height), new Vector2(142, 17));

            if (Icon.Tooltip != null)
            {
                var style = TextStyle.DefaultLabel;
                DrawLocalString(batch, style.TruncateToWidth(Icon.Tooltip, 120), new Vector2(40, -2), style);
            }
        }

        public void Dispose()
        {
            if (Icon != null) Remove(Icon);
        }
    }
}
