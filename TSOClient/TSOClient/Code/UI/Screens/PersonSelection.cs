using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Controls;
using TSOClient.LUI;
using TSOClient.Code.Utils;

namespace TSOClient.Code.UI.Screens
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

        public UIButton CreditsButton { get; set; }

        private List<PersonSlot> PersonSlots { get; set; }


        public PersonSelection()
        {
            var ui = this.RenderScript("personselection" + (ScreenWidth == 1024 ? "1024" : "") + ".uis");

            var numSlots = 3;
            PersonSlots = new List<PersonSlot>();

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
                    CityNameText = (UILabel)ui["CityNameText" + index],
                    HouseNameText = (UILabel)ui["HouseNameText" + index],

                    TabBackground = tabBackground,
                    TabEnterBackground = enterTabImage,
                    TabDescBackground = descTabImage
                };
                personSlot.Init();
                personSlot.SetSlotAvaliable(true);
                PersonSlots.Add(personSlot);
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


            /**
             * Music
             */
            var tracks = new string[]{
                "music\\modes\\select\\tsosas1_v2.mp3",
                "music\\modes\\select\\tsosas2_v2.mp3",
                "music\\modes\\select\\tsosas3.mp3",
                "music\\modes\\select\\tsosas4.mp3",
                "music\\modes\\select\\tsosas5.mp3"
            };
            GameFacade.SoundManager.PlayBackgroundMusic(
                GameFacade.GameFilePath(tracks.RandomItem())
            );
        }


        void CreditsButton_OnButtonClick(UIElement button)
        {
            /** Show the credits screen **/
            GameFacade.Screens.AddScreen(new Credits());
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


        public UILabel PersonNameText { get; set; }
        public UILabel CityNameText { get; set; }
        public UILabel HouseNameText { get; set; }

        public UIButton PersonDescriptionScrollUpButton { get; set; }
        public UIButton PersonDescriptionScrollDownButton { get; set; }


        private PersonSelection Screen { get; set; }

        public PersonSlot(PersonSelection screen)
        {
            this.Screen = screen;
        }


        /// <summary>
        /// Setup UI events
        /// </summary>
        public void Init()
        {
            SetTab(PersonSlotTab.EnterTab);

            /** Textures **/
            AvatarButton.Texture = Screen.SimCreateButtonImage;
            CityButton.Texture = Screen.CityButtonTemplateImage;
            HouseButton.Texture = Screen.HouseButtonTemplateImage;

            /** Send tab stuff to the bottom **/
            Screen.SendToBack(TabBackground, TabEnterBackground, TabDescBackground);

            /** Events **/
            EnterTabButton.OnButtonClick += new ButtonClickDelegate(EnterTabButton_OnButtonClick);
            DescTabButton.OnButtonClick += new ButtonClickDelegate(DescTabButton_OnButtonClick);
        }

        
        public void SetSlotAvaliable(bool isAvaliable)
        {
            EnterTabButton.Disabled = isAvaliable;
            DescTabButton.Disabled = isAvaliable;

            NewAvatarButton.Visible = isAvaliable;
            DeleteAvatarButton.Visible = !isAvaliable;

            if(isAvaliable){
                TabEnterBackground.Visible = false;
                TabDescBackground.Visible = false;
                TabBackground.Visible = false;
                CityButton.Visible = false;
                HouseButton.Visible = false;
                PersonDescriptionScrollUpButton.Visible = false;
                PersonDescriptionScrollDownButton.Visible = false;
                HouseNameText.Visible = false;
                CityNameText.Visible = false;
            }
        }

        public void SetTab(PersonSlotTab tab)
        {
            var isEnter = tab == PersonSlotTab.EnterTab;
            TabEnterBackground.Visible = isEnter;
            TabDescBackground.Visible = !isEnter;
            //TabBackground.Visible = isEnter;

            EnterTabButton.Selected = isEnter;
            DescTabButton.Selected = !isEnter;

            CityButton.Visible = isEnter;
            HouseButton.Visible = isEnter;

            PersonDescriptionScrollUpButton.Visible = !isEnter;
            PersonDescriptionScrollDownButton.Visible = !isEnter;
        }





        void DescTabButton_OnButtonClick(UIElement button)
        {
            SetTab(PersonSlotTab.DescriptionTab);
        }

        void EnterTabButton_OnButtonClick(UIElement button)
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
