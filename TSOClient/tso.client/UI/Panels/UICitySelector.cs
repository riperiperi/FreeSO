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
using FSO.Files;
using FSO.Common.Utils;
using FSO.Server.Protocol.CitySelector;
using FSO.Common;

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
        public UISlider CityListSlider { get; set; }
        public UIButton CityListScrollUpButton { get; set; }
        public UIButton CityScrollDownButton { get; set; }

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

        public UICitySelector(List<ShardStatusItem> shards)
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
            var listStyleNormal = script.Create<UIListBoxTextStyle>("CityListBoxColors", CityListBox.FontStyle);
            var listStyleBusy = script.Create<UIListBoxTextStyle>("CityListBoxColorsBusy", CityListBox.FontStyle);
            var listStyleFull = script.Create<UIListBoxTextStyle>("CityListBoxColorsFull", CityListBox.FontStyle);
            var listStyleReserved = script.Create<UIListBoxTextStyle>("CityListBoxColorsReserved", CityListBox.FontStyle);

            var statusToStyle = new Dictionary<ShardStatus, UIListBoxTextStyle>();
            statusToStyle.Add(ShardStatus.Up, listStyleNormal);
            statusToStyle.Add(ShardStatus.Busy, listStyleBusy);
            statusToStyle.Add(ShardStatus.Full, listStyleFull);
            statusToStyle.Add(ShardStatus.Down, listStyleFull);
            statusToStyle.Add(ShardStatus.Closed, listStyleFull);
            statusToStyle.Add(ShardStatus.Frontier, listStyleReserved);

            var statusToLabel = new Dictionary<ShardStatus, string>();
            statusToLabel.Add(ShardStatus.Up, StatusOk);
            statusToLabel.Add(ShardStatus.Busy, StatusBusy);
            statusToLabel.Add(ShardStatus.Full, StatusFull);
            statusToLabel.Add(ShardStatus.Down, StatusFull);
            statusToLabel.Add(ShardStatus.Closed, StatusFull);
            statusToLabel.Add(ShardStatus.Frontier, StatusOk);


            CityListSlider.AttachButtons(CityListScrollUpButton, CityScrollDownButton, 1);

            CityListBox.TextStyle = listStyleNormal;
            CityListBox.AttachSlider(CityListSlider);
            CityListBox.OnChange += new ChangeDelegate(CityListBox_OnChange);


            CityListBox.Items = shards.Select(x => new UIListBoxItem(x, CityIconImage, x.Name, x.Status == ShardStatus.Up ? OnlineStatusUp : OnlineStatusDown, statusToLabel[x.Status])
            {
                CustomStyle = statusToStyle[x.Status]
            }).ToList();

            if (shards.Count > 0){
                CityListBox.SelectedIndex = 0;
            }
        }


        void CancelButton_OnButtonClick(UIElement button)
        {
            UIScreen.RemoveDialog(this);
        }

        void OkButton_OnButtonClick(UIElement button)
        {
        }

        public ShardStatusItem SelectedShard
        {
            get
            {
                if (CityListBox.SelectedItem != null)
                {
                    return (ShardStatusItem)CityListBox.SelectedItem.Data;
                }
                return null;
            }
        }

        void ShowCityErrorDialog(string title, string body)
        {
            var alert = UIScreen.GlobalShowAlert(new UIAlertOptions { Title = title, Message = body }, true);
            alert.CenterAround(CityListBoxBackground);
        }

        /// <summary>
        /// Handle when a user selects a city
        /// </summary>
        /// <param name="element"></param>
        void CityListBox_OnChange(UIElement element)
        {
            var selectedItem = CityListBox.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            var city = (ShardStatusItem)selectedItem.Data;

            String gamepath = GameFacade.GameFilePath("");


            var fsoMap = int.Parse(city.Map) >= 100;

            var cityThumb = (fsoMap) ?
            Path.Combine(FSOEnvironment.ContentDir, "Cities/city_" + city.Map + "/thumbnail.png")
            : GameFacade.GameFilePath("cities/city_" + city.Map + "/thumbnail.bmp");

            //Take a copy so we dont change the original when we alpha mask it
            Texture2D cityThumbTex = TextureUtils.Copy(GameFacade.GraphicsDevice, TextureUtils.TextureFromFile(
               GameFacade.GraphicsDevice, cityThumb));
            TextureUtils.CopyAlpha(ref cityThumbTex, thumbnailAlphaImage);

            CityThumb.Texture = cityThumbTex;
            DescriptionText.CurrentText = GameFacade.Strings.GetString(fsoMap?"f104":"238", int.Parse(city.Map).ToString());
            DescriptionText.VerticalScrollPosition = 0;

            /** Validate **/
            var isValid = true;
            if (city.Status == ShardStatus.Frontier)
            {
                isValid = false;
                /** Already have a sim in this city **/
                ShowCityErrorDialog(CityReservedDialogTitle, CityReservedDialogMessage);
            }
            else if (city.Status == ShardStatus.Full)
            {
                isValid = false;
                /** City is full **/
                ShowCityErrorDialog(CityFullDialogTitle, CityFullDialogMessage);
            }
            else if (city.Status == ShardStatus.Busy)
            {
                isValid = false;
                /** City is busy **/
                ShowCityErrorDialog(CityBusyDialogTitle, CityBusyDialogMessage);
            }

            OkButton.Disabled = !isValid;
        }
    }
}
