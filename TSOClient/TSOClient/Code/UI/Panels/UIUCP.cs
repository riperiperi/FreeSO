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
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.LUI;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Screens;
using TSOClient.Code.Rendering.City;
using TSO.Simantics;
using TSO.Simantics.model;

namespace TSOClient.Code.UI.Panels
{
    /// <summary>
    /// UCP
    /// </summary>
    public class UIUCP : UIContainer
    {
        public Terrain CityRenderer; //We should probably use an overall controller to handle the game state.

        public TSO.Simantics.VM vm;
        private VMAvatar m_SelectedAvatar;
        public VMAvatar SelectedAvatar
        {
            set
            {
                m_SelectedAvatar = value;
                if (CurrentPanel == 1) ((UILiveMode)Panel).SelectedAvatar = value;
            }
            get
            {
                return m_SelectedAvatar;
            }
        }

        /// <summary>
        /// Variables which get wired up by the UIScript
        /// </summary>
        public Texture2D BackgroundGameImage { get; set; }
        public Texture2D BackgroundMatchmakerImage { get; set; }
        public UIButton PhoneButton { get; set; }

        /// <summary>
        /// Mode buttons
        /// </summary>
        public UIButton LiveModeButton { get; set; }
        public UIButton BuyModeButton { get; set; }
        public UIButton BuildModeButton { get; set; }
        public UIButton HouseModeButton { get; set; }
        public UIButton OptionsModeButton { get; set; }

        /// <summary>
        /// House view buttons
        /// </summary>
        public UIButton FirstFloorButton { get; set; }
        public UIButton SecondFloorButton { get; set; }
        public UIButton WallsDownButton { get; set; }
        public UIButton WallsCutawayButton { get; set; }
        public UIButton WallsUpButton { get; set; }
        public UIButton RoofButton { get; set; }
        public UIButton HouseViewSelectButton { get; set; }

        /// <summary>
        /// Zoom Control buttons
        /// </summary>
        public UIButton CloseZoomButton { get; set; }
        public UIButton MediumZoomButton { get; set; }
        public UIButton FarZoomButton { get; set; }
        public UIButton NeighborhoodButton { get; set; }
        public UIButton WorldButton { get; set; }
        public UIButton ZoomInButton { get; set; }
        public UIButton ZoomOutButton { get; set; }

        /// <summary>
        /// Rotate Control buttons
        /// </summary>
        public UIButton RotateClockwiseButton { get; set; }
        public UIButton RotateCounterClockwiseButton { get; set; }

        /// <summary>
        /// Backgrounds
        /// </summary>
        private UIImage BackgroundMatchmaker;
        private UIImage Background;

        /// <summary>
        /// Labels
        /// </summary>
        public UILabel TimeText { get; set; }
        public UILabel MoneyText { get; set; }

        private UIContainer Panel;
        private int CurrentPanel;

        public UIUCP()
        {
            this.RenderScript("ucp.uis");

            Background = new UIImage(BackgroundGameImage);
            this.AddAt(0, Background);
            Background.BlockInput();


            BackgroundMatchmaker = new UIImage(BackgroundMatchmakerImage);
            BackgroundMatchmaker.Y = 81;
            this.AddAt(0, BackgroundMatchmaker);
            BackgroundMatchmaker.BlockInput();

            TimeText.Caption = "12:00 am";
            MoneyText.Caption = "§0";

            CurrentPanel = -1;

            OptionsModeButton.OnButtonClick += new ButtonClickDelegate(OptionsModeButton_OnButtonClick);
            LiveModeButton.OnButtonClick += new ButtonClickDelegate(LiveModeButton_OnButtonClick);

            ZoomOutButton.OnButtonClick += new ButtonClickDelegate(ZoomControl);
            ZoomInButton.OnButtonClick += new ButtonClickDelegate(ZoomControl);
            NeighborhoodButton.OnButtonClick += new ButtonClickDelegate(SetCityZoom);
            WorldButton.OnButtonClick += new ButtonClickDelegate(SetCityZoom);
            PhoneButton.OnButtonClick += new ButtonClickDelegate(PhoneButton_OnButtonClick);

            SetInLot(false);
            SetMode(UCPMode.CityMode);
        }

        void LiveModeButton_OnButtonClick(UIElement button)
        {
            SetPanel(1);
        }

        void PhoneButton_OnButtonClick(UIElement button)
        {
            var screen = (CoreGameScreen)GameFacade.Screens.CurrentUIScreen;
            screen.OpenInbox();
        }

        private void ZoomControl(UIElement button)
        {
            if (button == ZoomInButton) CityRenderer.m_Zoomed = true;
            if (button == ZoomOutButton) CityRenderer.m_Zoomed = false;
            UpdateZoomButton();
            //this is definitely how this is not meant to work, but we'll fix it up when gameplay includes the city view and simulation view working together.
        }

        private void SetCityZoom(UIElement button)
        {
            if (CityRenderer != null)
            {
                if (button == NeighborhoodButton) CityRenderer.m_Zoomed = true;
                if (button == WorldButton) CityRenderer.m_Zoomed = false;
            }
            else
            {
                //we're ingame, we need to recreate the city renderer
                //of course, don't know how this is going to work yet!
            }
            UpdateZoomButton();
        }

        private void OptionsModeButton_OnButtonClick(UIElement button)
        {
            SetPanel(5);
        }

        public void SetPanel(int newPanel) {
            OptionsModeButton.Selected = false;
            LiveModeButton.Selected = false;
            if (CurrentPanel != -1) this.Remove(Panel);
            if (newPanel != CurrentPanel)
            {
                switch (newPanel)
                {
                    case 5:
                        Panel = new UIOptions();
                        Panel.X = 177;
                        Panel.Y = 96;
                        this.Add(Panel);
                        OptionsModeButton.Selected = true;
                        break;

                    case 1:
                        if (vm == null) break; //not ingame
                        Panel = new UILiveMode();
                        Panel.X = 177;
                        Panel.Y = 63;
                        ((UILiveMode)Panel).SelectedAvatar = m_SelectedAvatar;
                        ((UILiveMode)Panel).vm = vm;
                        this.Add(Panel);
                        LiveModeButton.Selected = true;
                        break;
                    default:
                        break;
                }
                CurrentPanel = newPanel;
            }
            else
            {
                CurrentPanel = -1;
            }
            
        }

        public void SetMode(UCPMode mode)
        {
            var isLotMode = mode == UCPMode.LotMode;
            var isCityMode = mode == UCPMode.CityMode;

            FirstFloorButton.Visible = isLotMode;
            SecondFloorButton.Visible = isLotMode;
            WallsDownButton.Visible = isLotMode;
            WallsCutawayButton.Visible = isLotMode;
            WallsUpButton.Visible = isLotMode;
            RoofButton.Visible = isLotMode;

            LiveModeButton.Visible = isLotMode;
            BuyModeButton.Visible = isLotMode;
            BuildModeButton.Visible = isLotMode;
            HouseModeButton.Visible = isLotMode;
            HouseViewSelectButton.Visible = isLotMode;

            BackgroundMatchmaker.Visible = isCityMode;
            Background.Visible = isLotMode;
        }

        public void SetInLot(bool inLot)
        {
            CloseZoomButton.Disabled = !inLot;
            MediumZoomButton.Disabled = !inLot;
            FarZoomButton.Disabled = !inLot;
            RotateClockwiseButton.Disabled = !inLot;
            RotateCounterClockwiseButton.Disabled = !inLot;
        }

        public void UpdateZoomButton()
        {
            if (CityRenderer != null)
            {
                NeighborhoodButton.Selected = CityRenderer.m_Zoomed;
                WorldButton.Selected = !CityRenderer.m_Zoomed;

                ZoomInButton.Disabled = CityRenderer.m_Zoomed;
                ZoomOutButton.Disabled = !CityRenderer.m_Zoomed;
            }
        }


        public enum UCPMode
        {
            LotMode,
            CityMode
        }

    }
}
