using FSO.Client.Controllers.Panels;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Files.Formats.tsodata;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIBulletinBoard : UICachedContainer
    {
        public UILabel MayorPostsLabel;
        public UILabel MayorPostsSubtitle;
        public UILabel MayorPostsPage;

        public UILabel SystemPostsLabel;
        public UILabel SystemPostsSubtitle;
        public UILabel SystemPostsPage;

        public UILabel CommunityPostsLabel;
        public UILabel CommunityPostsSubtitle;
        public UILabel CommunityPostsPage;

        public Action<BulletinItem> OnSelection;
        public bool AcceptSelections = true;

        private UITweenInstance MayorTween;
        private UITweenInstance SystemTween;
        private UITweenInstance CommunityTween;

        private List<BulletinItem> MayorPosts = new List<BulletinItem>();
        private List<BulletinItem> SystemPosts = new List<BulletinItem>();
        private List<BulletinItem> CommunityPosts = new List<BulletinItem>();

        public int MayorPage;
        public int SystemPage;
        public int CommunityPage;

        public UILargeBulletinSummary LastMayorPost;
        public UIMediumBulletinSummary[] MayorPostsBuffer = new UIMediumBulletinSummary[8];

        public UISmallBulletinSummary[] SystemPostsBuffer = new UISmallBulletinSummary[8];

        public UIMediumBulletinSummary LastCommunityPost;
        public UISmallBulletinSummary[] CommunityPostsBuffer = new UISmallBulletinSummary[16];

        private int CenterAlignedBase = (600 - 480) / 2;
        private int LargeLeftBase = 24;
        private int Separation = 122;
        private int VSeparation = 60;

        private int LargePostOff = -30;
        private int MediumPostOff = -19;
        private int SmallPostOff = -15;

        private int MayorLargeTop = 42;
        private int MayorMedTop = 111;
        private int SystemTop = 296 - 10;
        private int CommunityTop = 410;

        private UIButton ScrollMayorLeft; //hidden on page 1
        private UIButton ScrollMayorRight; //offset up on page 1

        private UIButton ScrollSystemLeft;
        private UIButton ScrollSystemRight;

        private UIButton ScrollCommunityLeft;
        private UIButton ScrollCommunityRight;

        private UIButton PostButton;

        private UIMediumBulletinSummary NoPostMayor;
        private UISmallBulletinSummary NoPostSystem;
        private UIMediumBulletinSummary NoPostCommunity;

        private UIMediumBulletinSummary[] NoPostLabels;

        private float OpacityFromOff(float off)
        {
            return Math.Max(0f, Math.Min((off > Separation * 3) ? (Separation * 4 - off) / Separation : 1 + (off / Separation), 1f));
        }

        private float _MayorPostsScroll;
        public float MayorPostsScroll
        {
            get
            {
                return _MayorPostsScroll;
            }
            set
            {
                var floor = (int)_MayorPostsScroll;
                var newFloor = (int)value;
                //newFloor mod 2 == 0: first 4 at left, last 4 at right
                //newFloor mod 2 == 1: last 4 at left, first 4 at right

                if (floor != newFloor)
                {
                    if (newFloor > floor) UpdateDisplay(UIBulletinBoardType.Mayor, newFloor+1, Math.Abs(floor - newFloor) > 1);
                    else UpdateDisplay(UIBulletinBoardType.Mayor, newFloor, Math.Abs(floor - newFloor) > 1);
                }

                var largeOff = 0f;
                if (newFloor == 0)
                {
                    //scroll intensity should factor in the LargeLeftBase multiplier
                    largeOff = (1 - value) * (LargeLeftBase - CenterAlignedBase);
                }

                var end = Separation * 4;
                var leftOff = (value % 1) * Separation * -4;
                var rightOff = leftOff;
                if (newFloor % 2 == 1) leftOff += Separation * 4;
                else rightOff += Separation * 4;

                if (newFloor == 0)
                {
                    var o = OpacityFromOff(leftOff);
                    LastMayorPost.Opacity = o;
                    LastMayorPost.IgnoreMouse = false;
                    if (o != 0) LastMayorPost.Position = new Vector2(leftOff + CenterAlignedBase + largeOff + LargePostOff, MayorLargeTop);
                    MayorPostsBuffer[0].Opacity = 0;
                    leftOff += Separation;
                } else
                {
                    LastMayorPost.IgnoreMouse = true;
                    LastMayorPost.Opacity = 0;
                }

                for (int i = (newFloor == 0) ? 1 : 0; i < 4; i++)
                {
                    var o = OpacityFromOff(leftOff);
                    if (o != 0) MayorPostsBuffer[i].Position = new Vector2(leftOff + CenterAlignedBase - largeOff + MediumPostOff, MayorMedTop);
                    MayorPostsBuffer[i].Opacity = o;
                    leftOff += Separation;
                }

                for (int i = 4; i < 8; i++)
                {
                    var o = OpacityFromOff(rightOff);
                    if (o != 0) MayorPostsBuffer[i].Position = new Vector2(rightOff + CenterAlignedBase - largeOff + MediumPostOff, MayorMedTop);
                    MayorPostsBuffer[i].Opacity = o;
                    rightOff += Separation;
                }

                //page button changes
                var first = Math.Min(1, value);
                if (MayorPosts.Count == 0) first = 1;
                ScrollMayorRight.Y = 56 + first * (150 - 56);
                ScrollMayorLeft.Opacity = first;
                MayorPostsLabel.X = 220 - first * 220;
                var width = MayorPostsLabel.CaptionStyle.MeasureString(MayorPostsLabel.Caption).X;
                MayorPostsLabel.Size = new Vector2(width + (600-width)*first, 0);

                MayorPostsSubtitle.X = 220 * (1-first) + (300 - MayorPostsSubtitle.Size.X/2) * first;
                Invalidate();

                _MayorPostsScroll = value;
            }
        }

        private float _SystemPostsScroll;
        public float SystemPostsScroll
        {
            get
            {
                return _SystemPostsScroll;
            }
            set
            {
                var floor = (int)_SystemPostsScroll;
                var newFloor = (int)value;
                //newFloor mod 2 == 0: first 4 at left, last 4 at right
                //newFloor mod 2 == 1: last 4 at left, first 4 at right

                if (floor != newFloor)
                {
                    if (newFloor > floor) UpdateDisplay(UIBulletinBoardType.System, newFloor + 1, Math.Abs(floor - newFloor) > 1);
                    else UpdateDisplay(UIBulletinBoardType.System, newFloor, Math.Abs(floor - newFloor) > 1);
                }

                var end = Separation * 4;
                var leftOff = (value % 1) * Separation * -4;
                var rightOff = leftOff;
                if (newFloor % 2 == 1) leftOff += Separation * 4;
                else rightOff += Separation * 4;

                for (int i = 0; i < 4; i++)
                {
                    var o = OpacityFromOff(leftOff);
                    if (o != 0) SystemPostsBuffer[i].Position = new Vector2(leftOff + CenterAlignedBase + SmallPostOff, SystemTop);
                    SystemPostsBuffer[i].Opacity = o;
                    leftOff += Separation;
                }

                for (int i = 4; i < 8; i++)
                {
                    var o = OpacityFromOff(rightOff);
                    if (o != 0) SystemPostsBuffer[i].Position = new Vector2(rightOff + CenterAlignedBase + SmallPostOff, SystemTop);
                    SystemPostsBuffer[i].Opacity = o;
                    rightOff += Separation;
                }

                _SystemPostsScroll = value;
            }
        }

        private float _CommunityPostsScroll;
        public float CommunityPostsScroll
        {
            get
            {
                return _CommunityPostsScroll;
            }
            set
            {
                var floor = (int)_CommunityPostsScroll;
                var newFloor = (int)value;
                //newFloor mod 2 == 0: first 4 at left, last 4 at right
                //newFloor mod 2 == 1: last 4 at left, first 4 at right

                if (floor != newFloor)
                {
                    if (newFloor > floor) UpdateDisplay(UIBulletinBoardType.Community, newFloor + 1, Math.Abs(floor - newFloor) > 1);
                    else UpdateDisplay(UIBulletinBoardType.Community, newFloor, Math.Abs(floor - newFloor) > 1);
                }

                var end = Separation * 4;
                var leftOff = (value % 1) * Separation * -4;
                var rightOff = leftOff;
                if (newFloor % 2 == 1) leftOff += Separation * 4;
                else rightOff += Separation * 4;

                if (newFloor == 0)
                {
                    var o = OpacityFromOff(leftOff);
                    if (o != 0) LastCommunityPost.Position = new Vector2(leftOff + CenterAlignedBase + MediumPostOff, CommunityTop);
                    LastCommunityPost.Opacity = o;
                    LastCommunityPost.IgnoreMouse = false;
                    CommunityPostsBuffer[0].Opacity = 0;
                    CommunityPostsBuffer[1].Opacity = 0;
                    leftOff += Separation;
                }
                else
                {
                    LastCommunityPost.IgnoreMouse = true;
                    LastCommunityPost.Opacity = 0;
                }

                for (int i = (newFloor == 0) ? 1 : 0; i < 4; i++)
                {
                    var o = OpacityFromOff(leftOff);
                    if (o != 0) CommunityPostsBuffer[i * 2].Position = new Vector2(leftOff + CenterAlignedBase + SmallPostOff, CommunityTop);
                    CommunityPostsBuffer[i * 2].Opacity = o;

                    if (o != 0) CommunityPostsBuffer[i * 2 + 1].Position = CommunityPostsBuffer[i * 2].Position + new Vector2(0, VSeparation);
                    CommunityPostsBuffer[i * 2 + 1].Opacity = o;
                    leftOff += Separation;
                }

                for (int i = 4; i < 8; i++)
                {
                    var o = OpacityFromOff(rightOff);
                    if (o != 0) CommunityPostsBuffer[i * 2].Position = new Vector2(rightOff + CenterAlignedBase + SmallPostOff, CommunityTop);
                    CommunityPostsBuffer[i * 2].Opacity = o;

                    if (o != 0) CommunityPostsBuffer[i * 2 + 1].Position = CommunityPostsBuffer[i * 2].Position + new Vector2(0, VSeparation);
                    CommunityPostsBuffer[i * 2 + 1].Opacity = o;
                    rightOff += Separation;
                }
                _CommunityPostsScroll = value;
            }
        }

        private void UpdateDisplay(UIBulletinBoardType type, int floor, bool fullUpdate)
        {
            int perPage = (type == UIBulletinBoardType.Community) ? 8 : 4;
            int firstPageSrcMinus = type == UIBulletinBoardType.Community ? 1 : 0; //items lost from the perPage total on the first page.
            int firstPageUIMinus = 0;
            List<BulletinItem> items = null;
            List<UIMediumBulletinSummary> uibuffer = null;
            List<UIMediumBulletinSummary> firstPageBuffer = new List<UIMediumBulletinSummary>();

            switch (type)
            {
                case UIBulletinBoardType.Mayor:
                    NoPostMayor.Visible = MayorPosts.Count == 0;
                    firstPageUIMinus = 1;
                    uibuffer = MayorPostsBuffer.ToList();
                    firstPageBuffer.Add(LastMayorPost);
                    items = MayorPosts;
                    break;
                case UIBulletinBoardType.Community:
                    NoPostCommunity.Visible = CommunityPosts.Count == 0;
                    firstPageUIMinus = 2;
                    uibuffer = CommunityPostsBuffer.Cast<UIMediumBulletinSummary>().ToList();
                    firstPageBuffer.Add(LastCommunityPost);
                    items = CommunityPosts;
                    break;
                case UIBulletinBoardType.System:
                    NoPostSystem.Visible = SystemPosts.Count == 0;
                    uibuffer = SystemPostsBuffer.Cast<UIMediumBulletinSummary>().ToList();
                    items = SystemPosts;
                    break;
            }
            int startSrc = floor * perPage;
            int startDest = (floor * perPage) % (perPage*2);
            int todo = (fullUpdate ? 2 : 1) * perPage;
            if (floor > 0)
            {
                startSrc -= firstPageSrcMinus;
            } else
            {
                for (int i = 0; i < firstPageBuffer.Count; i++)
                {
                    if (startSrc >= items.Count) firstPageBuffer[i].SetItem(null);
                    else firstPageBuffer[i].SetItem(items[startSrc]);
                    startSrc++;
                }
                startDest += firstPageUIMinus;
                todo -= firstPageUIMinus;
            }

            for (int i=0; i<todo; i++)
            {
                if (startSrc >= items.Count) uibuffer[startDest++].SetItem(null);
                else uibuffer[startDest++].SetItem(items[startSrc]);
                startSrc++;
                startDest %= (perPage * 2);
            }
        }

        public UIBulletinBoard()
        {
            var strings = GameFacade.Strings;

            var subtitleStyle = TextStyle.DefaultLabel.Clone();
            subtitleStyle.Size = 9;
            subtitleStyle.Shadow = true;

            var pageStyle = subtitleStyle.Clone();
            pageStyle.Color = Color.White;

            var titleStyle = TextStyle.DefaultLabel.Clone();
            titleStyle.Color = Color.White;
            titleStyle.Size = 22;
            titleStyle.Shadow = true;

            //mayor

            MayorPostsLabel = new UILabel()
            {
                Position = new Vector2(220, 38),
                CaptionStyle = titleStyle,
                Caption = strings.GetString("f120", "2")
            };
            Add(MayorPostsLabel);

            MayorPostsSubtitle = new UILabel()
            {
                Position = new Vector2(220, 72),
                Size = new Vector2(313, 32),
                Alignment = TextAlignment.Left | TextAlignment.Top,
                Wrapped = true,
                CaptionStyle = subtitleStyle,
                Caption = strings.GetString("f120", "3")
            };
            Add(MayorPostsSubtitle);

            MayorPostsPage = new UILabel()
            {
                Position = new Vector2(0, 232),
                Size = new Vector2(600, 1),
                Alignment = TextAlignment.Center | TextAlignment.Top,
                CaptionStyle = pageStyle,
                Caption = strings.GetString("f120", "8", new string[] { "1" })
            };
            Add(MayorPostsPage);

            //system

            SystemPostsLabel = new UILabel()
            {
                Position = new Vector2(20, 253 - 10),
                CaptionStyle = titleStyle,
                Caption = strings.GetString("f120", "4")
            };
            Add(SystemPostsLabel);

            SystemPostsSubtitle = new UILabel()
            {
                Position = new Vector2(0, 269 - 10),
                Size = new Vector2(600 - 20, 1),
                Alignment = TextAlignment.Right | TextAlignment.Top,
                CaptionStyle = subtitleStyle,
                Caption = strings.GetString("f120", "5")
            };
            Add(SystemPostsSubtitle);

            SystemPostsPage = new UILabel()
            {
                Position = new Vector2(0, 342),
                Size = new Vector2(600, 1),
                Alignment = TextAlignment.Center | TextAlignment.Top,
                CaptionStyle = pageStyle,
                Caption = strings.GetString("f120", "8", new string[] { "1" })
            };
            Add(SystemPostsPage);

            //community

            CommunityPostsLabel = new UILabel()
            {
                Position = new Vector2(0, 356),
                Size = new Vector2(600, 1),
                Alignment = TextAlignment.Center | TextAlignment.Top,
                CaptionStyle = titleStyle,
                Caption = strings.GetString("f120", "6")
            };
            Add(CommunityPostsLabel);

            CommunityPostsSubtitle = new UILabel()
            {
                Position = new Vector2(0, 389),
                Size = new Vector2(600, 1),
                Alignment = TextAlignment.Center | TextAlignment.Top,
                CaptionStyle = subtitleStyle,
                Caption = strings.GetString("f120", "7")
            };
            Add(CommunityPostsSubtitle);

            CommunityPostsPage = new UILabel()
            {
                Position = new Vector2(0, 526),
                Size = new Vector2(600, 1),
                Alignment = TextAlignment.Center | TextAlignment.Top,
                CaptionStyle = pageStyle,
                Caption = strings.GetString("f120", "8", new string[] { "1" })
            };
            Add(CommunityPostsPage);

            InitAllSummaries();

            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            var larrow = ui.Get("bulletin_pleft.png").Get(gd);
            var rarrow = ui.Get("bulletin_pright.png").Get(gd);

            DynamicOverlay.Add(ScrollMayorLeft = new UIButton(larrow) { Position = new Vector2(31 - 6, 150), Tooltip = strings.GetString("f120", "10") }); //hidden on page 1
            DynamicOverlay.Add(ScrollMayorRight = new UIButton(rarrow) { Position = new Vector2(555 + 6, 150), Tooltip = strings.GetString("f120", "9") }); //offset up on page 1

            DynamicOverlay.Add(ScrollSystemLeft = new UIButton(larrow) { Position = new Vector2(31 - 6, 306-10), Tooltip = strings.GetString("f120", "10") });
            DynamicOverlay.Add(ScrollSystemRight = new UIButton(rarrow) { Position = new Vector2(555 + 6, 306-10), Tooltip = strings.GetString("f120", "9") });

            DynamicOverlay.Add(ScrollCommunityLeft = new UIButton(larrow) { Position = new Vector2(31 - 6, 448), Tooltip = strings.GetString("f120", "10") });
            DynamicOverlay.Add(ScrollCommunityRight = new UIButton(rarrow) { Position = new Vector2(555 + 6, 448), Tooltip = strings.GetString("f120", "9") });

            ScrollMayorLeft.OnButtonClick += (btn) => Scroll(UIBulletinBoardType.Mayor, -1);
            ScrollMayorRight.OnButtonClick += (btn) => Scroll(UIBulletinBoardType.Mayor, 1);

            ScrollSystemLeft.OnButtonClick += (btn) => Scroll(UIBulletinBoardType.System, -1);
            ScrollSystemRight.OnButtonClick += (btn) => Scroll(UIBulletinBoardType.System, 1);

            ScrollCommunityLeft.OnButtonClick += (btn) => Scroll(UIBulletinBoardType.Community, -1);
            ScrollCommunityRight.OnButtonClick += (btn) => Scroll(UIBulletinBoardType.Community, 1);

            UpdateScrollButtons();
            MayorPostsScroll = MayorPostsScroll;
            SystemPostsScroll = SystemPostsScroll;
            CommunityPostsScroll = CommunityPostsScroll;

            PostButton = new UIButton(ui.Get("vote_big_btn.png").Get(gd));
            PostButton.Width = 150;
            PostButton.Caption = strings.GetString("f120", "12");
            PostButton.CaptionStyle = PostButton.CaptionStyle.Clone();
            PostButton.CaptionStyle.Color = Color.White;
            PostButton.CaptionStyle.Shadow = true;
            PostButton.CaptionStyle.Size = 22;
            PostButton.Position = new Vector2((600 - 150) / 2, 545);
            PostButton.OnButtonClick += PostButton_OnButtonClick;
            Add(PostButton);

            var noPostStyle = TextStyle.DefaultLabel.Clone();
            noPostStyle.Size = 12;
            noPostStyle.Shadow = true;
            noPostStyle.Color = new Color(255, 153, 153);
            
            DynamicOverlay.Add(NoPostMayor = new UIMediumBulletinSummary()
            {
                Position = new Vector2((600 - 150) / 2, MayorMedTop),
            });

            DynamicOverlay.Add(NoPostSystem = new UISmallBulletinSummary(true)
            {
                Position = new Vector2((600 - 150) / 2, SystemTop),
            });

            DynamicOverlay.Add(NoPostCommunity = new UIMediumBulletinSummary(true)
            {
                Position = new Vector2((600 - 150) / 2, CommunityTop),
            });

            NoPostLabels = new UIMediumBulletinSummary[] { NoPostMayor, NoPostSystem, NoPostCommunity };
            var rand = new Random();
            foreach (var label in NoPostLabels) PrepareInactivePost(label, rand);

            Size = new Vector2(600, 610);

            InitBulletinItems(new BulletinItem[]
            {
            });

            //InitTestData();
        }

        private void PrepareInactivePost(UIMediumBulletinSummary summary, Random rand)
        {
            UIUtils.GiveTooltip(summary);
            summary.HSVMod.G = 0;
            summary.HSVMod.B = 255;
            summary.Tooltip = GameFacade.Strings.GetString("f120", "39");
            summary.TitleLabel.CaptionStyle.Color = new Color(51, 51, 51);
            summary.Body.CaptionStyle.Color = new Color(70, 70, 70);
            summary.SetItem(new BulletinItem()
            {
                Subject = GameFacade.Strings.GetString("f122", rand.Next(10).ToString()),
                Body = GameFacade.Strings.GetString("f122", (10 + rand.Next(11)).ToString()),
                Time = 0// ClientEpoch.Now - rand.Next(60 * 60 * 24 * 7)
            });
        }

        private void PostButton_OnButtonClick(UIElement button)
        {
            if (AcceptSelections)
            {
                FindController<BulletinDialogController>().TransitionToPost();
            }
        }

        public void Lock()
        {
            AcceptSelections = false;
        }

        public void Unlock()
        {
            AcceptSelections = true;
        }

        private void SelectPost(UIAbstractStickyContainer sticky)
        {
            if (!AcceptSelections) return;
            var item = (UIMediumBulletinSummary)sticky;
            if (item.Item == null) return; //or mode is edit
            else
            {
                HIT.HITVM.Get().PlaySoundEvent(Model.UISounds.Click);
                HIT.HITVM.Get().PlaySoundEvent(Model.UISounds.Whoosh);
                //cancel tweens repositioning the bulletin items
                MayorTween?.Complete();
                SystemTween?.Complete();
                CommunityTween?.Complete();

                ScrollMayorLeft.Disabled = true;
                ScrollSystemLeft.Disabled = true;
                ScrollCommunityLeft.Disabled = true;

                ScrollMayorRight.Disabled = true;
                ScrollSystemRight.Disabled = true;
                ScrollCommunityRight.Disabled = true;

                GameFacade.Screens.Tween.To(item, 0.66f, new Dictionary<string, float>() {
                    { "X", (Size.X - item.Size.X*3) / 2 },
                    { "Y", (Size.Y - (item.Size.Y*3 - 30)) / 2 },
                    { "ScaleX", 3f }, { "ScaleY", 3f } }, TweenQuad.EaseOut);

                DynamicOverlay.SendToFront(item);

                OnSelection?.Invoke(item.Item);
            }
        }

        public void Fade(float opacity)
        {
            var elems = DynamicOverlay.GetChildren().ToList();
            if (opacity == 1f) {
                MayorPostsScroll = MayorPostsScroll;
                SystemPostsScroll = SystemPostsScroll;
                CommunityPostsScroll = CommunityPostsScroll;
                LastMayorPost.ScaleX = LastMayorPost.ScaleY = 1f;
                foreach (var elem in MayorPostsBuffer)
                {
                    elem.ScaleX = elem.ScaleY = 1f;
                }
                foreach (var elem in SystemPostsBuffer)
                {
                    elem.ScaleX = elem.ScaleY = 1f;
                }
                LastCommunityPost.ScaleX = LastCommunityPost.ScaleY = 1f;
                foreach (var elem in CommunityPostsBuffer)
                {
                    elem.ScaleX = elem.ScaleY = 1f;
                }

                NoPostMayor.Opacity = 1;
                NoPostCommunity.Opacity = 1;
                NoPostSystem.Opacity = 1;

                UpdateScrollButtons();
                elems.RemoveAll(x => x.Opacity == 0 && x is UIMediumBulletinSummary);
                foreach (var elem in elems) elem.Opacity = 0;
            }
            foreach (var child in elems)
            {
                GameFacade.Screens.Tween.To(child, 0.66f, new Dictionary<string, float>() { { "Opacity", opacity } }, (Opacity == 0) ? TweenQuad.EaseIn : TweenQuad.EaseOut);
            }

            GameFacade.Screens.Tween.To(this, 0.66f, new Dictionary<string, float>() { { "Opacity", opacity } }, (Opacity == 0) ? TweenQuad.EaseIn : TweenQuad.EaseOut);
        }

        private void InitTestData()
        {
            var test1 = new BulletinItem() { Subject = "Test Post 1", Body = "you never seen it cooommiiiiiiiinnnnnnnggggggggg g g g g g gggg g g g  g g ggggg", Type = BulletinType.Mayor, Flags = BulletinFlags.PromotedByMayor };
            var test2 = new BulletinItem() { Subject = "Important Mayor Post", Body = "christmas is cancelled", Type = BulletinType.Mayor };
            var test3 = new BulletinItem() { Subject = "Lorem Hippopotamus", Body = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Curabitur eu fringilla ipsum. Sed in odio at dui viverra posuere. Donec hendrerit mollis risus, eu molestie ante gravida ut. Aenean ut molestie leo. Aliquam id elit urna. Suspendisse accumsan ut orci et venenatis. Aliquam dictum leo lorem, nec volutpat sem finibus non. Aenean nec elit eu erat dictum fermentum ut id sem. Donec mollis, tellus nec sagittis bibendum, magna risus cursus augue, id lacinia sapien tortor at enim. Duis maximus massa risus, commodo venenatis lorem dictum in. Aenean sed condimentum justo. Quisque dictum mi eget libero auctor semper. Sed dapibus efficitur neque, a pellentesque ligula aliquet eu.", Type = BulletinType.Mayor };

            var test4 = new BulletinItem() { Subject = "Test Post 2", Body = "you are all banned", Type = BulletinType.System };
            var test5 = new BulletinItem() { Subject = "Important System Post", Body = "christmas is cancelled again", Type = BulletinType.System };
            var test6 = new BulletinItem() { Subject = "Lorem Sassafrass", Body = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Curabitur eu fringilla ipsum. Sed in odio at dui viverra posuere. Donec hendrerit mollis risus, eu molestie ante gravida ut. Aenean ut molestie leo. Aliquam id elit urna. Suspendisse accumsan ut orci et venenatis. Aliquam dictum leo lorem, nec volutpat sem finibus non. Aenean nec elit eu erat dictum fermentum ut id sem. Donec mollis, tellus nec sagittis bibendum, magna risus cursus augue, id lacinia sapien tortor at enim. Duis maximus massa risus, commodo venenatis lorem dictum in. Aenean sed condimentum justo. Quisque dictum mi eget libero auctor semper. Sed dapibus efficitur neque, a pellentesque ligula aliquet eu.", Type = BulletinType.System };

            var test7 = new BulletinItem() { Subject = "Test Post 3", Body = "Hi there! We are reaching out to lot owners who are currently not located (to our knowledge) within neighbourhoods that are planning to be super active come the neighbourhood update. If you don’t know what this update is then you should check out freeso.org for info on mayors and more. Hi there! We are reaching out to lot owners who are currently not located (to our knowledge) within neighbourhoods that are planning to be super active come the neighbourhood update. If you don’t know what this update is then you should check out freeso.org for info on mayors and more. Hi there! We are reaching out to lot owners who are currently not located (to our knowledge) within neighbourhoods that are planning to be super active come the neighbourhood update. If you don’t know what this update is then you should check out freeso.org for info on mayors and more. \n\nYours truly,\nCaleb “steal yo residents” Carrazella",
                Type = BulletinType.Community,
                Time = ClientEpoch.Now,
            };
            var test8 = new BulletinItem() { Subject = "Important Community Post", Body = "i'm pretty bummed out that christmas has been cancelled.", Type = BulletinType.Community };
            var test9 = new BulletinItem() { Subject = "Lorem Community", Body = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Curabitur eu fringilla ipsum. Sed in odio at dui viverra posuere. Donec hendrerit mollis risus, eu molestie ante gravida ut. Aenean ut molestie leo. Aliquam id elit urna. Suspendisse accumsan ut orci et venenatis. Aliquam dictum leo lorem, nec volutpat sem finibus non. Aenean nec elit eu erat dictum fermentum ut id sem. Donec mollis, tellus nec sagittis bibendum, magna risus cursus augue, id lacinia sapien tortor at enim. Duis maximus massa risus, commodo venenatis lorem dictum in. Aenean sed condimentum justo. Quisque dictum mi eget libero auctor semper. Sed dapibus efficitur neque, a pellentesque ligula aliquet eu.", Type = BulletinType.Community };


            InitBulletinItems(new BulletinItem[]
            {
                test1, test2, test3, test1, test2, test3, test1, test2, test3,
                test4, test5, test6, test4, test5, test6, test4,
                test7, test8, test9, test7, test8, test9, test7, test8, test9, test7, test8, test9, test7, test8, test9, test7, test8, test9, test7, test8, test9
            });
        }
        
        public void InitBulletinItems(BulletinItem[] src)
        {
            MayorPosts.Clear();
            CommunityPosts.Clear();
            SystemPosts.Clear();

            foreach (var item in src.OrderBy(x => -x.Time))
            {
                switch (item.Type)
                {
                    case BulletinType.Mayor:
                        MayorPosts.Add(item);
                        break;
                    case BulletinType.System:
                        SystemPosts.Add(item);
                        break;
                    case BulletinType.Community:
                        CommunityPosts.Add(item);
                        break;
                }
            }

            UpdateDisplay(UIBulletinBoardType.Mayor, (int)MayorPostsScroll, true);
            UpdateDisplay(UIBulletinBoardType.Community, (int)CommunityPostsScroll, true);
            UpdateDisplay(UIBulletinBoardType.System, (int)SystemPostsScroll, true);
            UpdateScrollButtons();

            Scroll(UIBulletinBoardType.Mayor, 0);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
        }

        public override void InternalDraw(UISpriteBatch batch)
        {
            base.InternalDraw(batch);
            var whitePx = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);

            /*
            foreach (var item in NoPostLabels)
            {
                if (item.Visible) DrawLocalTexture(batch, whitePx, null, item.Position, item.Size, Color.Black * 0.16f);
            }
            */
        }

        private void Scroll(UIBulletinBoardType type, int dir)
        {
            var strings = GameFacade.Strings;
            switch (type)
            {
                case UIBulletinBoardType.Community:
                    CommunityPage += dir;
                    CommunityTween?.Stop();
                    CommunityTween = GameFacade.Screens.Tween.To(this, 0.5f, new Dictionary<string, float>() { { "CommunityPostsScroll", CommunityPage } }, TweenQuad.EaseOut);
                    CommunityPostsPage.Caption = strings.GetString("f120", "8", new string[] { (CommunityPage + 1).ToString() });
                    break;
                case UIBulletinBoardType.Mayor:
                    MayorPage += dir;
                    MayorTween?.Stop();
                    MayorTween = GameFacade.Screens.Tween.To(this, 0.5f, new Dictionary<string, float>() { { "MayorPostsScroll", MayorPage } }, TweenQuad.EaseOut);
                    MayorPostsPage.Caption = strings.GetString("f120", "8", new string[] { (MayorPage + 1).ToString() });

                    var firstPage = MayorPosts.Count != 0 && (MayorPage <= 0);
                    MayorPostsLabel.Alignment = (firstPage ? TextAlignment.Left : TextAlignment.Center) | TextAlignment.Top;
                    MayorPostsSubtitle.Alignment = (firstPage ? TextAlignment.Left : TextAlignment.Center) | TextAlignment.Top;
                    break;
                case UIBulletinBoardType.System:
                    SystemPage += dir;
                    SystemTween?.Stop();
                    SystemTween = GameFacade.Screens.Tween.To(this, 0.5f, new Dictionary<string, float>() { { "SystemPostsScroll", SystemPage } }, TweenQuad.EaseOut);
                    SystemPostsPage.Caption = strings.GetString("f120", "8", new string[] { (SystemPage + 1).ToString() });
                    break;
            }

            UpdateScrollButtons();
        }

        private void UpdateScrollButtons()
        {
            ScrollMayorLeft.Visible = MayorPage != 0;
            ScrollMayorLeft.Disabled = false;
            ScrollSystemLeft.Visible = SystemPage != 0;
            ScrollSystemLeft.Disabled = SystemPage == 0;
            ScrollCommunityLeft.Visible = CommunityPage != 0;
            ScrollCommunityLeft.Disabled = CommunityPage == 0;

            ScrollMayorRight.Visible = MayorPage != Math.Max(MayorPosts.Count-1, 0)/4;
            ScrollSystemRight.Visible = SystemPage != Math.Max(SystemPosts.Count - 1, 0) / 4;
            ScrollCommunityRight.Visible = CommunityPage != Math.Max(CommunityPosts.Count, 0) / 8;
            ScrollMayorRight.Disabled = false;
            ScrollSystemRight.Disabled = false;
            ScrollCommunityRight.Disabled = false;
        }

        public void InitAllSummaries()
        {
            LastMayorPost = new UILargeBulletinSummary();
            LastMayorPost.OnClick += SelectPost;
            LastMayorPost.Opacity = 0;
            DynamicOverlay.Add(LastMayorPost);

            for (int i = 0; i < MayorPostsBuffer.Length; i++)
            {
                var mayorPost = new UIMediumBulletinSummary();
                mayorPost.Opacity = 0;
                mayorPost.OnClick += SelectPost;
                MayorPostsBuffer[i] = mayorPost;
                DynamicOverlay.Add(mayorPost);
                if (i % 2 == 1) mayorPost.HSVMod = new Color(255-16, 255, 245, 255);
            }

            for (int i = 0; i < SystemPostsBuffer.Length; i++)
            {
                var systemPost = new UISmallBulletinSummary(true);
                systemPost.OnClick += SelectPost;
                systemPost.Opacity = 0;
                SystemPostsBuffer[i] = systemPost;
                DynamicOverlay.Add(systemPost);
                if (i % 2 == 1) systemPost.HSVMod = new Color(20, 255, 240, 255);
            }

            LastCommunityPost = new UIMediumBulletinSummary(true);
            LastCommunityPost.OnClick += SelectPost;
            LastCommunityPost.Opacity = 0;
            DynamicOverlay.Add(LastCommunityPost);

            for (int i = 0; i < CommunityPostsBuffer.Length; i++)
            {
                var communityPost = new UISmallBulletinSummary(false);
                communityPost.OnClick += SelectPost;
                communityPost.Opacity = 0;
                CommunityPostsBuffer[i] = communityPost;
                DynamicOverlay.Add(communityPost);
                if ((i + i/2) % 2 == 1) communityPost.HSVMod = new Color(communityPost.HSVMod.R + 15, 255, 240, 255);
            }
        }
    }

    public enum UIBulletinBoardType {
        Mayor,
        System,
        Community,
    }
}
