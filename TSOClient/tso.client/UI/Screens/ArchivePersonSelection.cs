using FSO.Client.Controllers;
using FSO.Client.Properties;
using FSO.Client.Regulators;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.UI.Model;
using FSO.Client.UI.Panels;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.Utils;
using FSO.Common.Utils.Cache;
using FSO.Files;
using FSO.HIT;
using FSO.Server.Clients;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Electron.Packets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.Client.UI.Screens
{
    public class ArchivePersonSelection : GameScreen, IArchiveCharacterSelector
    {
        /// <summary>
        /// Values from the UIScript
        /// </summary>
        public Texture2D BackgroundImage { get; set; }
        public Texture2D BackgroundImageDialog { get; set; }

        public Texture2D SimCreateButtonImage { get; set; }
        public Texture2D SimSelectButtonImage { get; set; }
        public Texture2D HouseButtonTemplateImage { get; set; }
        public Texture2D CityButtonTemplateImage { get; set; }
        public Texture2D CityHouseButtonAlpha { get; set; }

        public UIButton CreditsButton { get; set; }
        private ArchivePersonSlot PersonSlot { get; set; }
        private UIButton m_ExitButton;

        public ApiClient Api;

        public LoginRegulator LoginRegulator;
        public UIButton CASButton { get; set; }
        public UIButton AcceptButton { get; set; }

        public UITextBox SearchBox;
        public UIListBox AvatarListBox;
        public UIImage ListBackground;
        public UILabel StatusLabel;

        private UIListBoxTextStyle ListBoxColors;
        private ArchiveAvatarsResponse Data;

        public UILabel SearchLabel;
        public UILabel ListNameLabel;
        public UILabel ListLotLabel;

        public Texture2D SimIconShared;
        public Texture2D SimIconOwned;
        public Texture2D SimIconRecent;

        public UILabel TitleLabel { get; set; }

        public ArchivePersonSelection() : base()
        {
            var offset = (new Vector2(1024, 768) - new Vector2(800, 600)) / 2;
            var gd = GameFacade.GraphicsDevice;
            var custom = Content.Content.Get().CustomUI;

            BackgroundImageDialog = custom.Get("archive_sasbg.png").Get(gd);

            SimIconShared = custom.Get("archive_simshared.png").Get(gd);
            SimIconOwned = custom.Get("archive_simowned.png").Get(gd);
            SimIconRecent = custom.Get("archive_simrecent.png").Get(gd);

            //Arrange UI
            Api = new ApiClient(ApiClient.CDNUrl ?? GlobalSettings.Default.GameEntryUrl);

            UIScript ui = null;
            ui = this.RenderScript("archivepersonselection1024.uis");

            Position = new Vector2(ScaleX * (GlobalSettings.Default.GraphicsWidth - 1024) / 2, ScaleY * (GlobalSettings.Default.GraphicsHeight - 768) / 2);

            m_ExitButton = (UIButton)ui["ExitButton"];

            var numSlots = 1;

            CreditsButton.Disabled = true;
            TitleLabel.Alignment = TextAlignment.Center | TextAlignment.Middle;
            TitleLabel.Size = new Vector2(620, 20); // For some reason, this changes to 525x20, so I need to change it back?

            for (var i = 0; i < numSlots; i++)
            {
                var index = (i + 1).ToString();

                /** Tab Background **/
                var tabBackground = ui.Create<UIImage>("TabBackgroundImage" + index);
                this.Add(tabBackground);

                tabBackground.With9Slice(0, 0, 75, 50);
                tabBackground.Height += 18;

                var enterTabImage = ui.Create<UIImage>("EnterTabImage" + index);
                this.Add(enterTabImage);

                var descTabImage = ui.Create<UIImage>("DescriptionTabImage" + index);
                this.Add(descTabImage);

                var descTabBgImage = ui.Create<UIImage>("DescriptionTabBackgroundImage" + index);
                var enterIcons = ui.Create<UIImage>("EnterTabBackgroundImage" + index);

                var personSlot = new ArchivePersonSlot(this)
                {
                    AvatarButton = (UIButton)ui["AvatarButton" + index],
                    CityButton = (UIButton)ui["CityButton" + index],
                    HouseButton = (UIButton)ui["HouseButton" + index],
                    EnterTabButton = (UIButton)ui["EnterTabButton" + index],
                    DescTabButton = (UIButton)ui["DescriptionTabButton" + index],
                    NewAvatarButton = (UIButton)ui["NewAvatarButton" + index],
                    DeleteAvatarButton = (UIButton)ui["DeleteAvatarButton" + index],
                    PersonNameText = (UILabel)ui["PersonNameText" + index],
                    PersonDescriptionScrollUpButton = (UIButton)ui["PersonDescriptionScrollUpButton" + index],
                    PersonDescriptionScrollDownButton = (UIButton)ui["PersonDescriptionScrollDownButton" + index],
                    PersonDescriptionSlider = (UISlider)ui["PersonDescriptionSlider" + index],
                    CityNameText = (UILabel)ui["CityNameText" + index],
                    HouseNameText = (UILabel)ui["HouseNameText" + index],
                    PersonDescriptionText = (UITextEdit)ui["PersonDescriptionText" + index],
                    DescriptionTabBackgroundImage = descTabBgImage,
                    EnterTabBackgroundImage = enterIcons,

                    TabBackground = tabBackground,
                    TabEnterBackground = enterTabImage,
                    TabDescBackground = descTabImage
                };

                this.AddBefore(descTabBgImage, personSlot.PersonDescriptionText);
                this.AddBefore(enterIcons, personSlot.CityButton);

                personSlot.Init();
                personSlot.SetSlotAvailable(true);
                PersonSlot = personSlot;
            }

            /** Backgrounds **/
            var bg = new UIImage(BackgroundImage).With9Slice(128, 128, 84, 84);
            this.AddAt(0, bg);
            bg.SetSize(GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight);
            bg.Position = new Vector2((GlobalSettings.Default.GraphicsWidth - 1024) / -2, (GlobalSettings.Default.GraphicsHeight - 768) / -2);
            Background = bg;

            if (BackgroundImageDialog != null)
            {
                this.AddAt(1, new UIImage(BackgroundImageDialog)
                {
                    X = 112,
                    Y = 84
                });
            }

            /** Archive controls **/

            var titleFont = TextStyle.DefaultLabel.Clone();
            titleFont.Size = 10;
            titleFont.Shadow = true;

            Add(SearchLabel = new UILabel()
            {
                Caption = "Search",
                Position = new Vector2(394, 92) + offset,
                CaptionStyle = titleFont
            });

            Add(ListNameLabel = new UILabel()
            {
                Caption = "Name",
                Position = new Vector2(397, 230) + offset,
                CaptionStyle = titleFont
            });

            Add(ListNameLabel = new UILabel()
            {
                Caption = "Lot",
                Position = new Vector2(397 + 163, 230) + offset,
                CaptionStyle = titleFont
            });

            var entryFont = TextStyle.DefaultLabel.Clone();
            entryFont.Size = 12;

            var listTex = custom.Get("archive_translist.png").Get(gd);

            Add(SearchBox = new UITextBox()
            {
                Position = new Vector2(385, 112) + offset,
                TextStyle = entryFont
            });

            SearchBox.SetBackgroundTexture(listTex, 13, 13, 13, 13);
            SearchBox.SetSize(304, 39);
            SearchBox.TextMargin = new Rectangle(14, 8, 14, 8);

            var searchFont = TextStyle.DefaultLabel.Clone();
            searchFont.Size = 10;

            ListBoxColors = new UIListBoxTextStyle(searchFont)
            {
                NormalColor = new Color(247, 232, 145),
                SelectedColor = new Color(0, 0, 0),
                HighlightedColor = new Color(255, 255, 255),
                DisabledColor = new Color(150, 150, 150)
            };

            ListBackground = new UIImage(listTex).With9Slice(13, 13, 13, 13);
            ListBackground.Position = new Vector2(365, 250) + offset;
            ListBackground.SetSize(358, 315);
            Add(ListBackground);

            Add(AvatarListBox = new UIListBox()
            {
                Size = ListBackground.Size - new Vector2(20, 20),
                Position = ListBackground.Position + new Vector2(10, 10),
                Mask = true,
                VisibleRows = 15,
                Columns = new UIListBoxColumnCollection()
                {
                    new UIListBoxColumn() { Width = 22, Alignment = TextAlignment.Left | TextAlignment.Middle },
                    new UIListBoxColumn() { Width = 163, Alignment = TextAlignment.Left | TextAlignment.Middle },
                    new UIListBoxColumn() { Width = 163, Alignment = TextAlignment.Left | TextAlignment.Middle }
                },
                FontStyle = searchFont,
                SelectionFillColor = new Color(250, 200, 140),
                ScrollbarImage = GetTexture(0x31000000001),
                ScrollbarGutter = 17,
                RowHeight = 20
            });

            AvatarListBox.InitDefaultSlider();

            var statusStyle = TextStyle.DefaultLabel.Clone();
            statusStyle.Shadow = true;

            Add(StatusLabel = new UILabel()
            {
                Caption = "Loading...",
                Position = AvatarListBox.Position,
                Size = AvatarListBox.Size,
                Wrapped = true,
                Alignment = TextAlignment.Center | TextAlignment.Middle,
                CaptionStyle = statusStyle,
            });

            AvatarListBox.OnChange += ChangedSelectedAvatar;

            /**
             * Button plumbing
             */
            CreditsButton.OnButtonClick += new ButtonClickDelegate(CreditsButton_OnButtonClick);
            m_ExitButton.OnButtonClick += new ButtonClickDelegate(m_ExitButton_OnButtonClick);
            CASButton.OnButtonClick += OpenCAS;
            AcceptButton.OnButtonClick += AcceptSelection;
            AcceptButton.Disabled = true;
            SearchBox.OnChange += (elem) =>
            {
                RefreshList();
            };

            ControllerUtils.BindController<ArchiveCharactersSelectorController>(this);

            FindController<ArchiveCharactersSelectorController>().Refresh();

            GameFacade.Screens.inputManager.SetFocus(SearchBox);

            /**
             * Music
             */

            HITVM.Get().PlaySoundEvent(UIMusic.SAS);

            GameThread.NextUpdate(x =>
            {
                // TODO: archive SAS hint
                //FSOFacade.Hints.TriggerHint("screen:sas");
            });
        }

        private void AcceptSelection(UIElement button)
        {
            SelectAvatar(button, false);
        }

        private void ChangedSelectedAvatar(UIElement element)
        {
            PersonSlot.AvatarButton.Disabled = AvatarListBox.SelectedItem == null;
            AcceptButton.Disabled = PersonSlot.AvatarButton.Disabled;

            if (PersonSlot.AvatarButton.Disabled)
            {
                PersonSlot.SetSlotAvailable(true);
            }
            else
            {
                var ava = (ArchiveAvatar)AvatarListBox.SelectedItem.Data;
                PersonSlot.DisplayAvatar(ava);
            }
        }

        public void SelectAvatar(Framework.UIElement button, bool gotoHouse)
        {
            if (AvatarListBox.SelectedItem == null)
            {
                return;
            }

            var ava = (ArchiveAvatar)AvatarListBox.SelectedItem.Data;
            FindController<ConnectArchiveController>().SelectAvatar(ava.AvatarId, gotoHouse ? ava.LotId : 0u);
        }

        public void OpenCAS(Framework.UIElement button)
        {
            FSOFacade.Controller.GotoCAS(true);
        }

        public void SetData(ArchiveAvatarsResponse data)
        {
            Data = data;

            if (!data.IsVerified)
            {
                StatusLabel.Visible = true;
                StatusLabel.Caption = "Waiting for verification...";
            }
            else
            {
                StatusLabel.Visible = false;
            }

            RefreshList();
        }

        public void RefreshList()
        {
            var query = (SearchBox.CurrentText ?? "").ToLower();

            if (Data == null)
            {
                // Empty the list
                AvatarListBox.Items.Clear();
            }
            else
            {
                var recentIds = Data.RecentAvatars;

                var myItems = Data.UserAvatars
                    .Where(x => x.Name.ToLower().Contains(query) || x.LotName.ToLower().Contains(query))
                    .Select((ArchiveAvatar x) =>
                    {
                        return new UIListBoxItem(x, new object[] { SimIconOwned, x.Name, x.LotName })
                        {
                            CustomStyle = ListBoxColors,
                        };
                    });

                var recentSorted = new ArchiveAvatar[Data.RecentAvatars.Length];

                foreach (var shared in Data.SharedAvatars)
                {
                    int index = Array.IndexOf(recentIds, shared.AvatarId);
                    if (index != -1)
                    {
                        recentSorted[index] = shared;                        
                    }
                }

                var recentItems = recentSorted
                    .Where(x => x.AvatarId != 0 && (x.Name.ToLower().Contains(query) || x.LotName.ToLower().Contains(query)))
                    .Select((ArchiveAvatar x) =>
                    {
                        return new UIListBoxItem(x, new object[] { SimIconRecent, x.Name, x.LotName })
                        {
                            CustomStyle = ListBoxColors,
                        };
                    });

                var sharedItems = Data.SharedAvatars
                    .Where(x => !recentIds.Contains(x.AvatarId) && (x.Name.ToLower().Contains(query) || x.LotName.ToLower().Contains(query)))
                    .Select((ArchiveAvatar x) =>
                    {
                        return new UIListBoxItem(x, new object[] { SimIconShared, x.Name, x.LotName })
                        {
                            CustomStyle = ListBoxColors,
                        };
                    });

                AvatarListBox.Items.Clear();

                AvatarListBox.Items.AddRange(myItems);
                AvatarListBox.Items.AddRange(recentItems);
                AvatarListBox.Items.AddRange(sharedItems);
            }

            AvatarListBox.Items = AvatarListBox.Items;
            Invalidate();
        }

        private UIImage Background;

        public override void GameResized()
        {
            base.GameResized();
            Position = new Vector2(ScaleX * (GlobalSettings.Default.GraphicsWidth - 1024) / 2, ScaleY * (GlobalSettings.Default.GraphicsHeight - 768) / 2);
            Background.SetSize(GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight);
            Background.Position = new Vector2((GlobalSettings.Default.GraphicsWidth - 1024) / -2, (GlobalSettings.Default.GraphicsHeight - 768) / -2);
            InvalidateMatrix();
            Parent?.InvalidateMatrix();
        }

        public void AsyncAPILotThumbnail(uint shardId, uint lotId, Action<Texture2D> callback)
        {
            Api.GetThumbnailAsync(shardId, lotId, (data) =>
            {
                if (data != null)
                {
                    GameThread.NextUpdate(x =>
                    {
                        if (UIScreen.Current != this) return;
                        using (var mem = new MemoryStream(data))
                        {
                            try
                            {
                                callback(ImageLoader.FromStream(GameFacade.GraphicsDevice, mem));
                            } catch
                            {

                            }
                        }
                    });
                }
            });
        }

        public Texture2D GetLotThumbnail(string shardName, uint lotId)
        {
            // TODO: accesses the resource action regulator

            var thumb = TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, GameFacade.GameFilePath("userdata/houses/defaulthouse.bmp"));
            TextureUtils.ManualTextureMask(ref thumb, new uint[] { 0xFF000000 });
            return thumb;
        }

        /// <summary>
        /// Device was reset, SceneManager called Content.Unload(), so reload everything.
        /// </summary>
        /// <param name="Device">The device.</param>
        public override void DeviceReset(GraphicsDevice Device)
        {
            PersonSlot.DeviceReset(Device);
            CalculateMatrix();
        }

        private void m_ExitButton_OnButtonClick(UIElement button)
        {
            UIScreen.ShowDialog(new UIExitDialog(), true);
        }

        private void CreditsButton_OnButtonClick(UIElement button)
        {
            /** Show the credits screen **/
            FSOFacade.Controller.ShowCredits();
        }

        public void ShowCitySelector(List<ShardStatusItem> shards, Callback<ShardStatusItem> onOk)
        {
            var cityPicker = new UICitySelector(shards);
            cityPicker.OkButton.OnButtonClick += (UIElement btn) =>
            {
                onOk(cityPicker.SelectedShard);
            };
            ShowDialog(cityPicker, true);
        }

        public void ShowSelectionError(ArchiveAvatarSelectCode code)
        {
            UIAlert alert = null;
            alert = GlobalShowAlert(new UIAlertOptions()
            {
                Title = GameFacade.Strings.GetString("f128", "100"),
                Message = GameFacade.Strings.GetString("f128", (100 + (int)code).ToString()) ,
                Buttons = UIAlertButton.Ok(x => {
                    RemoveDialog(alert);
                }),
            }, true);
        }
    }

    public class ArchivePersonSlot
    {
        public UIButton CityButton { get; set; }
        public UIButton AvatarButton { get; set; }
        public UIButton HouseButton { get; set; }
        public UIButton EnterTabButton { get; set; }
        public UIButton DescTabButton { get; set; }
        public UIButton NewAvatarButton { get; set; }
        public UIButton DeleteAvatarButton { get; set; }

        public UIImage TabBackground { get; set; }
        public UIImage TabEnterBackground { get; set; }
        public UIImage TabDescBackground { get; set; }
        public UIImage EnterTabBackgroundImage { get; set; }

        public UILabel PersonNameText { get; set; }
        public UILabel CityNameText { get; set; }
        public UILabel HouseNameText { get; set; }

        public UIButton PersonDescriptionScrollUpButton { get; set; }
        public UIButton PersonDescriptionScrollDownButton { get; set; }
        public UISlider PersonDescriptionSlider { get; set; }
        public UITextEdit PersonDescriptionText { get; set; }
        public UIImage DescriptionTabBackgroundImage { get; set; }

        private ArchivePersonSelection Screen { get; set; }
        public ArchiveAvatar? Avatar;
        private UIImage CityThumb { get; set; }
        private UIImage HouseThumb { get; set; }

        private UISim Sim;

        private PersonSlotTab _tab = PersonSlotTab.EnterTab;

        public ArchivePersonSlot(ArchivePersonSelection screen)
        {
            this.Screen = screen;
        }

        /// <summary>
        /// Setup UI events
        /// </summary>
        public void Init()
        {
            int offset = 9;
            CityButton.Y += offset;
            HouseButton.Y += offset;
            NewAvatarButton.Y += offset;
            DeleteAvatarButton.Y += offset;
            CityNameText.Y += offset;
            HouseNameText.Y += offset;

            PersonDescriptionText.Y += offset;
            PersonDescriptionSlider.Y += offset;
            PersonDescriptionScrollUpButton.Y += offset;
            PersonDescriptionScrollDownButton.Y += offset;

            EnterTabBackgroundImage.Y += offset;
            DescriptionTabBackgroundImage.Y += offset;

            /** Textures **/
            AvatarButton.Texture = Screen.SimCreateButtonImage;
            CityButton.Texture = Screen.CityButtonTemplateImage;
            HouseButton.Texture = Screen.HouseButtonTemplateImage;

            /** Send tab stuff to the bottom **/
            Screen.SendToBack(TabBackground, TabEnterBackground, TabDescBackground);

            /** Events **/
            EnterTabButton.OnButtonClick += new ButtonClickDelegate(EnterTabButton_OnButtonClick);
            DescTabButton.OnButtonClick += new ButtonClickDelegate(DescTabButton_OnButtonClick);

            NewAvatarButton.OnButtonClick += new ButtonClickDelegate(this.Screen.OpenCAS);
            DeleteAvatarButton.OnButtonClick += new ButtonClickDelegate(DeleteAvatarButton_OnButtonClick);

            PersonDescriptionSlider.AttachButtons(PersonDescriptionScrollUpButton, PersonDescriptionScrollDownButton, 1);
            PersonDescriptionText.AttachSlider(PersonDescriptionSlider);

            CityThumb = new UIImage
            {
                X = CityButton.X + 6,
                Y = CityButton.Y + 6
            };
            CityThumb.SetSize(78, 58);
            Screen.Add(CityThumb);


            HouseThumb = new UIImage
            {
                X = HouseButton.X + 6,
                Y = HouseButton.Y + 6
            };
            HouseThumb.SetSize(78, 58);
            Screen.Add(HouseThumb);

            Sim = new UISim();
            Sim.Visible = false;
            Sim.Position = AvatarButton.Position + new Vector2(1, 10);
            Sim.Size = new Vector2(140, 200);

            Screen.Add(Sim);
            SetTab(PersonSlotTab.EnterTab);

            AvatarButton.OnButtonClick += new ButtonClickDelegate(OnSelect);
            CityButton.OnButtonClick += new ButtonClickDelegate(OnSelect);
            HouseButton.OnButtonClick += new ButtonClickDelegate(OnSelect);
        }

        void OnSelect(UIElement button)
        {
            this.Screen.SelectAvatar(button, button == HouseButton);
        }

        private Texture2D DefaultHouseTex()
        {
            var thumb = TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, GameFacade.GameFilePath("userdata/houses/defaulthouse.bmp"));
            TextureUtils.ManualTextureMask(ref thumb, new uint[] { 0xFF000000 });

            return thumb;
        }

        /// <summary>
        /// User clicked the "Retire avatar" button.
        /// </summary>
        private void DeleteAvatarButton_OnButtonClick(UIElement button)
        {
            // TODO: deletion of avatars
            return;

            /**
            if (Avatar == null)
            {
                return;
            }

            UIAlertOptions AlertOptions = new UIAlertOptions();
            UIAlert alert = null;

            AlertOptions.Title = GameFacade.Strings.GetString("169", "9");
            AlertOptions.Message = GameFacade.Strings.GetString("169", "10");
            AlertOptions.Buttons = new UIAlertButton[] {
                new UIAlertButton(UIAlertButtonType.OK, (btn) => {
                    FSOFacade.Controller.RetireAvatar(Avatar.ShardName, Avatar.ID);
                }),
                new UIAlertButton(UIAlertButtonType.Cancel)
            };

            alert = UIScreen.GlobalShowAlert(AlertOptions, true);
            **/
        }

        public void DisplayAvatar(ArchiveAvatar avatar)
        {
            this.Avatar = avatar;

            SetSlotAvailable(false);

            PersonNameText.Caption = avatar.Name;
            //PersonDescriptionText.CurrentText = avatar.Description;
            AvatarButton.Texture = Screen.SimSelectButtonImage;


            var shard = Screen.FindController<ConnectArchiveController>().Shard;

            CityNameText.Caption = shard.ShardName;

            HouseNameText.Caption = avatar.LotName;
            HouseThumb.Texture?.Dispose();
            HouseThumb.Texture = null;

            var map = "0100"; // TODO: from archive

            var cityThumb = (int.Parse(map) >= 100) ?
                Path.Combine(FSOEnvironment.ContentDir, "Cities/city_" + map + "/thumbnail.png")
                : GameFacade.GameFilePath("cities/city_" + map + "/thumbnail.bmp");

            Texture2D cityThumbTex =
                TextureUtils.Resize(
                    GameFacade.GraphicsDevice,
                    TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, cityThumb),
                    78,
                    58);
            TextureUtils.CopyAlpha(ref cityThumbTex, Screen.CityHouseButtonAlpha);
            CityThumb.Texture = cityThumbTex;

            SetTab(_tab);

            Sim.Avatar.Appearance = (Vitaboy.AppearanceType)avatar.Type;
            Sim.Avatar.BodyOutfitId = avatar.Body;
            Sim.Avatar.HeadOutfitId = avatar.Head;

            Sim.Visible = true;

            PersonDescriptionText.CurrentText = "Loading...";

            AsyncFetchAvatarData(1); // TODO: shard ID
        }

        private int RequestNum = 0;

        private void AsyncFetchAvatarData(uint shardID)
        {
            var res = Screen.FindController<ArchiveCharactersSelectorController>().CityResource;
            var myNum = ++RequestNum;

            if (Avatar.Value.LotId != 0)
            {
                res.GetThumbnailAsync(shardID, Avatar.Value.LotId, (data) =>
                {
                    if (RequestNum != myNum)
                    {
                        return;
                    }

                    if (data == null)
                    {
                        HouseThumb.Texture = DefaultHouseTex();
                    }
                    else
                    {
                        try
                        {
                            Texture2D tex;

                            using (var mem = new MemoryStream(data))
                            {
                                tex = ImageLoader.FromStream(GameFacade.GraphicsDevice, mem);
                            }

                            HouseThumb.Texture = tex;
                        }
                        catch
                        {
                            HouseThumb.Texture = DefaultHouseTex();
                        }
                    }

                    HouseThumb.Y += HouseThumb.Size.Y / 2;
                    HouseThumb.SetSize(HouseThumb.Size.X, (int)(HouseThumb.Size.X * ((double)HouseThumb.Texture.Height / HouseThumb.Texture.Width)));
                    HouseThumb.Y -= HouseThumb.Size.Y / 2;
                });
            }

            res.GetAvatarDescriptionAsync(shardID, Avatar.Value.AvatarId, (data) =>
            {
                if (RequestNum != myNum)
                {
                    return;
                }

                PersonDescriptionText.CurrentText = data == null ? "" : Encoding.UTF8.GetString(data);
            });
        }

        public void SetSlotAvailable(bool isAvailable)
        {
            if (isAvailable)
            {
                this.Avatar = null;
            }

            EnterTabButton.Disabled = isAvailable;
            if (isAvailable) EnterTabButton.Selected = false;
            DescTabButton.Disabled = isAvailable;

            NewAvatarButton.Visible = isAvailable;
            DeleteAvatarButton.Visible = !isAvailable;

            if (isAvailable)
            {
                TabEnterBackground.Visible = false;
                TabDescBackground.Visible = false;
                TabBackground.Visible = false;
                CityButton.Visible = false;
                HouseButton.Visible = false;
                PersonDescriptionScrollUpButton.Visible = false;
                PersonDescriptionScrollDownButton.Visible = false;
                HouseNameText.Visible = false;
                CityNameText.Visible = false;
                DescriptionTabBackgroundImage.Visible = false;
                EnterTabBackgroundImage.Visible = false;
                PersonDescriptionSlider.Visible = false;
                PersonDescriptionText.Visible = false;

                Sim.Visible = false;
                HouseThumb.Visible = false;
                CityThumb.Visible = false;
                PersonNameText.Visible = false;

                AvatarButton.Texture = Screen.SimCreateButtonImage;
            }
            else
            {
                Sim.Visible = true;
                HouseThumb.Visible = true;
                CityThumb.Visible = true;
                PersonNameText.Visible = true;
                TabBackground.Visible = true;
            }
        }

        public void SetTab(PersonSlotTab tab)
        {
            _tab = tab;
            var isEnter = tab == PersonSlotTab.EnterTab;
            TabEnterBackground.Visible = isEnter;
            TabDescBackground.Visible = !isEnter;

            EnterTabButton.Selected = isEnter;
            DescTabButton.Selected = !isEnter;

            CityNameText.Visible = isEnter;
            CityButton.Visible = isEnter;
            EnterTabBackgroundImage.Visible = isEnter;
            CityThumb.Visible = isEnter;
            HouseThumb.Visible = isEnter;

            PersonDescriptionScrollUpButton.Visible = !isEnter;
            PersonDescriptionScrollDownButton.Visible = !isEnter;

            PersonDescriptionSlider.Visible = !isEnter;
            DeleteAvatarButton.Visible = !isEnter;
            PersonDescriptionText.Visible = !isEnter;
            DescriptionTabBackgroundImage.Visible = !isEnter;

            var hasLot = Avatar != null && Avatar.Value.LotId != 0;

            HouseNameText.Visible = isEnter && hasLot;
            HouseButton.Visible = isEnter && hasLot;
        }

        private void DescTabButton_OnButtonClick(UIElement button)
        {
            SetTab(PersonSlotTab.DescriptionTab);
        }

        private void EnterTabButton_OnButtonClick(UIElement button)
        {
            SetTab(PersonSlotTab.EnterTab);
        }

        public void DeviceReset(GraphicsDevice device){
            if (this.Avatar.HasValue)
            {
                DisplayAvatar(this.Avatar.Value);
            }
        }
    }
}
