/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSOClient.Code.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Screens;
using TSOClient.Code.Utils;
using TSOClient.LUI;
using TSOClient.Code.UI.Framework;
using TSOClient.Network;
using tso.common.utils;

namespace TSOClient.Code.UI.Panels
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


        public UICitySelector() : base(UIDialogStyle.Standard, true)
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


            CityListBox.TextStyle = listStyleNormal;
            CityListBox.Items =
                NetworkFacade.Cities.Select(
                    x => new UIListBoxItem(x, CityIconImage, x.Name, x.Online ? OnlineStatusUp : OnlineStatusDown, statusToLabel[x.Status])
                    {
                        //Disabled = x.Status != TSOServiceClient.Model.CityInfoStatus.Ok,
                        CustomStyle = statusToStyle[x.Status]
                    }
                ).ToList();

            CityListBox.OnChange += new ChangeDelegate(CityListBox_OnChange);
            //CityListBox.SelectedIndex = 0;
        }


        void CancelButton_OnButtonClick(TSOClient.Code.UI.Framework.UIElement button)
        {
            UIScreen.RemoveDialog(this);
        }

        void OkButton_OnButtonClick(TSOClient.Code.UI.Framework.UIElement button)
        {
            GameFacade.Controller.ShowPersonCreation((CityInfo)CityListBox.SelectedItem.Data);
        }

        void ShowCityErrorDialog(string title, string body)
        {
            var alert = UIScreen.ShowAlert(new UIAlertOptions { Title = title, Message = body }, true);
            alert.CenterAround(CityListBoxBackground);
        }

        /// <summary>
        /// Handle when a user selects a city
        /// </summary>
        /// <param name="element"></param>
        void CityListBox_OnChange(TSOClient.Code.UI.Framework.UIElement element)
        {
            var selectedItem = CityListBox.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            var city = (CityInfo)selectedItem.Data;
            //Take a copy so we dont change the original when we alpha mask it
            var cityThumb = TextureUtils.Copy(GameFacade.GraphicsDevice, Texture2D.FromFile(GameFacade.GraphicsDevice, new MemoryStream(ContentManager.GetResourceFromLongID(city.Thumbnail)))); 
            TextureUtils.CopyAlpha(ref cityThumb, thumbnailAlphaImage);

            CityThumb.Texture = cityThumb;
            DescriptionText.CurrentText = city.Description;
            DescriptionText.VerticalScrollPosition = 0;

            /** Validate **/
            var isValid = true;
            if (city.Status == CityInfoStatus.Reserved)
            {
                isValid = false;
                /** Already have a sim in this city **/
                ShowCityErrorDialog(CityReservedDialogTitle, CityReservedDialogMessage);
            }else if (city.Status == CityInfoStatus.Full)
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
