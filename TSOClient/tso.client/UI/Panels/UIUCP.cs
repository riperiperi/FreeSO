/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Screens;
using FSO.Client.Rendering.City;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using FSO.LotView;
using FSO.Client.Network;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// UCP
    /// </summary>
    public class UIUCP : UIContainer
    {
        private CoreGameScreen Game; //the main screen
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

        public int SelectedSimID;

        private UISelectHouseView SelWallsPanel; //select view panel that is created when clicking the current walls mode

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

        private UIDestroyablePanel Panel;
        private int CurrentPanel;

        public UIUCP(UIScreen owner)
        {

            this.RenderScript("ucp.uis");

            Game = (CoreGameScreen)owner;

            Background = new UIImage(BackgroundGameImage);
            this.AddAt(0, Background);
            Background.BlockInput();


            BackgroundMatchmaker = new UIImage(BackgroundMatchmakerImage);
            BackgroundMatchmaker.Y = 81;
            this.AddAt(0, BackgroundMatchmaker);
            BackgroundMatchmaker.BlockInput();

            TimeText.Caption = "12:00 am";
            TimeText.CaptionStyle = TimeText.CaptionStyle.Clone();
            TimeText.CaptionStyle.Shadow = true;
            MoneyText.Caption = "§0";
            MoneyText.CaptionStyle = TimeText.CaptionStyle.Clone();
            MoneyText.CaptionStyle.Shadow = true;

            CurrentPanel = -1;

            OptionsModeButton.OnButtonClick += new ButtonClickDelegate(OptionsModeButton_OnButtonClick);
            LiveModeButton.OnButtonClick += new ButtonClickDelegate(LiveModeButton_OnButtonClick);
            BuyModeButton.OnButtonClick += new ButtonClickDelegate(BuyModeButton_OnButtonClick);
            BuildModeButton.OnButtonClick += BuildModeButton_OnButtonClick;

            ZoomOutButton.OnButtonClick += new ButtonClickDelegate(ZoomControl);
            ZoomInButton.OnButtonClick += new ButtonClickDelegate(ZoomControl);
            PhoneButton.OnButtonClick += new ButtonClickDelegate(PhoneButton_OnButtonClick);

            CloseZoomButton.OnButtonClick += new ButtonClickDelegate(SetCityZoom);
            MediumZoomButton.OnButtonClick += new ButtonClickDelegate(SetCityZoom);
            FarZoomButton.OnButtonClick += new ButtonClickDelegate(SetCityZoom);
            NeighborhoodButton.OnButtonClick += new ButtonClickDelegate(SetCityZoom);
            WorldButton.OnButtonClick += new ButtonClickDelegate(SetCityZoom);

            HouseViewSelectButton.OnButtonClick += new ButtonClickDelegate(WallsViewPopup);
            WallsDownButton.OnButtonClick += new ButtonClickDelegate(WallsViewPopup);
            WallsUpButton.OnButtonClick += new ButtonClickDelegate(WallsViewPopup);
            WallsCutawayButton.OnButtonClick += new ButtonClickDelegate(WallsViewPopup);
            RoofButton.OnButtonClick += new ButtonClickDelegate(WallsViewPopup);

            RotateClockwiseButton.OnButtonClick += new ButtonClickDelegate(RotateClockwise);
            RotateCounterClockwiseButton.OnButtonClick += new ButtonClickDelegate(RotateCounterClockwise);

            FirstFloorButton.OnButtonClick += FirstFloor;
            SecondFloorButton.OnButtonClick += SecondFloor;

            SecondFloorButton.Selected = (Game.Level == Game.Stories);
            FirstFloorButton.Selected = (Game.Level == 1);

            SetInLot(false);
            SetMode(UCPMode.CityMode);
        }

        private void SecondFloor(UIElement button)
        {
            Game.Level = Math.Min((sbyte)(Game.Level + 1), Game.Stories);
            SecondFloorButton.Selected = (Game.Level == Game.Stories);
            FirstFloorButton.Selected = (Game.Level == 1);
        }

        private void FirstFloor(UIElement button)
        {
            Game.Level = Math.Max((sbyte)(Game.Level - 1), (sbyte)1);
            SecondFloorButton.Selected = (Game.Level == Game.Stories);
            FirstFloorButton.Selected = (Game.Level == 1);
        }

        private void RotateCounterClockwise(UIElement button)
        {
            var newRot = (Game.Rotation - 1);
            if (newRot < 0) newRot = 3;
            Game.Rotation = newRot;
        }

        private void RotateClockwise(UIElement button)
        {
            Game.Rotation = (Game.Rotation+1)%4;
        }

        private void WallsViewPopup(UIElement button)
        {
            if (SelWallsPanel == null)
            {
                SelWallsPanel = new UISelectHouseView();
                SelWallsPanel.X = 31;
                SelWallsPanel.Y = 48;
                SelWallsPanel.OnModeSelection += new HouseViewSelection(UpdateWallsViewCallback);
                this.Add(SelWallsPanel);
            }
        }

        private void UpdateWallsViewCallback(int mode)
        {
            Remove(SelWallsPanel);
            SelWallsPanel = null;
            Game.LotController.WallsMode = mode;
            UpdateWallsMode();
        }

        public override void Update(FSO.Common.Rendering.Framework.Model.UpdateState state)
        {
            //int min = NetworkFacade.ServerTime.Minute;
            //int hour = NetworkFacade.ServerTime.Hour;
            int min = 0;
            int hour = 0;

            if (Game.InLot) //if ingame, use time from ingame clock (should be very close to server time anyways, if we set the game pacing up right...)
            {
                min = Game.vm.Context.Clock.Minutes;
                hour = Game.vm.Context.Clock.Hours;
            }

            string suffix = (hour > 11) ? "pm" : "am";
            hour %= 12;
            if (hour == 0) hour = 12;

            TimeText.Caption = hour.ToString() + ":" + ZeroPad(min.ToString(), 2) + " " + suffix;

            base.Update(state);
        }

        private string ZeroPad(string input, int digits)
        {
            while (input.Length < digits)
            {
                input = "0" + input;
            }
            return input;
        }

        void LiveModeButton_OnButtonClick(UIElement button)
        {
            SetPanel(1);
        }

        void BuyModeButton_OnButtonClick(UIElement button)
        {
            SetPanel(2);
        }

        private void BuildModeButton_OnButtonClick(UIElement button)
        {
            SetPanel(3);
        }

        void PhoneButton_OnButtonClick(UIElement button)
        {
            var screen = (CoreGameScreen)GameFacade.Screens.CurrentUIScreen;
            screen.OpenInbox();
        }

        private void ZoomControl(UIElement button)
        {
            Game.ZoomLevel = (Game.ZoomLevel + ((button == ZoomInButton) ? -1 : 1));
            /*if(Game.ZoomLevel >= 4) SetPanel(0);    // Make the panels disappear when zoomed out to far mode   -  Causes crashes for unknown reasons*/
        }

        private void SetCityZoom(UIElement button)
        {
            if (button == CloseZoomButton) Game.ZoomLevel = 1;
            if (button == MediumZoomButton) Game.ZoomLevel = 2;
            if (button == FarZoomButton) Game.ZoomLevel = 3;
            if (button == NeighborhoodButton) Game.ZoomLevel = 4;
            if (button == WorldButton) Game.ZoomLevel = 5;
        }

        private void OptionsModeButton_OnButtonClick(UIElement button)
        {
            SetPanel(5);
        }

        public void SetPanel(int newPanel) {
            OptionsModeButton.Selected = false;
            BuyModeButton.Selected = false;
            BuildModeButton.Selected = false;
            LiveModeButton.Selected = false;
            
            if (Game.InLot)
            {
                Game.LotController.QueryPanel.Active = false;
                Game.LotController.QueryPanel.Visible = false;
                Game.LotController.LiveMode = true;
                Game.vm.Context.World.State.BuildMode = false;
            }

            if (CurrentPanel != -1)
            {
                this.Remove(Panel);
                Panel.Destroy();
            }
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
                    case 2:
                        if (!Game.InLot) break; //not ingame
                        Panel = new UIBuyMode(Game.LotController);
                        Game.LotController.LiveMode = false;
                        Panel.X = 177;
                        Panel.Y = 96;
                        ((UIBuyMode)Panel).SelectedAvatar = m_SelectedAvatar;
                        ((UIBuyMode)Panel).vm = Game.vm;
                        this.Add(Panel);
                        BuyModeButton.Selected = true;
                        break;
                    case 3:
                        if (!Game.InLot) break; //not ingame
                        Panel = new UIBuildMode(Game.LotController);

                        //enable air tile graphics
                        Game.vm.Context.World.State.BuildMode = true;

                        Game.LotController.LiveMode = false;
                        Panel.X = 177;
                        Panel.Y = 96;
                        ((UIBuildMode)Panel).SelectedAvatar = m_SelectedAvatar;
                        ((UIBuildMode)Panel).vm = Game.vm;
                        this.Add(Panel);
                        BuildModeButton.Selected = true;
                        break;
                    case 1:
                        if (!Game.InLot) break; //not ingame
                        Panel = new UILiveMode();
                        Panel.X = 177;
                        Panel.Y = 63;
                        ((UILiveMode)Panel).SelectedAvatar = m_SelectedAvatar;
                        ((UILiveMode)Panel).vm = Game.vm;
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

        public void UpdateWallsMode()
        {
            if (Background.Visible && Game.InLot)
            {
                var mode = Game.LotController.WallsMode;
                WallsDownButton.Visible = (mode == 0);
                WallsCutawayButton.Visible = (mode == 1);
                WallsUpButton.Visible = (mode == 2);
                RoofButton.Visible = (mode == 3);
            } else {
                WallsDownButton.Visible = false;
                WallsCutawayButton.Visible = false;
                WallsUpButton.Visible = false;
                RoofButton.Visible = false;
            }
        }

        public void SetMode(UCPMode mode)
        {
            var isLotMode = mode == UCPMode.LotMode;
            var isCityMode = mode == UCPMode.CityMode;

            FirstFloorButton.Visible = isLotMode;
            SecondFloorButton.Visible = isLotMode;

            LiveModeButton.Visible = isLotMode;
            BuyModeButton.Visible = isLotMode;
            BuildModeButton.Visible = isLotMode;
            HouseModeButton.Visible = isLotMode;
            HouseViewSelectButton.Visible = isLotMode;
            RotateClockwiseButton.Disabled = isCityMode;
            RotateCounterClockwiseButton.Disabled = isCityMode;

            BackgroundMatchmaker.Visible = isCityMode;
            Background.Visible = isLotMode;

            UpdateWallsMode();

            if (isCityMode && SelWallsPanel != null)
            {
                Remove(SelWallsPanel);
                SelWallsPanel = null;
            }
        }

        public void SetInLot(bool inLot)
        {
            CloseZoomButton.Disabled = !inLot;
            MediumZoomButton.Disabled = !inLot;
            FarZoomButton.Disabled = !inLot;
            UpdateWallsMode();
        }

        public void UpdateZoomButton()
        {
            CloseZoomButton.Selected = (Game.ZoomLevel == 1);
            MediumZoomButton.Selected = (Game.ZoomLevel == 2);
            FarZoomButton.Selected = (Game.ZoomLevel == 3);
            NeighborhoodButton.Selected = (Game.ZoomLevel == 4);
            WorldButton.Selected = (Game.ZoomLevel == 5);

            ZoomInButton.Disabled = (!Game.InLot) ? (Game.ZoomLevel == 4) : (Game.ZoomLevel == 1);
            ZoomOutButton.Disabled = (Game.ZoomLevel == 5);
        }


        public enum UCPMode
        {
            LotMode,
            CityMode
        }

    }
}
