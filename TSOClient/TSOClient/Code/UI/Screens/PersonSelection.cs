using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Controls;
using TSOClient.LUI;
using TSOClient.Code.Utils;
using TSOClient.Code.Network;
using TSOServiceClient.Model;
using TSOClient.Code.UI.Panels;
using TSOClient.Code.UI.Framework.Parser;
using TSOClient.Code.Data;
using TSOClient.ThreeD.Controls;
using TSOClient.VM;
using TSOClient.Code.Data.Model;
using Microsoft.Xna.Framework;
using SimsLib.ThreeD;

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
        public Texture2D CityHouseButtonAlpha { get; set; }

        public UIButton CreditsButton { get; set; }
        private List<PersonSlot> PersonSlots { get; set; }


        public PersonSelection()
        {
            //var ui = this.RenderScript("personselection" + (ScreenWidth == 1024 ? "1024" : "") + ".uis");
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
                personSlot.SetSlotAvaliable(true);
                PersonSlots.Add(personSlot);

                if (i < NetworkFacade.Avatars.Count)
                {
                    personSlot.DisplayAvatar(NetworkFacade.Avatars[i]);
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


            /**
             * Music
             */
            var tracks = new string[]{
                //"music\\modes\\select\\tsosas1_v2.mp3",
                "music\\modes\\select\\tsosas2_v2.mp3",
                //"music\\modes\\select\\tsosas3.mp3",
                //"music\\modes\\select\\tsosas4.mp3",
                //"music\\modes\\select\\tsosas5.mp3"
            };
            PlayBackgroundMusic(
                GameFacade.GameFilePath(tracks.RandomItem())
            );



            var simBox = new UISim();
            var sim = new Sim(Guid.NewGuid().ToString());
            var maleHeads = new Collection(ContentManager.GetResourceFromLongID((ulong)FileIDs.CollectionsFileIDs.ea_male_heads));
            //SimCatalog.LoadSim3D(sim, maleHeads.First().PurchasableObject.Outfit, AppearanceType.Light);
            //
            SimCatalog.LoadSim3D(sim, SimCatalog.GetOutfit(4462471020557), AppearanceType.Light);

            simBox.Sim = sim;
            simBox.Position = PersonSlots[0].AvatarButton.Position + new Vector2(70, 40);
            simBox.Size = PersonSlots[0].AvatarButton.Size;

            this.Add(simBox);



            //var gizmo = new UIGizmo();
            //gizmo.X = ScreenWidth - 500;
            //gizmo.Y = ScreenHeight - 300;
            //this.Add(gizmo);

        }


        void CreditsButton_OnButtonClick(UIElement button)
        {
            /** Show the credits screen **/
            GameFacade.Screens.AddScreen(new Credits());
        }



        /// <summary>
        /// Called when create new avatar is requested
        /// </summary>
        public void CreateAvatar()
        {
            var cityPicker = new UICitySelector();
            //if (GlobalSettings.Default.ScaleUI)
            //{
            //    cityPicker.ScaleX = cityPicker.ScaleY = this.ScaleX;
            //}
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
        private AvatarInfo Avatar;
        private UIImage CityThumb { get; set; }

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

            PersonDescriptionSlider.AttachButtons(PersonDescriptionScrollUpButton, PersonDescriptionScrollDownButton, 1);
            PersonDescriptionText.AttachSlider(PersonDescriptionSlider);


            CityThumb = new UIImage {
                X = CityButton.X + 6,
                Y = CityButton.Y + 6
            };
            CityThumb.SetSize(78, 58);
            Screen.Add(CityThumb);



            SetTab(PersonSlotTab.EnterTab);
        }

        

        /// <summary>
        /// Display an avatar
        /// </summary>
        /// <param name="avatar"></param>
        public void DisplayAvatar(AvatarInfo avatar)
        {
            this.Avatar = avatar;
            SetSlotAvaliable(false);

            PersonNameText.Caption = avatar.Name;
            PersonDescriptionText.CurrentText = avatar.Description;
            AvatarButton.Texture = Screen.SimSelectButtonImage;

            var myCity = NetworkFacade.Cities.First(x => x.ID == avatar.CityId);
            CityNameText.Caption = myCity.Name;

            var cityThumbTex = TextureUtils.Resize(GameFacade.GraphicsDevice, myCity.GetThumbnail(), 78, 58);
            TextureUtils.CopyAlpha(ref cityThumbTex, Screen.CityHouseButtonAlpha);
            CityThumb.Texture = cityThumbTex;

            SetTab(PersonSlotTab.EnterTab);
        }


        void NewAvatarButton_OnButtonClick(UIElement button)
        {
            Screen.CreateAvatar();
            //GameFacade.Controller.ShowPersonCreation();
        }

        
        public void SetSlotAvaliable(bool isAvaliable)
        {
            EnterTabButton.Disabled = isAvaliable;
            DescTabButton.Disabled = isAvaliable;

            NewAvatarButton.Visible = isAvaliable;
            DeleteAvatarButton.Visible = !isAvaliable;

            if (isAvaliable)
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
            }
            else
            {
                TabBackground.Visible = true;
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

            CityNameText.Visible = isEnter;
            CityButton.Visible = isEnter;
            EnterTabBackgroundImage.Visible = isEnter;
            CityThumb.Visible = isEnter;
            //HouseButton.Visible = isEnter;

            PersonDescriptionScrollUpButton.Visible = !isEnter;
            PersonDescriptionScrollDownButton.Visible = !isEnter;

            PersonDescriptionSlider.Visible = !isEnter;
            DeleteAvatarButton.Visible = !isEnter;
            PersonDescriptionText.Visible = !isEnter;
            DescriptionTabBackgroundImage.Visible = !isEnter;
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







    public static class CityInfoExtensions
    {
        private static Dictionary<string, Texture2D> CityThumbs = new Dictionary<string, Texture2D>();

        public static Texture2D GetThumbnail(this CityInfo city){
            if (CityThumbs.ContainsKey(city.Map))
            {
                return CityThumbs[city.Map];
            }

            //city_0002
            var mapDir = "city_" + String.Format("{0:0000}", int.Parse(city.Map));

            var texture = Texture2D.FromFile(GameFacade.GraphicsDevice, GameFacade.GameFilePath("cities\\" + mapDir + "\\thumbnail.bmp"));
            CityThumbs[city.Map] = texture;
            return texture;
        }
    }
}
