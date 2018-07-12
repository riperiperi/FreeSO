using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.EODs.Archetypes;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Utils;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public class UINewspaperEOD : UIBasicEOD
    {
        public bool Ready = false;
        public UINewspaperPctBar[] BarGraphs;
        public UINewspaperGraphTab GraphTab;
        public UINewspaperCover FrontTab;

        public UIButton FrontTabBtn;
        public UIButton BarTabBtn;
        public UIButton GraphTabBtn;

        public UIImage TopBg;
        public UISlider TopSlider;
        public UITextEdit TopText;

        public UINewspaperEOD(UIEODController controller) : base(controller, "newspaper", null)
        {
        }

        protected override EODLiveModeOpt GetEODOptions()
        {
            return new EODLiveModeOpt
            {
                Buttons = 0,
                Expandable = true,
                Expanded = false,
                Height = EODHeight.TallTall,
                Length = EODLength.Full,
                Timer = EODTimer.None,
                Tips = EODTextTips.None
            };
        }

        protected override void InitEOD()
        {
            base.InitEOD();
            BinaryHandlers["newspaper_state"] = SetState;
        }

        protected override void InitUI()
        {
            BarGraphs = new UINewspaperPctBar[8];
            for (int i = 0; i < 8; i++)
            {
                BarGraphs[i] = new UINewspaperPctBar(GameFacade.Strings.GetString("f108", (i + 1).ToString()), 0, 0, i == 7);
                BarGraphs[i].Position = new Vector2(80 + 45 * i, 45 + 187);
                Add(BarGraphs[i]);
            }

            GraphTab = new UINewspaperGraphTab();
            GraphTab.Position = new Vector2(0, 187);
            Add(GraphTab);

            FrontTab = new UINewspaperCover();
            FrontTab.Position = new Vector2(0, 187);
            FrontTab.OnShowNewsItem += FrontTab_OnShowNewsItem;
            Add(FrontTab);

            FrontTabBtn = new UIButton(GetTexture((ulong)GameContent.FileIDs.UIFileIDs.eod_dc_newcardbtn));
            FrontTabBtn.Position = new Vector2(33 + 21, 55 + 5 + 187);
            FrontTabBtn.OnButtonClick += (btn) => SetTab(0);
            FrontTabBtn.Tooltip = GameFacade.Strings.GetString("f108", "14");
            Add(FrontTabBtn);

            var ui = Content.Content.Get().CustomUI;
            BarTabBtn = new UIButton(ui.Get("eod_news_bar.png").Get(GameFacade.GraphicsDevice));
            BarTabBtn.Position = new Vector2(33, 55 + 37 + 187);
            BarTabBtn.Tooltip = GameFacade.Strings.GetString("f108", "15");
            BarTabBtn.OnButtonClick += (btn) => SetTab(1);
            Add(BarTabBtn);
            
            GraphTabBtn = new UIButton(ui.Get("eod_news_line.png").Get(GameFacade.GraphicsDevice));
            GraphTabBtn.Position = new Vector2(38, 55 + 71 + 187);
            GraphTabBtn.Tooltip = GameFacade.Strings.GetString("f108", "16");
            GraphTabBtn.OnButtonClick += (btn) => SetTab(2);
            Add(GraphTabBtn);

            TopBg = new UIImage(GetTexture((ulong)GameContent.FileIDs.UIFileIDs.eod_signs_readback));
            TopBg.Position = new Vector2(20, 93);
            Add(TopBg);

            TopSlider = new UISlider();
            TopSlider.Texture = GetTexture((ulong)GameContent.FileIDs.UIFileIDs.eod_signs_slider);
            TopSlider.Orientation = 1;
            TopSlider.SetSize(TopSlider.Texture.Width, 105);
            TopSlider.Position = new Vector2(425, 103);
            Add(TopSlider);

            TopText = new UITextEdit();
            TopText.SetSize(366, 107-12);
            TopText.TextMargin = new Rectangle(12, 12, 12, 12);
            TopText.Position = new Vector2(53, 95);
            TopText.TextStyle = TopText.TextStyle.Clone();
            TopText.TextStyle.LineHeightModifier = -3;
            TopText.CurrentText = GameFacade.Strings.GetString("f108", "19");
            Add(TopText);

            TopText.AttachSlider(TopSlider);

            SetTab(0);
            OnContract();
        }

        private void FrontTab_OnShowNewsItem(VMEODFNewspaperNews news)
        {
            var start = new DateTime(news.StartDate);
            var end = new DateTime(news.EndDate);
            string dateString;
            if ((end - start).TotalDays < 1) dateString = "(" + start.ToShortDateString() + ")";
            else dateString = "(" + start.ToShortDateString() + " - " + end.ToShortDateString() + ")";
            TopText.BBCodeEnabled = true;
            TopText.CurrentText = GameFacade.Emojis.EmojiToBB(news.Name + " " + dateString + "\r\n" + news.Description);

            //force the eod expanded.
            var opt = GetEODOptions();
            opt.Expanded = true;
            Controller.ShowEODMode(opt);
            OnExpand();
        }

        public override void OnExpand()
        {
            TopSlider.Visible = true;
            TopBg.Visible = true;
            TopText.Visible = true;
        }

        public override void OnContract()
        {
            TopSlider.Visible = false;
            TopBg.Visible = false;
            TopText.Visible = false;
        }

        private void SetTab(int tab)
        {
            foreach (var bar in BarGraphs)
            {
                bar.Visible = tab == 1;
                if (bar.Visible) bar.AnimateTransition();
            }
            FrontTab.Visible = tab == 0;
            GraphTab.Visible = tab == 2;

            FrontTabBtn.Selected = tab == 0;
            BarTabBtn.Selected = tab == 1;
            GraphTabBtn.Selected = tab == 2;
        }

        private void SetState(string evt, byte[] body)
        {
            var data = new VMEODFNewspaperData(body);

            int bonusSkill = -1;
            for (int i = 0; i < 8; i++)
            {
                var skill = data.Points.Where(x => x.Skilltype == i).ToList();
                var now = skill.LastOrDefault() ?? new VMEODFNewspaperPoint() { Skilltype = i, Multiplier = 1 };
                if (i == 0) GraphTab.SetDate(now.Day);
                var last = (skill.Count > 1) ? skill[skill.Count - 2] : now;
                BarGraphs[i].Percent = now.Multiplier;
                BarGraphs[i].Bonus = (now.Flags & 1) > 0;
                if (BarGraphs[i].Bonus) bonusSkill = i;
                BarGraphs[i].Difference = now.Multiplier - last.Multiplier;
            }

            GraphTab.Graph.Populate(data.Points);
            FrontTab.Populate(data.News, bonusSkill);
        }
    }

    public class UINewspaperPctBar : UIContainer
    {
        public string Name;
        public float Difference;
        public float Percent;
        public bool Bonus;
        private Texture2D WhitePx;

        private UILabel PctLabel;
        private UILabel DiffLabel;
        private UILabel NameLabel;
        private bool Rightmost;

        public float BarHeight
        {
            get { return DiffLabel.Size.Y; }
            set {
                var barHeight = value;
                DiffLabel.Size = new Vector2(30, barHeight);
                DiffLabel.Position = new Vector2(8, 93 - barHeight);
            }
        }

        public float AnimPct
        {
            get; set;
        }

        public UINewspaperPctBar(string name, float diff, float pct, bool rightmost)
        {
            Name = name;
            Difference = diff;
            Percent = pct;
            Rightmost = rightmost;

            //45x110

            PctLabel = new UILabel() { Alignment = TextAlignment.Center | TextAlignment.Middle, Size = new Vector2(45, 18) };
            NameLabel = new UILabel() { Alignment = TextAlignment.Center | TextAlignment.Middle, Size = new Vector2(45, 18), Position = new Vector2(0, 93) };

            int barHeight = (int)Math.Round(15 + 60 * (Percent - 0.5f));
            DiffLabel = new UILabel() { Alignment = TextAlignment.Center | TextAlignment.Middle, Size = new Vector2(30, barHeight), Position = new Vector2(8, 93 - barHeight) };
            WhitePx = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);

            NameLabel.CaptionStyle = NameLabel.CaptionStyle.Clone();
            NameLabel.CaptionStyle.Shadow = true;
            NameLabel.Caption = Name;
            PctLabel.CaptionStyle = NameLabel.CaptionStyle;

            DiffLabel.CaptionStyle = DiffLabel.CaptionStyle.Clone();
            DiffLabel.CaptionStyle.Color = Color.Black;

            UpdateLabels();

            Add(PctLabel);
            Add(NameLabel);
            Add(DiffLabel);

            BarHeight = BarHeight;
            AnimPct = AnimPct;
        }

        public void UpdateLabels()
        {
            PctLabel.Caption = Math.Round(Percent * 100).ToString() + "%";
            DiffLabel.Caption = ((Difference>=0)?"+":"")+Math.Round(Difference * 100).ToString();
        }

        public void AnimateTransition()
        {
            UpdateLabels();
            int barHeight = (int)Math.Round(15 + 60 * (Percent - 0.5f));
            GameFacade.Screens.Tween.To(this, 0.5f, 
                new Dictionary<string, float>() { { "BarHeight", (float)barHeight }, { "AnimPct", Percent } }, 
                TweenQuad.EaseInOut);
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            DrawLocalTexture(batch, WhitePx, null, Vector2.Zero, new Vector2(1, 110), Color.Black * 0.15f);
            if (Rightmost) DrawLocalTexture(batch, WhitePx, null, new Vector2(45, 0), new Vector2(1, 110), Color.Black * 0.15f);

            DrawLocalTexture(batch, WhitePx, null, DiffLabel.Position + Vector2.One, DiffLabel.Size, Color.Black);

            Color color = new Color(255, 249, 157);
            if (Bonus)
                color = Color.White;
            else if (AnimPct > 1)
                color = Color.Lerp(color, new Color(0, 255, 0), (AnimPct - 1)*2);
            else
                color = Color.Lerp(color, new Color(255, 0, 0), (1- AnimPct) * 2);


            DrawLocalTexture(batch, WhitePx, null, DiffLabel.Position, DiffLabel.Size, color);

            base.Draw(batch);
        }
    }

    public class UINewspaperGraphTab : UIContainer
    {
        public static Color[] SkillTypeCol = new Color[]
        {
            new Color(255, 115, 115), //Writer
            new Color(255, 185, 115), //Easel
            new Color(248, 242, 154), //Boards
            new Color(166, 255, 77), //Jams
            new Color(77, 255, 166), //Potion
            new Color(77, 166, 255), //Gnome
            new Color(204, 153, 255), //Pinata
            new Color(201, 38, 255) //Phome (telemarket)
        };

        private UILabel[] DateLabels;
        private UIClickableLabel[] SkillLabels;
        public UINewspaperGraph Graph;

        public UINewspaperGraphTab()
        {
            DateLabels = new UILabel[7];
            for (int i=0; i<7; i++)
            {
                var label = new UILabel();
                label.Size = new Vector2(31, 13);
                label.Alignment = TextAlignment.Center | TextAlignment.Middle;
                label.Position = new Vector2(125 + i*45, 55 - 16);
                label.CaptionStyle = label.CaptionStyle.Clone();
                label.CaptionStyle.Size = 9;
                label.CaptionStyle.Shadow = true;
                Add(label);

                DateLabels[i] = label;
            }

            SkillLabels = new UIClickableLabel[8];

            for (int i = 0; i < 8; i++)
            {
                var label = new UIClickableLabel();
                var id = i;
                label.Position = new Vector2(125 - 44, 55 - 13 + i*15);
                label.CaptionStyle = label.CaptionStyle.Clone();
                label.CaptionStyle.Color = SkillTypeCol[i];
                label.CaptionStyle.Size = 9;
                label.CaptionStyle.Shadow = true;
                label.OnMouseEvtExt += (type, state) => OnSkillHover(type, state, id);
                label.Size = new Vector2(45, 15);
                label.Alignment = TextAlignment.Left | TextAlignment.Top;
                label.Caption = GameFacade.Strings.GetString("f108", (i + 1).ToString());
                Add(label);

                SkillLabels[i] = label;
            }

            Graph = new UINewspaperGraph();
            Graph.Position = new Vector2(80 + 45, 55);
            Add(Graph);
        }

        private void OnSkillHover(UIMouseEventType type, Common.Rendering.Framework.Model.UpdateState state, int id)
        {
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    Graph.SetHover(id);
                    foreach (var skl in SkillLabels) skl.Opacity = 0.5f;
                    SkillLabels[id].Opacity = 1f;
                    break;
                case UIMouseEventType.MouseOut:
                    Graph.SetHover(-1);
                    foreach (var skl in SkillLabels) skl.Opacity = 1f;
                    break;
            }
        }

        public void SetDate(int day)
        {
            var time = new DateTime(1970, 1, 1) + new TimeSpan(day, 0, 0, 0);
            
            for (int i=6; i>=0; i--)
            {
                var formats = CultureInfo.CurrentCulture.DateTimeFormat;
                var pattern = formats.ShortDatePattern.Replace("y", "").Trim(formats.DateSeparator.FirstOrDefault());
                DateLabels[i].Caption = time.ToString(pattern);
                time -= new TimeSpan(1, 0, 0, 0);
            }
        }
    }

    public class UINewspaperGraph : UICachedContainer
    {
        private Texture2D WhitePx;
        public float[][] Data;
        public int SkillHover = -1;

        private float _FadePct;
        public float FadePct
        {
            get
            {
                return _FadePct;
            }
            set
            {
                _FadePct = value;
                Invalidate();
            }
        }

        public UINewspaperGraph() : base()
        {
            WhitePx = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
            UseMip = true;
            Size = new Vector2(299, 105);
        }

        private void DrawLine(Color tint, Vector2 Start, Vector2 End, SpriteBatch spriteBatch, int lineWidth) //draws a line from Start to End.
        {
            Start = LocalPoint(Start);
            End = LocalPoint(End);
            double length = Math.Sqrt(Math.Pow(End.X - Start.X, 2) + Math.Pow(End.Y - Start.Y, 2));
            float direction = (float)Math.Atan2(End.Y - Start.Y, End.X - Start.X);
            spriteBatch.Draw(WhitePx, new Rectangle((int)Start.X, (int)Start.Y - (int)(lineWidth / 2), (int)length, lineWidth),
                null, tint, direction, new Vector2(0, 0.5f), SpriteEffects.None, 0);
        }

        private void DrawAALine(Color tint, Vector2 Start, Vector2 End, SpriteBatch spriteBatch, int lineWidth)
        {
            DrawLine(tint * 0.5f, Start, End, spriteBatch, lineWidth + 1);
            DrawLine(tint, Start, End, spriteBatch, lineWidth);
        }

        private void DrawShadLine(Color tint, Vector2 Start, Vector2 End, SpriteBatch spriteBatch, int lineWidth)
        {
            DrawAALine(Color.Black, Start+Vector2.One, End+Vector2.One, spriteBatch, lineWidth);
            DrawAALine(tint, Start, End, spriteBatch, lineWidth);
        }

        public void SetHover(int skill)
        {
            GameFacade.Screens.Tween.To(this, 0.4f,
                new Dictionary<string, float>() { { "FadePct", (skill == -1)?0f:1f } },
                TweenQuad.EaseInOut);

            if (skill != -1)
            {
                SkillHover = skill;
            }
        }

        public void Populate(List<VMEODFNewspaperPoint> points)
        {
            Data = new float[8][];
            for (int i=0; i<8; i++)
            {
                Data[i] = new float[7];
                var skill = points.Where(x => x.Skilltype == i).ToList();
                var now = skill.LastOrDefault() ?? new VMEODFNewspaperPoint() { Skilltype = i, Multiplier = 1 };
                for (int j=6; j>=0; j--)
                {
                    Data[i][j] = now.Multiplier;
                    var next = skill.FirstOrDefault(x => x.Day == now.Day - 1);
                    if (next == null) now.Day--; //repeat this over the past day, which is missing a data point.
                    else now = next;
                }
            }
        }

        public override void InternalDraw(UISpriteBatch batch)
        {
            //draw bg
            DrawLocalTexture(batch, WhitePx, null, new Vector2(0, 3), Size, Color.Black * 0.75f);
            DrawLocalTexture(batch, WhitePx, null, new Vector2(0, 3), new Vector2(Size.X, 1), Color.Black);
            DrawLocalTexture(batch, WhitePx, null, new Vector2(0, 3), new Vector2(1, Size.Y), Color.Black);
            DrawLocalTexture(batch, WhitePx, null, new Vector2(0, Size.Y - 1), new Vector2(Size.X, 1), Color.Black);
            DrawLocalTexture(batch, WhitePx, null, new Vector2(Size.X - 1, 0), new Vector2(1, Size.Y), Color.Black);

            DrawLine(Color.Black, new Vector2(0, 15), new Vector2(Size.X, 15), batch, 1);
            DrawLine(Color.Black, new Vector2(0, Size.Y - 12), new Vector2(Size.X, Size.Y - 12), batch, 1);

            for (int i = 0; i < 7; i++)
            {
                DrawLine(Color.Black * 0.66f, new Vector2(15 + i * 45, 3), new Vector2(15 + i * 45, Size.Y), batch, 2);
            }

            if (Data != null)
            {
                var range = Size.Y - (12 + 15);
                for (int i=0; i<Data.Length; i++)
                {
                    var color = UINewspaperGraphTab.SkillTypeCol[i];

                    if (SkillHover == i) continue;
                    color *= (1 - FadePct) * 0.8f + 0.2f;
                    for (int j=0; j<Data[i].Length-1; j++)
                    {
                        var point1 = (Data[i][j] - 0.5f) * range;
                        var point2 = (Data[i][j+1] - 0.5f) * range;
                        DrawShadLine(color, new Vector2(15 + j * 45, (Size.Y - 12) - point1), new Vector2(15 + (j + 1) * 45, (Size.Y - 12) - point2), batch, 2);
                    }
                }

                if (SkillHover != -1)
                {
                    var i = SkillHover;
                    var color = UINewspaperGraphTab.SkillTypeCol[i];
                    for (int j = 0; j < Data[i].Length - 1; j++)
                    {
                        var point1 = (Data[i][j] - 0.5f) * range;
                        var point2 = (Data[i][j + 1] - 0.5f) * range;
                        DrawShadLine(color, new Vector2(15 + j * 45, (Size.Y - 12) - point1), new Vector2(15 + (j + 1) * 45, (Size.Y - 12) - point2), batch, 2);
                    }
                }
            }

            base.InternalDraw(batch);

        }
    }

    public class UINewspaperCover : UIContainer
    {
        public UILabel TitleLabel;
        public UILabel PayoutLabel;
        public UILabel LatestLabel;
        public UILabel RecentLabel;

        public UIButton NextButton;
        public UIButton PrevButton;

        public UINewspaperItemButton LatestButton;
        public UINewspaperItemButton[] Recents;
        public UINewspaperItemButton[] AllButtons;
        public event Action<VMEODFNewspaperNews> OnShowNewsItem;

        public UINewspaperCover()
        {
            AllButtons = new UINewspaperItemButton[7];
            LatestButton = new UINewspaperItemButton();
            LatestButton.SetSize(180, 58);
            LatestButton.SetText(new VMEODFNewspaperNews());
            LatestButton.BaseButton.Disabled = true;
            LatestButton.Position = new Vector2(84, 99);
            LatestButton.OnClicked += (btn2) => { OnShowNewsItem(LatestButton.News); };
            Add(LatestButton);
            AllButtons[0] = LatestButton;

            Recents = new UINewspaperItemButton[6];
            for (int i = 0; i < 6; i++)
            {
                var btn = new UINewspaperItemButton();
                btn.SetText(new VMEODFNewspaperNews());
                btn.Position = new Vector2(((i % 4) >= 2) ? 84 : 271, +(i % 2) * 50 + 59);
                btn.OnClicked += (btn2) => { OnShowNewsItem(btn.News); };
                btn.BaseButton.Disabled = true;
                Recents[i] = btn;
                AllButtons[i + 1] = btn;
                Add(btn);
            }

            TitleLabel = new UILabel();
            TitleLabel.CaptionStyle = TitleLabel.CaptionStyle.Clone();
            TitleLabel.CaptionStyle.Size = 15;
            TitleLabel.CaptionStyle.Color = Color.White;
            TitleLabel.CaptionStyle.Shadow = true;
            TitleLabel.Size = new Vector2(180, 23);
            TitleLabel.Position = new Vector2(84, 40);
            TitleLabel.Caption = GameFacade.Strings.GetString("f108", "9", new string[] {
                GameFacade.CurrentCityName.Contains('/')?"Sandbox":GameFacade.CurrentCityName
            });
            Add(TitleLabel);

            PayoutLabel = new UILabel();
            PayoutLabel.CaptionStyle = PayoutLabel.CaptionStyle.Clone();
            PayoutLabel.CaptionStyle.Size = 9;
            PayoutLabel.CaptionStyle.Shadow = true;
            PayoutLabel.Size = new Vector2(180, 23);
            PayoutLabel.Position = new Vector2(84, 63);
            PayoutLabel.Caption = GameFacade.Strings.GetString("f108", "20");
            Add(PayoutLabel);

            LatestLabel = new UILabel();
            LatestLabel.CaptionStyle = PayoutLabel.CaptionStyle.Clone();
            LatestLabel.CaptionStyle.Shadow = true;
            LatestLabel.Size = new Vector2(180, 23);
            LatestLabel.Position = new Vector2(84, 81);
            LatestLabel.Caption = GameFacade.Strings.GetString("f108", "12");
            Add(LatestLabel);

            RecentLabel = new UILabel();
            RecentLabel.CaptionStyle = PayoutLabel.CaptionStyle;
            RecentLabel.Size = new Vector2(180, 23);
            RecentLabel.Position = new Vector2(271, 40);
            RecentLabel.Caption = GameFacade.Strings.GetString("f108", "13");
            Add(RecentLabel);

            NextButton = new UIButton(GetTexture((ulong)GameContent.FileIDs.UIFileIDs.buypanel_scrollrightbtn));
            NextButton.Position = new Vector2(271 + 185, 59 + 24);
            NextButton.OnButtonClick += (btn) => SetPage(1);
            NextButton.Tooltip = GameFacade.Strings.GetString("f108", "17");
            Add(NextButton);

            PrevButton = new UIButton(GetTexture((ulong)GameContent.FileIDs.UIFileIDs.buypanel_scrollleftbtn));
            PrevButton.Position = new Vector2(84 - 14, 59 + 24);
            PrevButton.OnButtonClick += (btn) => SetPage(0);
            PrevButton.Tooltip = GameFacade.Strings.GetString("f108", "18");
            Add(PrevButton);

            SetPage(0);
        }

        public void Populate(List<VMEODFNewspaperNews> news, int bonusSkill)
        {
            for (int i = 0; i < 7; i++)
            {
                if (i < news.Count)
                {
                    var n = news[i];
                    AllButtons[i].SetText(n);
                    AllButtons[i].BaseButton.Disabled = false;
                }
                else
                {
                    AllButtons[i].SetText(new VMEODFNewspaperNews());
                    AllButtons[i].BaseButton.Disabled = true;
                }
            }

            if (bonusSkill > -1)
            {
                PayoutLabel.Caption = GameFacade.Strings.GetString("f108", "10", new string[] { GameFacade.Strings.GetString("f108", (bonusSkill + 1).ToString()) });
            } else
            {
                PayoutLabel.Caption = GameFacade.Strings.GetString("f108", "11");
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            base.Draw(batch);

            if (TitleLabel.Visible)
            {
                var white = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
                DrawLocalTexture(batch, white, null, new Vector2(95, 65), new Vector2(163, 1), Color.Black);
                DrawLocalTexture(batch, white, null, new Vector2(94, 64), new Vector2(163, 1));
            }
        }

        public void SetPage(int page)
        {
            LatestButton.Visible = page == 0;
            TitleLabel.Visible = page == 0;
            PayoutLabel.Visible = page == 0;
            LatestLabel.Visible = page == 0;
            RecentLabel.X = (page == 0) ? 271 : 177;

            for (int i=0; i<Recents.Length; i++)
            {
                Recents[i].Visible = ((i + 2) / 4 == page);
            }

            NextButton.Visible = page == 0;
            PrevButton.Visible = page == 1;
        }
    }

    public class UINewspaperItemButton : UIContainer
    {
        public VMEODFNewspaperNews News;

        public UINineSliceButton BaseButton;
        public UILabel Title;
        public UITextEdit Body;
        public event ButtonClickDelegate OnClicked;

        public UINewspaperItemButton()
        {
            BaseButton = new UINineSliceButton();
            BaseButton.SetNineSlice(15, 15, 15, 15);
            BaseButton.Width = 180;
            BaseButton.Height = 48;
            BaseButton.OnButtonClick += (btn) => OnClicked?.Invoke(btn);
            Add(BaseButton);

            Title = new UILabel();
            Title.CaptionStyle = Title.CaptionStyle.Clone();
            Title.CaptionStyle.Size = 12;
            Title.CaptionStyle.Color = Color.White;
            Title.Alignment = TextAlignment.Left | TextAlignment.Top;
            Title.Position = new Vector2(7, -1);
            Add(Title);

            Body = new UITextEdit();
            Body.TextStyle = Body.TextStyle.Clone();
            Body.TextStyle.Size = 9;
            Body.TextStyle.LineHeightModifier = -5;
            Body.SetSize(167, 32);
            Body.Mode = UITextEditMode.ReadOnly;
            Body.Position = new Vector2(7, 15);
            Body.RemoveMouseEvent();
            Add(Body);
        }

        public void SetText(VMEODFNewspaperNews n)
        {
            News = n;
            Title.Caption = n.Name;
            Body.BBCodeEnabled = true;
            Body.CurrentText = GameFacade.Emojis.EmojiToBB(n.Description);
        }

        public void SetSize(int width, int height)
        {
            BaseButton.Width = width;
            BaseButton.Height = height;
            Body.SetSize(167, height - 16);
        }
    }
}
