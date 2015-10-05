/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;
using FSO.Client.Utils;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Framework.Parser;
using ProtocolAbstractionLibraryD;
using Microsoft.Xna.Framework;
using FSO.Content;
using FSO.Vitaboy;
using FSO.Files;
using FSO.Client.Network;
using FSO.Common.Utils;

namespace FSO.Client.UI.Screens
{
    public class PersonSelection : GameScreen
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
        private List<PersonSlot> m_PersonSlots { get; set; }
        private UIButton m_ExitButton;

        private List<UISim> m_UISims = new List<UISim>();

        public PersonSelection()
        {
            UIScript ui = null;
            if (GlobalSettings.Default.ScaleUI)
            {
                ui = this.RenderScript("personselection.uis");
                this.Scale800x600 = true;
            }
            else
            {
                ui = this.RenderScript("personselection" + (ScreenWidth == 1024 ? "1024" : "") + ".uis");
            }

            m_ExitButton = (UIButton)ui["ExitButton"];

            var numSlots = 3;
            m_PersonSlots = new List<PersonSlot>();

            for (var i = 0; i < numSlots; i++)
            {
                var index = (i + 1).ToString();

                /** Tab Background **/
                var tabBackground = ui.Create<UIImage>("TabBackgroundImage" + index);
                this.Add(tabBackground);

                var enterTabImage = ui.Create<UIImage>("EnterTabImage" + index);
                this.Add(enterTabImage);

                var descTabImage = ui.Create<UIImage>("DescriptionTabImage" + index);
                this.Add(descTabImage);

                var descTabBgImage = ui.Create<UIImage>("DescriptionTabBackgroundImage" + index);
                var enterIcons = ui.Create<UIImage>("EnterTabBackgroundImage" + index);

                var personSlot = new PersonSlot(this)
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
                m_PersonSlots.Add(personSlot);

                lock (NetworkFacade.Avatars)
                {
                    if (i < NetworkFacade.Avatars.Count)
                    {
                        personSlot.DisplayAvatar(NetworkFacade.Avatars[i]);
                        personSlot.AvatarButton.OnButtonClick += new ButtonClickDelegate(AvatarButton_OnButtonClick);

                        var SimBox = new UISim(NetworkFacade.Avatars[i].GUID);

                        SimBox.Avatar.Body = NetworkFacade.Avatars[i].Body;
                        SimBox.Avatar.Head = NetworkFacade.Avatars[i].Head;
                        SimBox.Avatar.Handgroup = NetworkFacade.Avatars[i].Body;
                        SimBox.Avatar.Appearance = NetworkFacade.Avatars[i].Avatar.Appearance;

                        SimBox.Position = m_PersonSlots[i].AvatarButton.Position + new Vector2(70, (m_PersonSlots[i].AvatarButton.Size.Y - 35));
                        SimBox.Size = m_PersonSlots[i].AvatarButton.Size;

                        SimBox.Name = NetworkFacade.Avatars[i].Name;

                        m_UISims.Add(SimBox);
                        this.Add(SimBox);
                    }
                }
            }

            this.AddAt(0, new UIImage(BackgroundImage));
            if (BackgroundImageDialog != null)
            {
                this.AddAt(1, new UIImage(BackgroundImageDialog)
                {
                    X = 112,
                    Y = 84
                });
            }

            /**
             * Button plumbing
             */
            CreditsButton.OnButtonClick += new ButtonClickDelegate(CreditsButton_OnButtonClick);
            m_ExitButton.OnButtonClick += new ButtonClickDelegate(m_ExitButton_OnButtonClick);

            /**
             * Music
             */
            var tracks = new string[]{
                GlobalSettings.Default.StartupPath + "\\music\\modes\\select\\tsosas1_v2.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\select\\tsosas2_v2.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\select\\tsosas3.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\select\\tsosas4.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\select\\tsosas5.mp3"
            };
            PlayBackgroundMusic(
                tracks
            );

            NetworkFacade.Controller.OnCityToken += new OnCityTokenDelegate(Controller_OnCityToken);
            NetworkFacade.Controller.OnPlayerAlreadyOnline += new OnPlayerAlreadyOnlineDelegate(Controller_OnPlayerAlreadyOnline);
            NetworkFacade.Controller.OnCharacterRetirement += new OnCharacterRetirementDelegate(Controller_OnCharacterRetirement);
        }

        /// <summary>
        /// Device was reset, SceneManager called Content.Unload(), so reload everything.
        /// </summary>
        /// <param name="Device">The device.</param>
        public override void DeviceReset(GraphicsDevice Device)
        {
            lock (NetworkFacade.Avatars)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (i < NetworkFacade.Avatars.Count)
                        m_PersonSlots[i].DisplayAvatar(NetworkFacade.Avatars[i]);
                }
            }

            CalculateMatrix();
        }

        /// <summary>
        /// Player wished to log into a city!
        /// </summary>
        /// <param name="button">The avatar button that was clicked.</param>
        public void AvatarButton_OnButtonClick(UIElement button)
        {
            PersonSlot PSlot = m_PersonSlots.First(x => x.AvatarButton.ID.Equals(button.ID, StringComparison.InvariantCultureIgnoreCase));
            UISim Avatar = NetworkFacade.Avatars.First(x => x.Name == PSlot.PersonNameText.Caption);
            //This is important, the avatar contains ResidingCity, which is neccessary to
            //continue to CityTransitionScreen.
            PlayerAccount.CurrentlyActiveSim = Avatar;

            UIPacketSenders.RequestCityToken(NetworkFacade.Client, Avatar);
        }

        #region Network handlers

        /// <summary>
        /// Received character retirement status from LoginServer.
        /// </summary>
        /// <param name="CharacterName">Name of character that was retired.</param>
        private void Controller_OnCharacterRetirement(string GUID)
        {
            foreach (PersonSlot Slot in m_PersonSlots)
            {
                if (new Guid(GUID).CompareTo(Slot.Avatar.GUID) == 0)
                {
                    Slot.SetSlotAvailable(true);
                    Slot.AvatarButton.OnButtonClick -= new ButtonClickDelegate(AvatarButton_OnButtonClick);
                    break;
                }
            }

            lock (NetworkFacade.Avatars)
            {
                for (int i = 0; i < NetworkFacade.Avatars.Count; i++)
                {
                    if (NetworkFacade.Avatars[i].GUID.CompareTo(new Guid(GUID)) == 0)
                    {
                        NetworkFacade.Avatars.Remove(NetworkFacade.Avatars[i]);
                        break;
                    }
                }
            }

            Cache.DeleteCache();

            //Removes actual sims from this screen.
            for (int i = 0; i < m_UISims.Count; i++)
            {
                if (m_UISims[i].Name.Equals(m_PersonSlots[i].PersonNameText.Caption, StringComparison.InvariantCultureIgnoreCase))
                {
                    this.Remove(m_UISims[i]);
                    m_UISims.Remove(m_UISims[i]);
                    break;
                }
            }
        }

        /// <summary>
        /// Received token from LoginServer - proceed to CityServer!
        /// </summary>
        private void Controller_OnCityToken(CityInfo SelectedCity)
        {
            GameFacade.Controller.ShowCityTransition(SelectedCity, false);
        }

        private void Controller_OnPlayerAlreadyOnline()
        {
            UIAlertOptions AlertOptions = new UIAlertOptions();
            //These should be imported as strings for localization.
            AlertOptions.Title = "Character Already Online";
            AlertOptions.Message = "You cannot play this character now, as it is already online.";

            UIScreen.ShowAlert(AlertOptions, false);
        }

        #endregion

        private void m_ExitButton_OnButtonClick(UIElement button)
        {
            UIScreen.ShowDialog(new UIExitDialog(), true);
        }

        private void CreditsButton_OnButtonClick(UIElement button)
        {
            /** Show the credits screen **/
            GameFacade.Controller.ShowCredits();
        }

        /// <summary>
        /// Called when create new avatar is requested
        /// </summary>
        public void CreateAvatar()
        {
            var cityPicker = new UICitySelector();
            UIScreen.ShowDialog(cityPicker, true);
        }
    }

    public class PersonSlot
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

        private PersonSelection Screen { get; set; }
        public UISim Avatar;
        private UIImage CityThumb { get; set; }

        //This is shown to ask the user if he wants to retire the char in this slot.
        private UIAlert RetireCharAlert;

        public PersonSlot(PersonSelection screen)
        {
            this.Screen = screen;
        }

        /// <summary>
        /// Setup UI events
        /// </summary>
        public void Init()
        {
            /** Textures **/
            AvatarButton.Texture = Screen.SimCreateButtonImage;
            CityButton.Texture = Screen.CityButtonTemplateImage;
            HouseButton.Texture = Screen.HouseButtonTemplateImage;

            /** Send tab stuff to the bottom **/
            Screen.SendToBack(TabBackground, TabEnterBackground, TabDescBackground);

            /** Events **/
            EnterTabButton.OnButtonClick += new ButtonClickDelegate(EnterTabButton_OnButtonClick);
            DescTabButton.OnButtonClick += new ButtonClickDelegate(DescTabButton_OnButtonClick);

            NewAvatarButton.OnButtonClick += new ButtonClickDelegate(NewAvatarButton_OnButtonClick);
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

            SetTab(PersonSlotTab.EnterTab);
        }

        /// <summary>
        /// User clicked the "Retire avatar" button.
        /// </summary>
        private void DeleteAvatarButton_OnButtonClick(UIElement button)
        {
            UIAlertOptions AlertOptions = new UIAlertOptions();
            //These should be imported as strings for localization.
            AlertOptions.Title = "Are you sure?";
            AlertOptions.Message = "Do you want to retire this Sim?";
            AlertOptions.Buttons = new UIAlertButton[] {
                new UIAlertButton(UIAlertButtonType.OK, new ButtonClickDelegate(PersonSlot_OnButtonClick)),
                new UIAlertButton(UIAlertButtonType.Cancel)
            };

            RetireCharAlert = UIScreen.ShowAlert(AlertOptions, true);
        }

        /// <summary>
        /// User confirmed character retirement.
        /// </summary>
        private void PersonSlot_OnButtonClick(UIElement button)
        {
            UIPacketSenders.SendCharacterRetirement(Avatar);
            UIScreen.RemoveDialog(RetireCharAlert);
        }

        /// <summary>
        /// Display an avatar
        /// </summary>
        /// <param name="avatar"></param>
        public void DisplayAvatar(UISim avatar)
        {
            this.Avatar = avatar;
            SetSlotAvailable(false);

            PersonNameText.Caption = avatar.Name;
            PersonDescriptionText.CurrentText = avatar.Description;
            AvatarButton.Texture = Screen.SimSelectButtonImage;

            CityNameText.Caption = avatar.ResidingCity.Name;

            String gamepath = GameFacade.GameFilePath("");
            int CityNum = GameFacade.GetCityNumber(avatar.ResidingCity.Name);
            string CityStr = gamepath + "cities\\" + ((CityNum >= 10) ? "city_00" + CityNum.ToString() : "city_000" + CityNum.ToString());

            var stream = new FileStream(CityStr + "\\Thumbnail.bmp", FileMode.Open, FileAccess.Read, FileShare.Read);

            Texture2D cityThumbTex = TextureUtils.Resize(GameFacade.GraphicsDevice, ImageLoader.FromStream(
                GameFacade.Game.GraphicsDevice, stream), 78, 58);

            stream.Close();

            TextureUtils.CopyAlpha(ref cityThumbTex, Screen.CityHouseButtonAlpha);
            CityThumb.Texture = cityThumbTex;

            SetTab(PersonSlotTab.EnterTab);
        }

        /// <summary>
        /// Player wanted to create a sim.
        /// </summary>
        private void NewAvatarButton_OnButtonClick(UIElement button)
        {
            Screen.CreateAvatar();
        }

        public void SetSlotAvailable(bool isAvailable)
        {
            EnterTabButton.Disabled = isAvailable;
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
            }
            else
                TabBackground.Visible = true;
        }

        public void SetTab(PersonSlotTab tab)
        {
            var isEnter = tab == PersonSlotTab.EnterTab;
            TabEnterBackground.Visible = isEnter;
            TabDescBackground.Visible = !isEnter;

            EnterTabButton.Selected = isEnter;
            DescTabButton.Selected = !isEnter;

            CityNameText.Visible = isEnter;
            CityButton.Visible = isEnter;
            EnterTabBackgroundImage.Visible = isEnter;
            CityThumb.Visible = isEnter;

            PersonDescriptionScrollUpButton.Visible = !isEnter;
            PersonDescriptionScrollDownButton.Visible = !isEnter;

            PersonDescriptionSlider.Visible = !isEnter;
            DeleteAvatarButton.Visible = !isEnter;
            PersonDescriptionText.Visible = !isEnter;
            DescriptionTabBackgroundImage.Visible = !isEnter;
        }

        private void DescTabButton_OnButtonClick(UIElement button)
        {
            SetTab(PersonSlotTab.DescriptionTab);
        }

        private void EnterTabButton_OnButtonClick(UIElement button)
        {
            SetTab(PersonSlotTab.EnterTab);
        }
    }

    public enum PersonSlotTab
    {
        EnterTab,
        DescriptionTab
    }
}
