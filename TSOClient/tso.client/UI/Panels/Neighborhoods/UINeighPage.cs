using FSO.Client.Controllers;
using FSO.Client.Controllers.Panels;
using FSO.Client.Rendering.City;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.DataService.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UINeighPage : UIContainer
    {
        public UIImage BackgroundImage { get; set; }
        public UIButton HouseNameButton { get; set; }

        public UIImage InfoTabBackgroundImage { get; set; }
        public UIImage InfoTabImage { get; set; }

        public UIImage TopSTab2BackgroundImage { get; set; }
        public UIImage TopSTab3BackgroundImage { get; set; }
        public UIImage TopSTabImage { get; set; }

        public UIImage TopSTabTab1BackgroundImage { get; set; }
        public UIImage TopSTabTab2BackgroundImage { get; set; }
        public UIImage TopSTabTab3BackgroundImage { get; set; }
        public UIImage TopSTabTab4BackgroundImage { get; set; }

        public UIImage TopSTabTab1SeatImage { get; set; }
        public UIImage TopSTabTab2SeatImage { get; set; }
        public UIImage TopSTabTab3SeatImage { get; set; }
        public UIImage TopSTabTab4SeatImage { get; set; }

        public UIImage MayorTabBackgroundImage { get; set; }
        public UIImage MayorTabImage { get; set; }
        public UIImage MayorTabRateImage { get; set; }

        //tab buttons
        public UIButton InfoButton { get; set; }
        public UIButton HouseButton { get; set; }
        public UIButton PersonButton { get; set; }
        public UIButton MayorButton { get; set; }

        public UIButton CloseButton { get; set; }
        public UIButton CenterButton { get; set; }
        public UIButton BulletinButton { get; set; }

        public UIButton TopTab1Button { get; set; }
        public UIButton TopTab2Button { get; set; }
        public UIButton TopTab3Button { get; set; }
        public UIButton TopTab4Button { get; set; }

        //info tab
        public UITextEdit DescriptionText { get; set; }
        public UISlider DescriptionSlider { get; set; }
        public UIButton DescriptionScrollUpButton { get; set; }
        public UIButton DescriptionScrollDownButton { get; set; }

        public UILabel StatusLabel { get; set; }
        public UILabel ActivityRatingLabel { get; set; }
        public UINeighBanner TownNameBanner { get; set; }
        //public UILabel TownHallNameLabel { get; set; }
        public UILabel ResidentCountLabel { get; set; }
        public UILabel PropertyCountLabel { get; set; }

        public UILabel MayorRatingFlairLabel { get; set; }

        public UILotThumbButton LotThumbnail;
        public Texture2D RoommateThumbButtonImage { get; set; }
        public Texture2D VisitorThumbButtonImage { get; set; }
        private Texture2D DefaultThumb;

        //top 10 tabs
        public UILabel TabTypeLabel { get; set; }

        public List<UITop10Pedestal> Pedestals;
        public List<UILabel> Top10Labels;

        //rate tab
        public UILabel MayorElectionLabel { get; set; }
        public UILabel MayorNominationLabel { get; set; }
        public UILabel MayorStatusLabel { get; set; }
        public UIButton RateButton { get; set; }
        public UIBigPersonButton MayorPersonButton { get; set; }
        public UIRatingDisplay RatingStars { get; set; }

        public UINeighBanner MayorBanner { get; set; }
        public UINeighBanner TermBanner { get; set; }

        public UINeighPageTab CurrentTab;
        public UINeighLotsTab CurrentLotTab;
        public UINeighPersonTab CurrentPersonTab;
        public UINeighMayorTabMode CurrentMayorTab;

        public UIRatingSummaryPanel MayorRatingBox1 { get; set; }
        public UIRatingSummaryPanel MayorRatingBox2 { get; set; }

        //mayor and mod actions
        public UIButton MayorActionReturn { get; set; }
        public UIButton MayorActionMoveTH { get; set; }
        public UIButton MayorActionNewTH { get; set; }

        public UIButton MayorActionOrdinances { get; set; }
        public UIButton MayorActionMod { get; set; }

        public UIButton ModActionSetMayor { get; set; }
        public UIButton ModActionManageRatings { get; set; }
        public UIButton ModActionReserved { get; set; }

        public UIButton ModActionTestCycle { get; set; }
        public UIButton ModActionReturn { get; set; }

        //stuff
        public Binding<Neighborhood> CurrentNeigh;
        public Binding<Avatar> CurrentMayor;
        public Binding<Lot> CurrentTownHall;
        private bool HasTownHall;
        private LotThumbEntry ThumbLock;
        private Texture2D LastThumb;
        private bool MayorIsMe;
        public bool HasShownFilters;
        public bool DescriptionChanged;

        private int RatingCycleTime = 0;
        private static int RATING_CYCLE_DURATION = 7; //7 seconds

        private uint _MayorID;
        public uint MayorID
        {
            get { return _MayorID; }
            set
            {
                if (_MayorID != value)
                {
                    Ratings = null;
                    FindController<NeighPageController>()?.GetMayorInfo(value);
                    RatingStars.LinkAvatar = value;
                }
                _MayorID = value;
            }
        }

        private uint _TownHallID;
        public uint TownHallID
        {
            get { return _TownHallID; }
            set
            {
                if (_TownHallID != value)
                {
                    FindController<NeighPageController>()?.GetTownHallInfo(value);
                }
                _TownHallID = value;
            }
        }

        private ImmutableList<uint> _Ratings;
        public ImmutableList<uint> Ratings
        {
            get
            {
                return _Ratings;
            }
            set
            {
                if ((_Ratings == null || value == null) || !(_Ratings.Count == value.Count && _Ratings.SequenceEqual(value)))
                {
                    _Ratings = value;
                    ShowRandomRatings();
                }
            }
        }

        private uint LastLotID;
        public void AsyncAPIThumb(uint lotID)
        {
            if (lotID == LastLotID) return;
            if (ThumbLock != null) ThumbLock.Held--;
            //LotThumbnail.SetThumbnail(DefaultThumb, CurrentLot.Value?.Id ?? 0);
            if (lotID != 0) ThumbLock = FindController<CoreGameScreenController>().Terrain.LockLotThumb(lotID);
            else ThumbLock = null;
            LastLotID = lotID;
        }

        public UINeighPage()
        {
            Add(BackgroundImage = new UIImage());

            Add(InfoTabBackgroundImage = new UIImage());
            Add(InfoTabImage = new UIImage());

            Add(TopSTab2BackgroundImage = new UIImage());
            Add(TopSTab3BackgroundImage = new UIImage());
            Add(TopSTabImage = new UIImage());

            Add(TopSTabTab1BackgroundImage = new UIImage());
            Add(TopSTabTab2BackgroundImage = new UIImage());
            Add(TopSTabTab3BackgroundImage = new UIImage());
            Add(TopSTabTab4BackgroundImage = new UIImage());

            Add(TopSTabTab1SeatImage = new UIImage());
            Add(TopSTabTab2SeatImage = new UIImage());
            Add(TopSTabTab3SeatImage = new UIImage());
            Add(TopSTabTab4SeatImage = new UIImage());

            Add(MayorTabBackgroundImage = new UIImage());
            Add(MayorTabImage = new UIImage());
            Add(MayorTabRateImage = new UIImage());

            MayorBanner = new UINeighBanner() { ScaleX = 0.3333f, ScaleY = 0.3333f,
                Caption = GameFacade.Strings.GetString("f115", "72") };
            TermBanner = new UINeighBanner() { ScaleX = 0.3333f, ScaleY = 0.3333f,
                Caption = GameFacade.Strings.GetString("f115", "83", new string[] { GetOrdinal(1) }) };
            TownNameBanner = new UINeighBanner()
            {
                ScaleX = 0.3333f,
                ScaleY = 0.3333f,
                Caption = GameFacade.Strings.GetString("f115", "14")
            };

            MayorRatingBox1 = new UIRatingSummaryPanel();
            MayorRatingBox2 = new UIRatingSummaryPanel();

            RatingStars = new UIRatingDisplay(false);
            RatingStars.HalfStars = 7;

            var script = RenderScript("fsoneighpage.uis");

            DescriptionText.OnChange += DescriptionText_OnChange;
            HouseNameButton.OnButtonClick += RenameAdmin;
            InfoButton.OnButtonClick += (btn) => SetTab(UINeighPageTab.Description);
            HouseButton.OnButtonClick += (btn) => SetTab(UINeighPageTab.Lots);
            PersonButton.OnButtonClick += (btn) => SetTab(UINeighPageTab.People);
            MayorButton.OnButtonClick += (btn) => SetTab(UINeighPageTab.Mayor);

            TopTab1Button.OnButtonClick += (btn) => SetSubTab(0);
            TopTab2Button.OnButtonClick += (btn) => SetSubTab(1);
            TopTab3Button.OnButtonClick += (btn) => SetSubTab(2);
            TopTab4Button.OnButtonClick += (btn) => SetSubTab(3);

            RateButton.OnButtonClick += RateSwitch;

            UIUtils.MakeDraggable(BackgroundImage, this, true);

            DescriptionSlider.AttachButtons(DescriptionScrollUpButton, DescriptionScrollDownButton, 1);
            DescriptionText.AttachSlider(DescriptionSlider);

            RateButton.Width = 60;
            LotThumbnail = script.Create<UILotThumbButton>("HouseThumbSetup");
            LotThumbnail.Init(RoommateThumbButtonImage, VisitorThumbButtonImage);
            DefaultThumb = TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, GameFacade.GameFilePath("userdata/houses/defaulthouse.bmp"));
            TextureUtils.ManualTextureMask(ref DefaultThumb, new uint[] { 0xFF000000 });
            LotThumbnail.SetThumbnail(DefaultThumb, 0);
            LotThumbnail.OnLotClick += (btn) => {
                FindController<CoreGameScreenController>()?.ShowLotPage(CurrentNeigh.Value?.Neighborhood_TownHallXY ?? 0);
            };
            Add(LotThumbnail);

            MayorPersonButton = script.Create<UIBigPersonButton>("MayorPersonButton");
            Add(MayorPersonButton);

            Add(RatingStars);
            Add(MayorBanner);
            Add(TermBanner);
            TermBanner.Flip = true;
            Add(TownNameBanner);
            Add(MayorRatingBox1);
            Add(MayorRatingBox2);

            CurrentNeigh = new Binding<Neighborhood>()
                .WithBinding(HouseNameButton, "Caption", "Neighborhood_Name")
                .WithBinding(ResidentCountLabel, "Caption", "Neighborhood_AvatarCount", (object ava) =>
                {
                    return GameFacade.Strings.GetString("f115", "15", new string[] { ava.ToString() });
                })
                .WithBinding(PropertyCountLabel, "Caption", "Neighborhood_LotCount", (object lot) =>
                {
                    return GameFacade.Strings.GetString("f115", "16", new string[] { lot.ToString() });
                })
                .WithBinding(ActivityRatingLabel, "Caption", "Neighborhood_ActivityRating", (object rate) =>
                {
                    return GameFacade.Strings.GetString("f115", "13", new string[] { rate.ToString() });
                })
                .WithBinding(TermBanner, "Caption", "Neighborhood_ElectedDate", (object rate) =>
                {
                    return GameFacade.Strings.GetString("f115", "83", new string[] { GetOrdinal(GetTermsSince((uint)rate)) });
                })
                .WithBinding(this, "MayorID", "Neighborhood_MayorID")
                .WithBinding(this, "TownHallID", "Neighborhood_TownHallXY")
                .WithBinding(StatusLabel, "Caption", "Neighborhood_Flag", (object flago) =>
                {
                    var flag = (uint)flago;
                    var availableText = "11";
                    if ((flag & 1) > 0) availableText = "12";
                    return GameFacade.Strings.GetString("f115", availableText);
                })
                .WithMultiBinding((changes) => Redraw(), "Neighborhood_Description", "Neighborhood_TownHallXY", "Neighborhood_MayorID",
                "Neighborhood_TopLotCategory", "Neighborhood_TopLotOverall", "Neighborhood_TopAvatarActivity", "Neighborhood_TopAvatarFamous",
                "Neighborhood_TopAvatarInfamous");

            CurrentMayor = new Binding<Avatar>()
                .WithBinding(RatingStars, "DisplayStars", "Avatar_MayorRatingHundredth", (object hundredths) =>
                {
                    return ((uint)hundredths) / 100f;
                })
                .WithBinding(this, "Ratings", "Avatar_ReviewIDs");

            CurrentTownHall = new Binding<Lot>()
                .WithBinding(TownNameBanner, "Caption", "Lot_Name", (object name) =>
                {
                    if ((string)name == "Retrieving...") return GameFacade.Strings.GetString("f115", "14");
                    else return (string)name;
                });

            CenterButton.OnButtonClick += (btn) =>
            {
                (UIScreen.Current as CoreGameScreen).CityRenderer.NeighGeom.CenterNHood((int)(CurrentNeigh.Value?.Id ?? 0));
            };

            BulletinButton.OnButtonClick += (btn) =>
            {
                var id = CurrentNeigh?.Value?.Id ?? 0;
                if (id != 0 && !UIBulletinDialog.Present)
                {
                    var dialog = new UIBulletinDialog(id);
                    dialog.CloseButton.OnButtonClick += (btn2) =>
                    {
                        UIScreen.RemoveDialog(dialog);
                    };
                    UIScreen.GlobalShowDialog(dialog, false);
                }
            };

            CloseButton.OnButtonClick += (btn) =>
            {
                FindController<NeighPageController>().Close();
            };

            //mayor action buttons:
            MayorActionMod.OnButtonClick += (btn) => { CurrentMayorTab = UINeighMayorTabMode.ModActions; Redraw(); };
            MayorActionReturn.OnButtonClick += (btn) => { CurrentMayorTab = UINeighMayorTabMode.Rate; Redraw(); };
            MayorActionMoveTH.OnButtonClick += (btn) => { MoveTownHall(true); };
            MayorActionNewTH.OnButtonClick += (btn) => { MoveTownHall(false); };
            ModActionSetMayor.OnButtonClick += ModSetMayor;

            ModActionReturn.OnButtonClick += (btn) => { CurrentMayorTab = UINeighMayorTabMode.Actions; Redraw(); };
            ModActionManageRatings.OnButtonClick += (btn) => {
                var ratingList = new UIRatingList(CurrentMayor.Value?.Avatar_Id ?? 0);
                UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Title = GameFacade.Strings.GetString("f118", "23", new string[] { "Retrieving..." }),
                    Message = GameFacade.Strings.GetString("f118", "24", new string[] { "Retrieving..." }),
                    GenericAddition = ratingList,
                    Width = 530
                }, true);
            };

            foreach (var elem in Children)
            {
                var label = elem as UILabel;
                if (label != null)
                {
                    label.CaptionStyle = label.CaptionStyle.Clone();
                    label.CaptionStyle.Shadow = true;
                }
            }

            Pedestals = new List<UITop10Pedestal>();
            Top10Labels = new List<UILabel>();
            var top10style = TextStyle.DefaultLabel.Clone();
            top10style.Shadow = true;
            top10style.Size = 8;
            for (int i = 0; i < 10; i++)
            {
                var alt = i % 2 == 1;
                var ped = new UITop10Pedestal()
                {
                    AltColor = alt
                };
                Pedestals.Add(ped);

                var label = new UILabel()
                {
                    CaptionStyle = top10style,
                    Alignment = TextAlignment.Center | TextAlignment.Middle,
                    Size = new Vector2(1, 1)
                };
                Top10Labels.Add(label);
                Add(label);
            }
            for (int i = 0; i < 10; i += 2) Add(Pedestals[i]); //backmost
            for (int i = 1; i < 10; i += 2) Add(Pedestals[i]); //frontmost

            SetPedestalPosition(false, true);

            Redraw();
        }

        private void ModSetMayor(UIElement button)
        {
            FindController<NeighPageController>()?.ModSetMayor();
        }

        private void RefreshMayor()
        {
            var oldID = _MayorID;
            _MayorID = 0;
            MayorID = oldID;
        }

        private void RateSwitch(UIElement button)
        {
            if (MayorIsMe || GameFacade.EnableMod)
            {
                CurrentMayorTab = UINeighMayorTabMode.Actions;
                Redraw();
            }
            else
            {
                FindController<CoreGameScreenController>()?.NeighborhoodProtocol?.BeginRating
                    (CurrentNeigh.Value?.Id ?? 0,
                    CurrentNeigh.Value?.Neighborhood_MayorID ?? 0,
                    (success) =>
                    {
                        if (success == Server.Protocol.Electron.Packets.NhoodResponseCode.SUCCESS)
                        {
                            UIAlert.Alert("", GameFacade.Strings.GetString("f115", "97"), true);
                        }
                        GameThread.SetTimeout(() =>
                        {
                            RefreshMayor();
                        }, 500);
                    });
            }
        }

        private void MoveTownHall(bool move)
        {
            if (CurrentNeigh.Value != null)
            {
                FindController<CoreGameScreenController>()?.Terrain.PlaceTownHall(
                    move,
                    CurrentNeigh.Value.Id,
                    CurrentNeigh.Value.Neighborhood_Name);
            }
        }

        private void DescriptionText_OnChange(UIElement element)
        {
            DescriptionChanged = true;
        }

        private void ShowRandomRatings()
        {
            RatingCycleTime = 0;
            var rate = _Ratings;
            if (rate == null || rate.Count == 0)
            {
                MayorRatingBox1.FindController<RatingSummaryController>()?.SetRating(0);
                MayorRatingBox2.FindController<RatingSummaryController>()?.SetRating(0);
            }
            else if (rate.Count == 1)
            {
                MayorRatingBox1.FindController<RatingSummaryController>()?.SetRating(rate.First());
                MayorRatingBox2.FindController<RatingSummaryController>()?.SetRating(0);
            }
            else
            {
                var rand = new Random();
                //select a random "bad" review and a random "good" one
                //bad is from lower 50%, good is from upper.
                MayorRatingBox1.FindController<RatingSummaryController>()?.SetRating(rate[rand.Next(rate.Count/2)]);
                MayorRatingBox2.FindController<RatingSummaryController>()?.SetRating(rate[rate.Count / 2 + rand.Next(rate.Count-(rate.Count / 2))]);
            }
        }

        private void RenameAdmin(UIElement button)
        {
            if (GameFacade.EnableMod)
            {
                var lotName = new UILotPurchaseDialog();
                lotName.OnNameChosen += (name) =>
                {
                    if (CurrentNeigh != null && CurrentNeigh.Value != null)
                    {
                        CurrentNeigh.Value.Neighborhood_Name = name;
                        FindController<NeighPageController>().SaveName(CurrentNeigh.Value);
                    }
                    UIScreen.RemoveDialog(lotName);

                    var terrain = (UIScreen.Current as CoreGameScreen)?.CityRenderer;
                    if (terrain != null)
                    {
                        terrain.NeighGeom.Data[terrain.NeighGeom.ToID((int)CurrentNeigh.Value.Id)].Name = name;
                    }
                };
                if (CurrentNeigh != null && CurrentNeigh.Value != null)
                {
                    lotName.NameTextEdit.CurrentText = CurrentNeigh.Value.Neighborhood_Name;
                }
                UIScreen.GlobalShowDialog(new DialogReference
                {
                    Dialog = lotName,
                    Controller = this,
                    Modal = true,
                });
            }
        }

        public void ForcePropertyFilters()
        {
            if (CurrentNeigh.Value != null)
            {
                var gizmo = (UIScreen.Current as CoreGameScreen)?.gizmo;
                if (gizmo != null)
                {
                    if (CurrentLotTab == UINeighLotsTab.TopCategory)
                    {
                        gizmo.FilterList = CurrentNeigh.Value.Neighborhood_TopLotCategory ?? ImmutableList<uint>.Empty;
                    }
                    else
                    {
                        gizmo.FilterList = CurrentNeigh.Value.Neighborhood_TopLotOverall ?? ImmutableList<uint>.Empty;
                    }
                }
            }
            HasShownFilters = true;
        }

        private void SetPedestalPosition(bool categories, bool set)
        {
            if (set) {
                for (int i = 0; i < 10; i++)
                {
                    var alt = i % 2 == 1;
                    Pedestals[i].Position = new Vector2(118 + 32 * i, alt ? 181 : 165);
                }
            } else
            {
                for (int i = 0; i < 10; i++)
                {
                    var alt = i % 2 == 1;
                    GameFacade.Screens.Tween.To(Pedestals[i], 0.5f,
                        new Dictionary<string, float>() {
                            { "Y", (alt ? 181f : 165f) - (categories?20:0) }
                        }, TweenQuad.EaseOut);
                }
            }
        }

        private void SetTopLabelState(bool isPeople, bool categories)
        {
            for (int i = 0; i < 10; i++)
            {
                var alt = i % 2 == 1;
                if (isPeople) alt = true;
                var label = Top10Labels[i];

                var topHeight = 97;
                var btmHeight = 205;
                if (!isPeople)
                {
                    if (categories)
                    {
                        btmHeight -= 16;
                        topHeight = 113;
                    }
                    else
                    {
                        topHeight = 97 + Math.Min(i, 3) * 12;
                    }
                }

                label.Position = new Vector2(118 + 32 * i, alt ? btmHeight : topHeight);
                if (categories)
                {
                    label.Caption = GameFacade.Strings.GetString("f115", (73 + i).ToString());
                }
                else
                {
                    label.Caption = GetOrdinal(i+1);
                }
            }
        }

        private string GetOrdinal(int place)
        {
            if ((place / 10) % 10 == 1)
                return GameFacade.Strings.GetString("f115", "26", new string[] { place.ToString() });
            else
            {
                if (place > 0 && place < 4)
                    return GameFacade.Strings.GetString("f115", (22+place).ToString(), new string[] { place.ToString() });
                else
                    return GameFacade.Strings.GetString("f115", "26", new string[] { place.ToString() });
            }
        }

        private int GetTermsSince(uint time)
        {
            var startDate = ClientEpoch.ToDate(time);
            var now = DateTime.UtcNow;

            var months = 0;
            if (now.Year != startDate.Year)
            {
                months += (((now.Year - startDate.Year) - 1) * 12 + 13 - startDate.Month);
                months += now.Month-1;
            } else
            {
                months = (now.Month - startDate.Month)+1;
            }
            return months + 1;
        }

        public override void Update(UpdateState state)
        {
            if (ThumbLock != null && CurrentNeigh?.Value != null && LastThumb != ThumbLock.LotTexture && ThumbLock.Loaded)
            {
                LotThumbnail.SetThumbnail(ThumbLock.LotTexture, ThumbLock.Location);
                LastThumb = ThumbLock.LotTexture;
            } else if (ThumbLock == null && LastThumb != null)
            {
                LotThumbnail.SetThumbnail(null, 0);
                LastThumb = null;
            }
            if (Visible && CurrentNeigh?.Value != null)
            {
                if (GameFacade.EnableMod) DescriptionText.Mode = UITextEditMode.Editor;
                string mayorString;
                if (CurrentNeigh.Value.Neighborhood_MayorID != 0)
                    mayorString = GameFacade.Strings.GetString("f115", "22", new string[] { MayorPersonButton.MainButton.Tooltip });
                else
                    mayorString = GameFacade.Strings.GetString("f115", "72");
                if (MayorBanner.Caption != mayorString)
                {
                    MayorBanner.Caption = mayorString;
                }

                if (CurrentTab == UINeighPageTab.Mayor)
                {
                    if (RatingCycleTime++ >= RATING_CYCLE_DURATION * FSOEnvironment.RefreshRate)
                    {
                        ShowRandomRatings();
                    }
                }
            }
            base.Update(state);
        }

        public void TrySaveDescription()
        {
            if (CurrentNeigh != null && CurrentNeigh.Value != null && GameFacade.EnableMod
                && DescriptionText.CurrentText != CurrentNeigh.Value.Neighborhood_Description && DescriptionChanged)
            {
                CurrentNeigh.Value.Neighborhood_Description = DescriptionText.CurrentText;
                FindController<NeighPageController>().SaveDescription(CurrentNeigh.Value);
                DescriptionChanged = false;
            }
        }

        private void SetTab(UINeighPageTab tab)
        {
            if (tab == UINeighPageTab.Lots) ForcePropertyFilters();
            CurrentTab = tab;
            Redraw();
            FindController<NeighPageController>().ChangeTopic();
        }

        private void SetSubTab(int tab)
        {
            if (CurrentTab == UINeighPageTab.Lots)
            {
                CurrentLotTab = (UINeighLotsTab)tab;
                ForcePropertyFilters();
            } else
            {
                CurrentPersonTab = (UINeighPersonTab)tab;
            }
            Redraw();
        }

        private ImmutableList<uint> GetData()
        {
            if (CurrentNeigh.Value != null)
            {
                if (CurrentTab == UINeighPageTab.People)
                {
                    switch (CurrentPersonTab)
                    {
                        case UINeighPersonTab.TopActivity:
                            return CurrentNeigh.Value.Neighborhood_TopAvatarActivity ?? ImmutableList<uint>.Empty;
                        case UINeighPersonTab.TopFamous:
                            return CurrentNeigh.Value.Neighborhood_TopAvatarFamous ?? ImmutableList<uint>.Empty;
                        case UINeighPersonTab.TopInfamous:
                            return CurrentNeigh.Value.Neighborhood_TopAvatarInfamous ?? ImmutableList<uint>.Empty;
                    }
                }
                else
                {
                    switch (CurrentLotTab)
                    {
                        case UINeighLotsTab.TopOverall:
                            //return ImmutableList.Create<uint>(13828398, 13828398, 13828398, 13828398, 13828398, 13828398, 13828398, 13828398, 13828398, 13828398);
                            return CurrentNeigh.Value.Neighborhood_TopLotOverall ?? ImmutableList<uint>.Empty;
                        case UINeighLotsTab.TopCategory:
                            return CurrentNeigh.Value.Neighborhood_TopLotCategory ?? ImmutableList<uint>.Empty;
                    }
                }
            }
            return ImmutableList<uint>.Empty;
        }

        private void Redraw()
        {
            var isDesc = CurrentTab == UINeighPageTab.Description;
            var isLot = CurrentTab == UINeighPageTab.Lots;
            var isPeople = CurrentTab == UINeighPageTab.People;
            var isTop = isLot || isPeople;
            var isMayor = CurrentTab == UINeighPageTab.Mayor;

            InfoButton.Selected = isDesc;
            HouseButton.Selected = isLot;
            PersonButton.Selected = isPeople;
            MayorButton.Selected = isMayor;

            InfoTabBackgroundImage.Visible = isDesc;
            InfoTabImage.Visible = isDesc;

            LotThumbnail.Visible = isDesc;
            DescriptionText.Visible = isDesc;
            DescriptionSlider.Visible = isDesc;
            DescriptionScrollDownButton.Visible = isDesc;
            DescriptionScrollUpButton.Visible = isDesc;

            StatusLabel.Visible = isDesc;
            ActivityRatingLabel.Visible = isDesc;
            TownNameBanner.Visible = isDesc;
            //TownHallNameLabel.Visible = isDesc;
            ResidentCountLabel.Visible = isDesc;
            PropertyCountLabel.Visible = isDesc;

            TabTypeLabel.Visible = isTop;
            TopSTab2BackgroundImage.Visible = isLot;
            TopSTab3BackgroundImage.Visible = isPeople;
            TopSTabImage.Visible = isTop;

            var topData = GetData();
            for (int i=0; i<Pedestals.Count; i++)
            {
                var ped = Pedestals[i];
                var label = Top10Labels[i];
                label.Visible = isTop;
                ped.Visible = isTop;
                if (!isTop) ped.Height = 0;
                else
                {
                    if (isLot && CurrentLotTab == UINeighLotsTab.TopCategory)
                        ped.SetPlace(0);
                    else
                        ped.SetPlace(i + 1);

                    //set the top items
                    if (i >= topData.Count)
                    {
                        ped.AvatarId = 0;
                        ped.LotId = 0;
                    }
                    else
                    {
                        if (isLot)
                        {
                            ped.LotId = topData[i];
                            ped.AvatarId = 0;
                        }
                        else
                        {
                            ped.LotId = 0;
                            ped.AvatarId = topData[i];
                        }
                    }
                }
            }
            

            if (isTop) {
                SetPedestalPosition(isLot && CurrentLotTab == UINeighLotsTab.TopCategory, false);
                SetTopLabelState(isPeople, isLot && CurrentLotTab == UINeighLotsTab.TopCategory);
                if (isLot)
                {
                    TabTypeLabel.Caption = GameFacade.Strings.GetString("f115", (17+(int)CurrentLotTab).ToString());

                    TopTab1Button.Tooltip = GameFacade.Strings.GetString("f115", "17");
                    TopTab2Button.Tooltip = GameFacade.Strings.GetString("f115", "18");
                }
                else
                {
                    TabTypeLabel.Caption = GameFacade.Strings.GetString("f115", (19+(int)CurrentPersonTab).ToString());

                    TopTab1Button.Tooltip = GameFacade.Strings.GetString("f115", "19");
                    TopTab2Button.Tooltip = GameFacade.Strings.GetString("f115", "20");
                    TopTab3Button.Tooltip = GameFacade.Strings.GetString("f115", "21");
                }
            }

            TopSTabTab1BackgroundImage.Visible = (isLot && CurrentLotTab == UINeighLotsTab.TopOverall) || (isPeople && CurrentPersonTab == UINeighPersonTab.TopActivity);
            TopSTabTab2BackgroundImage.Visible = (isLot && CurrentLotTab == UINeighLotsTab.TopCategory) || (isPeople && CurrentPersonTab == UINeighPersonTab.TopFamous);
            TopSTabTab3BackgroundImage.Visible = isPeople && CurrentPersonTab == UINeighPersonTab.TopInfamous;
            TopSTabTab4BackgroundImage.Visible = false;

            TopSTabTab1SeatImage.Visible = (isTop && !TopSTabTab1BackgroundImage.Visible);
            TopSTabTab2SeatImage.Visible = (isTop && !TopSTabTab2BackgroundImage.Visible);
            TopSTabTab3SeatImage.Visible = (isPeople && !TopSTabTab3BackgroundImage.Visible);
            TopSTabTab4SeatImage.Visible = false;

            TopTab1Button.Visible = isTop;
            TopTab2Button.Visible = isTop && !isPeople; //top famous and infamous disabled til relationship rework
            TopTab3Button.Visible = false;// isPeople;
            TopTab4Button.Visible = false;

            TopTab1Button.Selected = TopSTabTab1BackgroundImage.Visible;
            TopTab2Button.Selected = TopSTabTab2BackgroundImage.Visible;
            TopTab3Button.Selected = TopSTabTab3BackgroundImage.Visible;
            TopTab4Button.Selected = TopSTabTab4BackgroundImage.Visible;

            MayorTabBackgroundImage.Visible = isMayor;
            MayorTabImage.Visible = isMayor;
            
            MayorPersonButton.Visible = isMayor;

            MayorElectionLabel.Visible = isMayor;
            MayorNominationLabel.Visible = isMayor;
            
            var now = ClientEpoch.Now;
            bool hasMayor = false;
            bool iAmMayor = false;
            
            if (CurrentNeigh.Value != null) {
                iAmMayor = FindController<CoreGameScreenController>().IsMe(CurrentNeigh.Value.Neighborhood_MayorID);
                MayorIsMe = iAmMayor;
                if (CurrentTab == UINeighPageTab.Description && !DescriptionChanged)
                {
                    DescriptionText.CurrentText = CurrentNeigh.Value.Neighborhood_Description;
                }
                if (!HasShownFilters 
                    && CurrentNeigh.Value.Neighborhood_TopLotCategory != null && CurrentNeigh.Value.Neighborhood_TopLotCategory.Count > 0
                    && CurrentNeigh.Value.Neighborhood_TopLotOverall != null && CurrentNeigh.Value.Neighborhood_TopLotOverall.Count > 0)
                {
                    ForcePropertyFilters();
                }
                AsyncAPIThumb(CurrentNeigh.Value.Neighborhood_TownHallXY);
                if (MayorPersonButton.AvatarId != CurrentNeigh.Value.Neighborhood_MayorID)
                    MayorPersonButton.AvatarId = CurrentNeigh.Value.Neighborhood_MayorID;
                var hasElect = (CurrentNeigh.Value.Neighborhood_Flag & 2) == 0;
                hasMayor = CurrentNeigh.Value.Neighborhood_MayorID != 0;
                if (isMayor)
                {
                    //render election data
                    MayorElectionLabel.Visible = hasElect;
                    MayorNominationLabel.Visible = hasElect;

                    if (hasElect && CurrentNeigh.Value.Neighborhood_ElectionCycle != null)
                    {
                        var electionOver = ClientEpoch.Now > CurrentNeigh.Value.Neighborhood_ElectionCycle.ElectionCycle_EndDate;
                        var currentMayorDate = ClientEpoch.ToDate(CurrentNeigh.Value.Neighborhood_ElectedDate).ToLocalTime();
                        var awaitingResult = electionOver && CurrentNeigh.Value.Neighborhood_ElectedDate < CurrentNeigh.Value.Neighborhood_ElectionCycle.ElectionCycle_EndDate;
                        MayorStatusLabel.Caption = GameFacade.Strings.GetString("f115", awaitingResult ? "29" : "30", new string[] { currentMayorDate.ToShortDateString() });

                        var electionDay = CurrentNeigh.Value.Neighborhood_ElectionCycle.ElectionCycle_EndDate;

                        MayorElectionLabel.Caption = GameFacade.Strings.GetString("f115", "31", new string[] { TimeLeftToString((int)(electionDay - now)) });

                        MayorNominationLabel.Caption = GameFacade.Strings.GetString("f115", "32", new string[] { TimeLeftToString((int)((electionDay-60*60*24*3) - now)) });
                    }
                    else
                    {
                        MayorStatusLabel.Caption = GameFacade.Strings.GetString("f115", "28");
                    }
                }
                var hallLoc = CurrentNeigh.Value.Neighborhood_TownHallXY;
                HasTownHall = hallLoc != 0;
            }

            if (isMayor) {
                var canUseExtraTools = iAmMayor || GameFacade.EnableMod;
                MayorRatingFlairLabel.Caption = GameFacade.Strings.GetString("f115", (37 + (int)CurrentMayorTab).ToString());
                RateButton.Caption = GameFacade.Strings.GetString("f115", (canUseExtraTools) ? "89" : "33");
                if (!canUseExtraTools) CurrentMayorTab = UINeighMayorTabMode.Rate;
            }

            MayorStatusLabel.Visible = isMayor && hasMayor;

            bool isRating = isMayor && CurrentMayorTab == UINeighMayorTabMode.Rate && hasMayor;
            MayorTabRateImage.Visible = isRating;
            RateButton.Visible = isRating || GameFacade.EnableMod && isMayor;
            MayorRatingBox1.Visible = isRating;
            MayorRatingBox2.Visible = isRating;

            bool isMayorAction = isMayor && CurrentMayorTab == UINeighMayorTabMode.Actions;

            MayorActionMod.Visible = isMayorAction && GameFacade.EnableMod;
            MayorActionMoveTH.Visible = isMayorAction;
            MayorActionMoveTH.Disabled = !HasTownHall;
            MayorActionNewTH.Visible = isMayorAction;
            MayorActionNewTH.Disabled = HasTownHall;
            MayorActionOrdinances.Visible = false; //isMayorAction;
            MayorActionReturn.Visible = isMayorAction;

            bool isModAction = isMayor && CurrentMayorTab == UINeighMayorTabMode.ModActions;

            MayorRatingFlairLabel.Visible = isRating || isMayorAction || isModAction;

            ModActionManageRatings.Visible = isModAction;
            ModActionReserved.Visible = isModAction;
            ModActionReturn.Visible = isModAction;
            ModActionSetMayor.Visible = isModAction;
            ModActionTestCycle.Visible = isModAction;

            MayorBanner.Visible = isMayor;
            RatingStars.Visible = isMayor && hasMayor;
            TermBanner.Visible = isMayor && hasMayor;
        }

        private string TimeLeftToString(int secondsLeft)
        {
            if (secondsLeft <= 0)
            {
                return GameFacade.Strings.GetString("f115", "71"); //done!
            }
            else if (secondsLeft < 60)
            {
                return GameFacade.Strings.GetString("f115", "34", "<1"); //<1 mins left
            }
            else if (secondsLeft < 60*60)
            {
                return GameFacade.Strings.GetString("f115", "34", new string[] { (secondsLeft / 60 + 1).ToString() }); //mins left
            }
            else if (secondsLeft < 60*60*24)
            {
                return GameFacade.Strings.GetString("f115", "35", new string[] { (secondsLeft / (60 * 60) + 1).ToString() }); //hours left
            }
            else
            {
                return GameFacade.Strings.GetString("f115", "36", new string[] { (secondsLeft / (60 * 60 * 24) + 1).ToString() }); //days left
            }
        }
    }

    public enum UINeighPageTab
    {
        Description,
        Lots,
        People,
        Mayor
    }

    public enum UINeighLotsTab : int
    {
        TopOverall = 0,
        TopCategory
    }

    public enum UINeighPersonTab : int
    {
        TopActivity = 0,
        TopFamous,
        TopInfamous
    }

    public enum UINeighMayorTabMode : int
    {
        Rate = 0, //rate or view ratings for the current mayor. if mayor is us, replace rate button with switch to "Actions".
        Actions, //move town hall
        ModActions //force mayor, dummy nhood date for process, etc 
    }
}
