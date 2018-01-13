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
using Microsoft.Xna.Framework.Input;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Screens;
using FSO.Client.Rendering.City;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using FSO.LotView;
using FSO.Client.Network;
using Microsoft.Xna.Framework;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.Client.Controllers;
using FSO.Common;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Utils;
using FSO.Common.Rendering.Framework;
using FSO.LotView.RC;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// UCP
    /// </summary>
    public class UIUCP : UICachedContainer
    {
        private IGameScreen Game; //the main screen
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
        /// Bookmarks
        /// </summary>
        public UIButton BookmarkButton { get; set; }
        public UIButton FriendshipWebButton { get; set; }

        /// <summary>
        /// Labels
        /// </summary>
        public UILabel TimeText { get; set; }
        public UILabel MoneyText { get; set; }

        private UIContainer Panel;
        public int CurrentPanel;

        private uint OldMoney;
        private int MoneyHighlightFrames;

        private UCPFocusMode Focus;
        private UIBlocker SelfBlocker;
        private UIBlocker PanelBlocker;
        private UIBlocker GameBlocker;
        private UILabel FloorNumLabel;
        private int LastZoom;

        private int InboxFlashTime;
        private bool InboxFlashing;

        public UIUCP(UIScreen owner)
        {
            this.RenderScript("ucp.uis");

            Game = (IGameScreen)owner;

            Background = new UIImage(BackgroundGameImage);
            this.AddAt(0, Background);
            Background.BlockInput();
            Size = new Vector2(Background.Width, Background.Height); //for caching


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
            HouseModeButton.OnButtonClick += (btn) => { SetPanel(4); };

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

            BookmarkButton.OnButtonClick += BookmarkButton_OnButtonClick;
            FriendshipWebButton.OnButtonClick += FriendshipWebButton_OnButtonClick;

            SecondFloorButton.Selected = (Game.Level == Game.Stories);
            FirstFloorButton.Selected = (Game.Level == 1);

            MoneyText.CaptionStyle = MoneyText.CaptionStyle.Clone();

            var temp = new UILabel();
            temp.X = SecondFloorButton.X + 7;
            temp.Y = SecondFloorButton.Y - 14;
            temp.Caption = "1";
            temp.CaptionStyle = temp.CaptionStyle.Clone();
            temp.CaptionStyle.Size = 7;
            temp.CaptionStyle.Shadow = true;
            Add(temp);
            FloorNumLabel = temp;

            SetInLot(false);
            SetMode(UCPMode.CityMode);
            Focus = UCPFocusMode.UCP;
            SetFocus(UCPFocusMode.Game);
        }

        private void FriendshipWebButton_OnButtonClick(UIElement button)
        {
            FindController<CoreGameScreenController>()?.ToggleRelationshipDialog();
        }

        private void BookmarkButton_OnButtonClick(UIElement button)
        {
            FindController<CoreGameScreenController>()?.ToggleBookmarks();
        }

        private void SecondFloor(UIElement button)
        {
            Game.LotControl.World.State.ScrollAnchor = null; //stop following a sim on a manual adjustment
            Game.Level = Math.Min((sbyte)(Game.Level + 1), Game.Stories);
            SecondFloorButton.Selected = (Game.Level == Game.Stories);
            FirstFloorButton.Selected = (Game.Level == 1);
        }

        /// <summary>
        /// Sets the "focus mode" of the UCP, used to make the UI accessible on phones.
        /// </summary>
        /// <param name="focus"></param>
        public void SetFocus(UCPFocusMode focus)
        {
            if (Focus == focus) return;
            if (FSOEnvironment.UIZoomFactor>1f)
            {
                if (focus != UCPFocusMode.Game)
                {
                    var tween = GameFacade.Screens.Tween.To(this, 0.33f, new Dictionary<string, float>()
                    {
                        {"ScaleX", FSOEnvironment.UIZoomFactor},
                        {"ScaleY", FSOEnvironment.UIZoomFactor},
                        {"Y", Game.ScreenHeight-(int)(210*FSOEnvironment.UIZoomFactor) },
                        {"X", (focus == UCPFocusMode.ActiveTab)?-(int)(225*FSOEnvironment.UIZoomFactor):0 }
                    }, TweenQuad.EaseInOut);

                    Remove(SelfBlocker); SelfBlocker = null;
                    if (focus == UCPFocusMode.ActiveTab)
                    {
                        Remove(PanelBlocker); PanelBlocker = null;
                    }

                    if (GameBlocker == null)
                    {
                        GameBlocker = new UIBlocker();
                        GameBlocker.Position = new Vector2(0, 220 - Game.ScreenHeight);
                        GameBlocker.OnMouseEvt += (evt, state) =>
                        {
                            if (evt == UIMouseEventType.MouseDown) SetFocus(UCPFocusMode.Game);
                        };
                        AddAt(0, GameBlocker);
                    }
                } else
                {
                    var tween = GameFacade.Screens.Tween.To(this, 0.33f, new Dictionary<string, float>()
                    {
                        {"ScaleX", 1f},
                        {"ScaleY", 1f},
                        {"Y", Game.ScreenHeight-210 },
                        {"X", 0 }
                    }, TweenQuad.EaseInOut);

                    if (SelfBlocker == null)
                    {
                        SelfBlocker = new UIBlocker(220, 210);
                        SelfBlocker.OnMouseEvt += (evt, state) =>
                        {
                            if (evt == UIMouseEventType.MouseDown) SetFocus(UCPFocusMode.UCP);
                        };
                        Add(SelfBlocker);
                    }

                    if (CurrentPanel > -1 && PanelBlocker == null)
                    {
                        PanelBlocker = new UIBlocker(580, 104);
                        PanelBlocker.Position = new Vector2(220, 106);
                        PanelBlocker.OnMouseEvt += (evt, state) =>
                        {
                            if (evt == UIMouseEventType.MouseDown) SetFocus(UCPFocusMode.ActiveTab);
                        };
                        Add(PanelBlocker);
                    }

                    Remove(GameBlocker); GameBlocker = null;
                }
            }
            Focus = focus;
        }

        private void FirstFloor(UIElement button)
        {
            Game.LotControl.World.State.ScrollAnchor = null; //stop following a sim on a manual adjustment
            Game.Level = Math.Max((sbyte)(Game.Level - 1), (sbyte)1);
            SecondFloorButton.Selected = (Game.Level == Game.Stories);
            FirstFloorButton.Selected = (Game.Level == 1);
        }

        private void RotateCounterClockwise(UIElement button)
        {
            if (FSOEnvironment.Enable3D && Game.InLot) return;
            var newRot = (Game.Rotation - 1);
            if (newRot < 0) newRot = 3;
            Game.Rotation = newRot;
        }

        private void RotateClockwise(UIElement button)
        {
            if (FSOEnvironment.Enable3D && Game.InLot) return;
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
            Game.LotControl.WallsMode = mode;
            UpdateWallsMode();
        }
        private void UpdateWallsViewKeyHandler(int type)
        {
            var mode = Game.LotControl.WallsMode;
            HouseViewSelectButton.Disabled = true; // triggers redraw of panel to show the correct mode
            switch (type)
            {
                case 0:
                    if (mode > 0) Game.LotControl.WallsMode -= 1;
                    break;
                case 1:
                    if (mode < 3) Game.LotControl.WallsMode += 1;
                    break;
            }
            UpdateWallsMode();
            HouseViewSelectButton.Disabled = false;
        }

        public override void Update(FSO.Common.Rendering.Framework.Model.UpdateState state)
        {
            var time = DateTime.UtcNow;
            var tsoTime = TSOTime.FromUTC(time);
            int min = tsoTime.Item2;
            int hour = tsoTime.Item1;

            if (MoneyHighlightFrames > 0)
            {
                if (--MoneyHighlightFrames == 0) MoneyText.CaptionStyle.Color = TextStyle.DefaultLabel.Color;
            }
            uint budget = 0;
            if (Game.InLot)
            {
                // if ingame, use time from ingame clock 
                // (should be very close to server time anyways, if we set the game pacing up right...)
                min = Game.vm.Context.Clock.Minutes;
                hour = Game.vm.Context.Clock.Hours;

                // update with ingame budget.
                var cont = Game.LotControl;
                if (cont.ActiveEntity != null && cont.ActiveEntity is VMAvatar)
                {
                    var avatar = (VMAvatar)cont.ActiveEntity;
                    budget = avatar.TSOState.Budget.Value;

                    //check if we have build/buy permissions
                    //TODO: global build/buy enable/disable (via the global calls)
                    BuildModeButton.Disabled = ((VMTSOAvatarState)(avatar.TSOState)).Permissions
                        < VMTSOAvatarPermissions.BuildBuyRoommate;
                    HouseModeButton.Disabled = BuyModeButton.Disabled;

                    if (CurrentPanel == 2)
                    {
                        var panel = (UIBuyMode)Panel;
                        var isRoomie = ((VMTSOAvatarState)(avatar.TSOState)).Permissions
                            >= VMTSOAvatarPermissions.Roommate;
                        panel.SetRoommate(isRoomie);
                    }
                }
                var level = Game.LotControl.World.State.Level.ToString();
                if (FloorNumLabel.Caption != level) FloorNumLabel.Caption = level;

                if (CurrentPanel == 3 && BuildModeButton.Disabled) SetPanel(-1);
                if (LastZoom != Game.ZoomLevel) UpdateZoomButton();
            }
            else budget = OldMoney;

            if (budget != OldMoney)
            {
                OldMoney = budget;
                MoneyText.CaptionStyle.Color = Color.White;
                MoneyHighlightFrames = 45;
                Game.VisualBudget = budget;
            }

            string suffix = (hour > 11) ? "pm" : "am";
            hour %= 12;
            if (hour == 0) hour = 12;

            TimeText.Caption = hour.ToString() + ":" + ZeroPad(min.ToString(), 2) + " " + suffix;
            MoneyText.Caption = "$" + Game.VisualBudget.ToString("##,#0");

            if (InboxFlashing)
            {
                if ((InboxFlashTime++) > FSOEnvironment.RefreshRate/2)
                {
                    if (PhoneButton.ForceState == 2)
                    {
                        PhoneButton.ForceState = -1;
                        Invalidate();
                    }
                } else
                {
                    if (PhoneButton.ForceState != 2)
                    {
                        PhoneButton.ForceState = 2;
                        Invalidate();
                    }
                }
                InboxFlashTime %= FSOEnvironment.RefreshRate;
            }

            var keys = state.NewKeys;
            var nofocus = state.InputManager.GetFocus() == null;
            base.Update(state);
            if (Game.InLot)
            {
                if (keys.Contains(Keys.F1) && !state.CtrlDown) SetPanel(1); // Live Mode Panel
                if (keys.Contains(Keys.F2)) SetPanel(2); // Buy Mode Panel
                if (keys.Contains(Keys.F3) && !BuildModeButton.Disabled) SetPanel(3); // Build Mode Panel
                if (keys.Contains(Keys.F4)) SetPanel(4); // House Mode Panel

                if (nofocus)
                {
                    if (FSOEnvironment.Enable3D)
                    {
                        //if the zoom or rotation buttons are down, gradually change their values.
                        if (RotateClockwiseButton.IsDown || state.KeyboardState.IsKeyDown(Keys.OemPeriod)) ((WorldStateRC)Game.vm.Context.World.State).RotationX += 2f / FSOEnvironment.RefreshRate;
                        if (RotateCounterClockwiseButton.IsDown || state.KeyboardState.IsKeyDown(Keys.OemComma)) ((WorldStateRC)Game.vm.Context.World.State).RotationX -= 2f / FSOEnvironment.RefreshRate;
                        if (ZoomInButton.IsDown || (state.KeyboardState.IsKeyDown(Keys.OemPlus) && !state.CtrlDown)) Game.LotControl.TargetZoom = Math.Max(0.25f, Math.Min(Game.LotControl.TargetZoom + 1f / FSOEnvironment.RefreshRate, 2));
                        if (ZoomOutButton.IsDown || (state.KeyboardState.IsKeyDown(Keys.OemMinus) && !state.CtrlDown)) Game.LotControl.TargetZoom = Math.Max(0.25f, Math.Min(Game.LotControl.TargetZoom - 1f / FSOEnvironment.RefreshRate, 2));
                    }
                    else
                    {
                        if (keys.Contains(Keys.OemPlus) && !state.CtrlDown && !ZoomInButton.Disabled) { Game.ZoomLevel -= 1; UpdateZoomButton(); }
                        if (keys.Contains(Keys.OemMinus) && !state.CtrlDown && !ZoomOutButton.Disabled) { Game.ZoomLevel += 1; UpdateZoomButton(); }
                        if (keys.Contains(Keys.OemComma)) RotateCounterClockwise(null);
                        if (keys.Contains(Keys.OemPeriod)) RotateClockwise(null);
                        if (keys.Contains(Keys.PageDown)) FirstFloor(null);
                        if (keys.Contains(Keys.PageUp)) SecondFloor(null);
                        if (keys.Contains(Keys.Home)) UpdateWallsViewKeyHandler(1);
                        if (keys.Contains(Keys.End)) UpdateWallsViewKeyHandler(0);
                    }
                }
            }
            if (keys.Contains(Keys.F5)) SetPanel(5); // Options Mode Panel
        }

        public void FlashInbox(bool flash)
        {
            InboxFlashing = flash;
            InboxFlashTime = 0;
            if (!flash)
            {
                PhoneButton.ForceState = -1;
            }
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
            var screen = (GameFacade.Screens.CurrentUIScreen as CoreGameScreen);
            screen?.OpenInbox();
        }

        private void ZoomControl(UIElement button)
        {
            if (FSOEnvironment.Enable3D && Game.InLot) return;
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
            GameFacade.Cursor.SetCursor(CursorType.Hourglass);

            OptionsModeButton.Selected = false;
            BuyModeButton.Selected = false;
            BuildModeButton.Selected = false;
            LiveModeButton.Selected = false;
            HouseModeButton.Selected = false;
            
            if (Game.InLot)
            {
                Game.LotControl.QueryPanel.Active = false;
                Game.LotControl.QueryPanel.Visible = false;
                Game.LotControl.LiveMode = true;
                Game.LotControl.World.State.BuildMode = 0;
            }

            if (CurrentPanel != -1)
            {
                switch (CurrentPanel)
                {
                    case 3:
                    case 2:
                        if (Game.InLot && Game.vm.TSOState.Roommates.Contains(Game.vm.MyUID)) FindController<CoreGameScreenController>().UploadLotThumbnail();
                        break;
                }
                DynamicOverlay.Remove(Panel);
                Panel = null;

                if (Game.InLot) Game.LotControl.PanelActive = false;
            }
            if (newPanel != CurrentPanel)
            {
                if (Game.InLot) Game.LotControl.PanelActive = true;
                switch (newPanel)
                {
                    case 5:
                        Panel = new UIOptions();
                        Panel.X = 177;
                        Panel.Y = 96;
                        DynamicOverlay.Add(Panel);
                        OptionsModeButton.Selected = true;
                        SetFocus(UCPFocusMode.ActiveTab);
                        break;
                    case 2:
                        if (!Game.InLot) break; //not ingame
                        Panel = new UIBuyMode(Game.LotControl);

                        //enable grid
                        Game.LotControl.World.State.BuildMode = 1;

                        Game.LotControl.LiveMode = false;
                        Panel.X = 177;
                        Panel.Y = 96;
                        DynamicOverlay.Add(Panel);
                        BuyModeButton.Selected = true;
                        SetFocus(UCPFocusMode.ActiveTab);
                        break;
                    case 3:
                        if (!Game.InLot) break; //not ingame
                        Panel = new UIBuildMode(Game.LotControl);

                        //enable air tile graphics + grid
                        Game.LotControl.World.State.BuildMode = 2;

                        Game.LotControl.LiveMode = false;
                        Panel.X = 177;
                        Panel.Y = 96;
                        DynamicOverlay.Add(Panel);
                        BuildModeButton.Selected = true;
                        SetFocus(UCPFocusMode.ActiveTab);
                        break;
                    case 4:
                        if (!Game.InLot) break; //not ingame
                        Panel = new UIHouseMode(Game.LotControl);

                        //enable grid
                        Game.LotControl.World.State.BuildMode = 1;

                        Panel.X = 177;
                        Panel.Y = 87;
                        DynamicOverlay.Add(Panel);
                        HouseModeButton.Selected = true;
                        SetFocus(UCPFocusMode.ActiveTab);
                        break;
                    case 1:
                        if (!Game.InLot) break; //not ingame
                        Panel = new UILiveMode(Game.LotControl);
                        Panel.X = 177;
                        Panel.Y = 61;
                        DynamicOverlay.Add(Panel);
                        LiveModeButton.Selected = true;
                        SetFocus(UCPFocusMode.ActiveTab);
                        break;
                    default:
                        if (Game.InLot) Game.LotControl.PanelActive = false;
                        break;
                }
                CurrentPanel = newPanel;
            }
            else
            {
                Remove(PanelBlocker);
                PanelBlocker = null;
                CurrentPanel = -1;
            }
            GameFacade.Cursor.SetCursor(CursorType.Normal);
        }

        public void UpdateWallsMode()
        {
            if (Background.Visible && Game.InLot)
            {
                var mode = Game.LotControl.WallsMode;
                WallsDownButton.Visible = (mode == 0);
                WallsCutawayButton.Visible = (mode == 1);
                WallsUpButton.Visible = (mode == 2);
                RoofButton.Visible = (mode == 3);
                Game.LotControl.World.State.DrawRoofs = (mode == 3);
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
            FloorNumLabel.Visible = isLotMode;

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

            if (Game is SandboxGameScreen)
            {
                ZoomInButton.Disabled = (!Game.InLot) || (!FSOEnvironment.Enable3D && (Game.ZoomLevel == 1));
                ZoomOutButton.Disabled = (!FSOEnvironment.Enable3D && (Game.ZoomLevel == 3));
            }
            else
            {
                ZoomInButton.Disabled = (!Game.InLot) ? (Game.ZoomLevel == 4) : (!FSOEnvironment.Enable3D && (Game.ZoomLevel == 1));
                ZoomOutButton.Disabled = (Game.ZoomLevel == 5);
            }
            LastZoom = Game.ZoomLevel;
        }


        public enum UCPMode
        {
            LotMode,
            CityMode
        }

        public enum UCPFocusMode
        {
            Game,
            UCP,
            ActiveTab
        }

    }
}
