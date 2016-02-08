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
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Screens;
using FSO.Client.Utils;
using FSO.Client.UI.Framework;
using FSO.Client.Network;
using ProtocolAbstractionLibraryD;
using FSO.Files;
using FSO.Common.Utils;

namespace FSO.Client.UI.Panels
{
    public class UICitySelector : UIDialog
    {
        //Positioned & sized by UIScript
        public UIImage CityListBoxBackground { get; set; }
        public UIImage CityDescriptionBackground { get; set; }

        //Set by UIScript
        public Texture2D CityIconImage { get; set; }
        public Texture2D thumbnailBackgroundImage { get; set; }
        public Texture2D thumbnailAlphaImage { get; set; }
        public UIListBox CityListBox { get; set; }
        public UITextEdit DescriptionText { get; set; }
        public UISlider CityDescriptionSlider { get; set; }
        public UIButton CityDescriptionScrollUpButton { get; set; }
        public UIButton CityDescriptionDownButton { get; set; }

        public UIButton OkButton { get; set; }
        public UIButton CancelButton { get; set; }

        /** Strings **/
        public string OnlineStatusUp { get; set; }
        public string OnlineStatusDown { get; set; }
        public string StatusBusy { get; set; }
        public string StatusFull { get; set; }
        public string StatusBusyFull { get; set; }
        public string StatusOk { get; set; }

        public string CityReservedDialogTitle { get; set; }
        public string CityReservedDialogMessage { get; set; }
        public string CityFullDialogTitle { get; set; }
        public string CityFullDialogMessage { get; set; }
        public string CityBusyDialogTitle { get; set; }
        public string CityBusyDialogMessage { get; set; }

        //Internal
        private UIImage CityThumb { get; set; }
        private UIListBoxTextStyle listStyleNormal;
        private UIListBoxTextStyle listStyleBusy;
        private UIListBoxTextStyle listStyleFull;
        private UIListBoxTextStyle listStyleReserved;

        public UICitySelector()
            : base(UIDialogStyle.Standard, true)
        {
            this.Opacity = 0.9f;


            CityListBoxBackground = new UIImage(UITextBox.StandardBackground);
            this.Add(CityListBoxBackground);
            CityDescriptionBackground = new UIImage(UITextBox.StandardBackground);
            this.Add(CityDescriptionBackground);

            var script = this.RenderScript("cityselector.uis");
            this.DialogSize = (Point)script.GetControlProperty("DialogSize");

            var cityThumbBG = new UIImage(thumbnailBackgroundImage);
            cityThumbBG.Position = (Vector2)script.GetControlProperty("CityThumbnailBackgroundPosition");
            this.Add(cityThumbBG);
            CityThumb = new UIImage();
            CityThumb.Position = (Vector2)script.GetControlProperty("CityThumbnailPosition");
            this.Add(CityThumb);

            CityDescriptionSlider.AttachButtons(CityDescriptionScrollUpButton, CityDescriptionDownButton, 1);
            DescriptionText.AttachSlider(CityDescriptionSlider);

            OkButton.Disabled = true;
            OkButton.OnButtonClick += new ButtonClickDelegate(OkButton_OnButtonClick);
            CancelButton.OnButtonClick += new ButtonClickDelegate(CancelButton_OnButtonClick);

            this.Caption = (string)script["TitleString"];


            /** Parse the list styles **/
            listStyleNormal = script.Create<UIListBoxTextStyle>("CityListBoxColors", CityListBox.FontStyle);
            listStyleBusy = script.Create<UIListBoxTextStyle>("CityListBoxColorsBusy", CityListBox.FontStyle);
            listStyleFull = script.Create<UIListBoxTextStyle>("CityListBoxColorsFull", CityListBox.FontStyle);
            listStyleReserved = script.Create<UIListBoxTextStyle>("CityListBoxColorsReserved", CityListBox.FontStyle);

            UpdateItems();

            CityListBox.OnChange += new ChangeDelegate(CityListBox_OnChange);
            NetworkFacade.Controller.OnNewCityServer += new OnNewCityServerDelegate(Controller_OnNewCityServer);
            NetworkFacade.Controller.OnCityServerOffline += new OnCityServerOfflineDelegate(Controller_OnCityServerOffline);
        }

        /// <summary>
        /// Updates CityListBox.Items.
        /// </summary>
        private void UpdateItems()
        {
            var statusToStyle = new Dictionary<CityInfoStatus, UIListBoxTextStyle>();
            statusToStyle.Add(CityInfoStatus.Ok, listStyleNormal);
            statusToStyle.Add(CityInfoStatus.Busy, listStyleBusy);
            statusToStyle.Add(CityInfoStatus.Full, listStyleFull);
            statusToStyle.Add(CityInfoStatus.Reserved, listStyleReserved);

            var statusToLabel = new Dictionary<CityInfoStatus, string>();
            statusToLabel.Add(CityInfoStatus.Ok, StatusOk);
            statusToLabel.Add(CityInfoStatus.Busy, StatusBusy);
            statusToLabel.Add(CityInfoStatus.Full, StatusFull);
            statusToLabel.Add(CityInfoStatus.Reserved, StatusOk);

            lock (CityListBox.Items)
            {
                CityListBox.Items =
                    NetworkFacade.Cities.Select(
                        x => new UIListBoxItem(x, CityIconImage, x.Name, x.Online ? OnlineStatusUp : OnlineStatusDown, statusToLabel[x.Status])
                        {
                            CustomStyle = statusToStyle[x.Status]
                        }
                    ).ToList();
            }
        }

        /// <summary>
        /// New city server came online!
        /// </summary>
        private void Controller_OnNewCityServer()
        {
            UpdateItems();
        }

        /// <summary>
        /// City server went offline!
        /// </summary>
        private void Controller_OnCityServerOffline()
        {
            UpdateItems();
        }

        private void CancelButton_OnButtonClick(UIElement button)
        {
            UIScreen.RemoveDialog(this);
        }

        private void OkButton_OnButtonClick(UIElement button)
        {
            GameFacade.Controller.ShowPersonCreation((CityInfo)CityListBox.SelectedItem.Data);
        }

        private void ShowCityErrorDialog(string title, string body)
        {
            var alert = UIScreen.ShowAlert(new UIAlertOptions { Title = title, Message = body }, true);
            alert.CenterAround(CityListBoxBackground);
        }

        /// <summary>
        /// Handle when a user selects a city
        /// </summary>
        /// <param name="element"></param>
        private void CityListBox_OnChange(UIElement element)
        {
            var selectedItem = CityListBox.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            var city = (CityInfo)selectedItem.Data;

            String gamepath = GameFacade.GameFilePath("");
            int CityNum = GameFacade.GetCityNumber(city.Name);
            string CityStr = gamepath + "cities/" + ((CityNum >= 10) ? "city_00" + CityNum.ToString() : "city_000" + CityNum.ToString());

            //Take a copy so we dont change the original when we alpha mask it
            var stream = new FileStream(CityStr + "/Thumbnail.bmp", FileMode.Open, FileAccess.Read, FileShare.Read);

            Texture2D cityThumbTex = TextureUtils.Copy(GameFacade.GraphicsDevice, ImageLoader.FromStream(
               GameFacade.Game.GraphicsDevice, stream));
            TextureUtils.CopyAlpha(ref cityThumbTex, thumbnailAlphaImage);

            stream.Close();
            
            CityThumb.Texture = cityThumbTex;
            DescriptionText.CurrentText = city.Description;
            DescriptionText.VerticalScrollPosition = 0;

            /** Validate **/
            var isValid = true;
            if (city.Status == CityInfoStatus.Reserved)
            {
                isValid = false;
                /** Already have a sim in this city **/
                ShowCityErrorDialog(CityReservedDialogTitle, CityReservedDialogMessage);
            }
            else if (city.Status == CityInfoStatus.Full)
            {
                isValid = false;
                /** City is full **/
                ShowCityErrorDialog(CityFullDialogTitle, CityFullDialogMessage);
            }
            else if (city.Status == CityInfoStatus.Busy)
            {
                isValid = false;
                /** City is busy **/
                ShowCityErrorDialog(CityBusyDialogTitle, CityBusyDialogMessage);
            }

            OkButton.Disabled = !isValid;
        }
    }
}
