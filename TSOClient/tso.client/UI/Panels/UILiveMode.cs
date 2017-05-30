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
using Microsoft.Xna.Framework;
using FSO.SimAntics;
using FSO.SimAntics.Model;
using FSO.Client.UI.Panels.EODs;
using FSO.Client.UI.Framework.Parser;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.LotView.Components;
using FSO.LotView;
using FSO.Common;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Live Mode Panel
    /// </summary>
    public class UILiveMode : UICachedContainer
    {
        public UIImage Background;
        public UIImage Divider;
        public UIVMPersonButton Thumb;
        public UIMotiveDisplay MotiveDisplay;
        public Texture2D DividerImg { get; set; }
        public Texture2D PeopleListBackgroundImg { get; set; }
        public Texture2D PeopleListEODBackgroundImg { get; set; }
        public Texture2D MoodPositiveImg { get; set; }
        public Texture2D MoodNegativeImg { get; set; }

        //EOD stuff
        public UIButton EODHelpButton { get; set; }
        public UIButton EODCloseButton { get; set; }
        public UIButton EODExpandButton { get; set; }
        public UIButton EODContractButton { get; set; }
        public UIImage EODExpandBack { get; set; }

        private UIEODLayout EODLayout;

        public Texture2D BackgroundEODImg { get; set; } //live mode with backgrounded eod
        public Texture2D EODPanelImg { get; set; }
        public Texture2D EODPanelTallImg { get; set; }
        public Texture2D EODDoublePanelTallImg { get; set; }

        public UIImage EODPanel { get; set; }
        public UIImage EODPanelTall { get; set; }
        public UIImage EODDoublePanelTall { get; set; }

        public UIImage EODButtonLayout { get; set; }
        public UIImage EODTopButtonLayout { get; set; }
        public UIImage EODSub { get; set; }
        public UIImage EODTopSub { get; set; }

        public UIImage EODMsgWin { get; set; }

        public UIImage EODTimer { get; set; }
        public Texture2D EODButtonImg { get; set; }

        public UIListBox MsgWinTextEntry { get; set; }
        public UITextEdit TimerTextEntry { get; set; }

        //onlinejobs stuff
        public UIImage StatusBarMsgWinStraight { get; set; }
        public UIImage StatusBarTimerStraight { get; set; }
        public UIImage StatusBarTimerBreakIcon { get; set; }
        public UIImage StatusBarTimerWorkIcon { get; set; }
        public UITextEdit StatusBarTimerTextEntry { get; set; }
        public UIListBox StatusBarMsgWinTextEntry { get; set; }

        public int StatusBarCycleTime;
        public VMTSOJobUI JobUI;

        //normal stuff
        public UIButton MoodPanelButton;

        public UIButton PreviousPageButton { get; set; }
        public UIButton NextPageButton { get; set; }

        public UILabel MotivesLabel { get; set; }
        public UIImage PeopleListBg;

        public UIPersonGrid PersonGrid;

        public UILotControl LotController;
        private VMAvatar SelectedAvatar
        {
            get
            {
                return (LotController.ActiveEntity != null && LotController.ActiveEntity is VMAvatar) ? (VMAvatar)LotController.ActiveEntity : null;
            }
        }
        private VMAvatar LastSelected;
        private EODLiveModeOpt LastEODConfig;
        private UIEOD ActiveEOD;
        private bool HideEOD;

        private UIScript Script;
        
        public Vector2 DefaultNextPagePos;

        public UIButton EODButton;
        public UIImage EODImage;
        public Texture2D DefaultBGImage;
        private bool Small800;

        public UILiveMode (UILotControl lotController) {
            Small800 = (GlobalSettings.Default.GraphicsWidth < 1024) || FSOEnvironment.UIZoomFactor > 1f;
            var script = this.RenderScript("livepanel"+(Small800?"":"1024")+".uis");
            EODLayout = new UIEODLayout(script);
            Script = script;
            LotController = lotController;

            DefaultBGImage = GetTexture(Small800 ? (ulong)0x000000D800000002 : (ulong)0x0000018300000002);
            Background = new UIImage(DefaultBGImage);
            Background.Y = 35;
            this.AddAt(0, Background);

            EODCloseButton.OnButtonClick += EODCloseButton_OnButtonClick;

            MotivesLabel.CaptionStyle = MotivesLabel.CaptionStyle.Clone();
            MotivesLabel.CaptionStyle.Shadow = true;
            MotivesLabel.Alignment = TextAlignment.Left;
            MotivesLabel.Position -= new Vector2(0, 5);

            PeopleListBg = new UIImage(PeopleListBackgroundImg);
            PeopleListBg.Position = new Microsoft.Xna.Framework.Vector2(375, 38);
            this.AddAt(1, PeopleListBg);

            Divider = new UIImage(DividerImg);
            Divider.Position = new Microsoft.Xna.Framework.Vector2(140, 49);
            this.AddAt(1, Divider);

            MoodPanelButton = new UIButton();
            
            MoodPanelButton.Texture = GetTexture((ulong)GameContent.FileIDs.UIFileIDs.lpanel_moodpanelbtn);
            MoodPanelButton.ImageStates = 4;
            MoodPanelButton.Position = new Vector2(31, 63);
            this.Add(MoodPanelButton);

            MotiveDisplay = new UIMotiveDisplay();
            MotiveDisplay.Position = new Vector2(165, 56);
            this.Add(MotiveDisplay);
            DynamicOverlay.Add(MotiveDisplay);

            PersonGrid = new UIPersonGrid(LotController.vm);
            Add(PersonGrid);
            PersonGrid.Position = new Vector2(409, 51);
            if (Small800) {
                PersonGrid.Columns = 4;
                PersonGrid.DrawPage();
            }
            
            EODPanel = new UIImage(EODPanelImg);
            EODPanelTall = new UIImage(EODPanelTallImg);
            EODDoublePanelTall = new UIImage(EODDoublePanelTallImg);

            Size = new Vector2(Background.Size.X, EODPanelTall.Size.Y);

            AddAt(0, EODDoublePanelTall);
            AddAt(0, EODPanel);
            AddAt(0, EODPanelTall);


            EODButtonLayout = new UIImage();
            EODSub = new UIImage();
            EODExpandBack = Script.Create<UIImage>("EODExpandBack");

            Add(EODButtonLayout);
            Add(EODSub);
            Add(EODExpandBack);

            EODTopSub = new UIImage();
            EODTopButtonLayout = new UIImage();
            Add(EODTopButtonLayout);
            Add(EODTopSub);


            StatusBarMsgWinStraight = script.Create<UIImage>("StatusBarMsgWinStraight");
            StatusBarTimerStraight = script.Create<UIImage>("StatusBarTimerStraight");
            StatusBarTimerBreakIcon = script.Create<UIImage>("StatusBarTimerBreakIcon");
            StatusBarTimerWorkIcon = script.Create<UIImage>("StatusBarTimerWorkIcon");

            StatusBarTimerStraight.X -= 1;
            StatusBarTimerStraight.Y += 2;
            StatusBarTimerBreakIcon.Y += 2;
            StatusBarTimerWorkIcon.Y += 2;
            StatusBarTimerTextEntry.Y += 2;
            StatusBarTimerTextEntry.X += 3;
            StatusBarMsgWinStraight.Y += 2;

            AddAt(0, StatusBarTimerBreakIcon);
            AddAt(0, StatusBarTimerWorkIcon);
            AddAt(0, StatusBarTimerStraight);
            AddAt(0, StatusBarMsgWinStraight);

            StatusBarMsgWinStraight.Visible = false;
            StatusBarTimerStraight.Visible = false;
            StatusBarTimerBreakIcon.Visible = false;
            StatusBarTimerWorkIcon.Visible = false;
            StatusBarTimerTextEntry.Visible = false;
            StatusBarMsgWinTextEntry.Visible = false;

            EODMsgWin = new UIImage();
            EODTimer = script.Create<UIImage>("EODTimer");

            AddAt(0, EODTimer);
            AddAt(0, EODMsgWin);

            EODButton = new UIButton(EODButtonImg);
            Add(EODButton);
            EODButton.OnButtonClick += EODToggle;
            EODImage = script.Create<UIImage>("EODButtonImageSize");
            Add(EODImage);

            Add(EODExpandButton);
            Add(EODContractButton);

            EODExpandButton.OnButtonClick += EODExpandToggle;
            EODContractButton.OnButtonClick += EODExpandToggle;

            NextPageButton.OnButtonClick += (UIElement btn) => { PersonGrid.NextPage(); };
            DefaultNextPagePos = NextPageButton.Position;
            PreviousPageButton.OnButtonClick += (UIElement btn) => { PersonGrid.PreviousPage(); };

            MsgWinTextEntry.Items.Add(new UIListBoxItem("", ""));
            
            SetInEOD(null, null);
        }

        private void EODExpandToggle(UIElement button)
        {
            if(LastEODConfig != null)
            {
                LastEODConfig.Expanded = !LastEODConfig.Expanded;
                if (LastEODConfig.Expanded)
                {
                    ActiveEOD.OnExpand();
                }
                else
                {
                    ActiveEOD.OnContract();
                }
                SetInEOD(LastEODConfig, ActiveEOD);
            }
        }

        private void EODToggle(UIElement button)
        {
            HideEOD = !HideEOD;
            SetInEOD(LastEODConfig, ActiveEOD);
        }

        private void EODCloseButton_OnButtonClick(UIElement button)
        {
            if (ActiveEOD != null) ActiveEOD.OnClose();
        }

        public void SetInEOD(EODLiveModeOpt options, UIEOD eod)
        {
            Invalidate(); //i mean, duh
            bool eodPresent = (options != null);
            bool inEOD = eodPresent && !HideEOD;
            if (ActiveEOD != null) DynamicOverlay.Remove(ActiveEOD);

            LastEODConfig = options;
            ActiveEOD = eod;


            /**
             * Useful values
             */

            bool isTall = inEOD && (options.Height == EODHeight.Tall || options.Height == EODHeight.TallTall);
            bool isDoubleTall = inEOD && options.Height == EODHeight.TallTall;


            /**
             * Reset / hide standard and eod UI
             */
            MoodPanelButton.Position = (eodPresent) ? EODLayout.Baseline + new Vector2(20, 7) : new Vector2(31, 63);
            EODButtonLayout.Visible = inEOD;
            EODSub.Visible = inEOD;
            EODMsgWin.Visible = inEOD && options.Tips != EODTextTips.None;

            EODHelpButton.Visible = inEOD;
            EODCloseButton.Visible = inEOD;
            EODExpandButton.Visible = inEOD && options.Expandable && !options.Expanded;
            EODContractButton.Visible = inEOD && options.Expandable && options.Expanded;
            EODExpandBack.Visible = inEOD && options.Expandable;
            EODButton.Visible = eodPresent;

            EODTopSub.Visible = inEOD && options.Expandable && options.Expanded;
            EODTopButtonLayout.Visible = inEOD && options.Expandable && options.Expanded;

            EODPanel.Visible = inEOD && !isTall;
            EODPanelTall.Visible = inEOD && isTall;
            EODDoublePanelTall.Visible = inEOD && isDoubleTall && options.Expanded;

            EODTimer.Visible = inEOD && options.Timer == EODs.EODTimer.Normal;
            MsgWinTextEntry.Visible = inEOD && options.Tips != EODTextTips.None;
            TimerTextEntry.Visible = inEOD && options.Timer != EODs.EODTimer.None;

            //Cleanup
            if (EODImage.Texture != null) EODImage.Texture.Dispose();
            EODImage.Texture = null;

            //EOD Button
            EODButton.Selected = inEOD;
            EODButton.Position = EODLayout.EODButtonPosition;

            /**
             * Attach new EOD UI
             */
            if (inEOD)
            {
                DynamicOverlay.Add(ActiveEOD);
            }
            
            /**
             * Position / style EOD specific UI
             */
            if (eodPresent)
            {
                EODButtonLayout.Reset();
                EODSub.Reset();
                EODMsgWin.Reset();

                var buttons = new string[] { "None", "One", "Two" };
                var buttonLayout = buttons[options.Buttons];
                Script.ApplyControlProperties(EODButtonLayout, "EODButtonLayout" + buttonLayout + EODLayout.GetHeightSuffix(options.Height, true));
                Script.ApplyControlProperties(EODSub, "EODSub" + options.Length + "Length" + EODLayout.GetHeightSuffix(options.Height, true));

                if (options.Tips != EODTextTips.None){
                    Script.ApplyControlProperties(EODMsgWin, "EODMsgWin" + options.Tips.ToString());
                }

                var topLeft = EODLayout.GetTopLeft(options.Height);
                
                //EOD position
                ActiveEOD.Position = topLeft + (Vector2)Script.GetControlProperty("EODPosition");

                //Close button
                EODCloseButton.Position = EODLayout.GetChromePosition("EODCloseButton", options.Height);

                //Help button
                EODHelpButton.Position = EODLayout.HelpButtonPosition;

                //Chrome
                var chromeOffset = EODLayout.GetChromeOffset(options.Height);
                EODButtonLayout.Position += chromeOffset;
                EODSub.Position += chromeOffset;

                //Message
                EODMsgWin.Position = EODLayout.GetMessageWindowPosition(options.Height, options.Tips, options.Expanded);
                MsgWinTextEntry.Position = EODLayout.GetMessageWindowTextPosition(options.Height, options.Expanded);

                //Timer
                EODTimer.Position = EODLayout.GetTimerPosition(options.Height);
                TimerTextEntry.Position = EODLayout.GetTimerTextPosition(options.Height);

                //Expand / contract
                EODExpandButton.Position = EODLayout.GetExpandButtonPosition(options.Height);
                EODContractButton.Position = EODLayout.GetContractButtonPosition(options.Height);
                EODExpandBack.Position = EODLayout.GetExpandBackPosition(options.Height);

                //backgrounds
                EODPanel.Position = EODLayout.GetPanelPosition(EODHeight.Normal);
                EODPanelTall.Position = EODLayout.GetPanelPosition(EODHeight.Tall);
                EODDoublePanelTall.Position = EODLayout.GetPanelPosition(EODHeight.TallTall);

                Size = new Vector2(Background.Size.X, (options.Height == EODHeight.TallTall)? EODDoublePanelTall.Size.Y:EODPanelTall.Size.Y);
                BackOffset = new Point();

                //Double tall panel chrome
                if (options.Height == EODHeight.TallTall)
                {
                    BackOffset = new Point(0, -(int)EODDoublePanelTall.Y);
                    EODTopSub.Reset();
                    EODTopButtonLayout.Reset();

                    var topButtonLayout = buttons[options.TopPanelButtons];
                    Script.ApplyControlProperties(EODTopButtonLayout, "EODButtonLayout" + topButtonLayout + EODLayout.GetHeightSuffix(EODHeight.Tall));
                    Script.ApplyControlProperties(EODTopSub, "EODSub" + options.Length + "Length" + EODLayout.GetHeightSuffix(EODHeight.Tall));
                    
                    EODTopButtonLayout.Position -= new Vector2(0, 155);
                    EODTopSub.Position -= new Vector2(0, 155);
                }



                var ava = SelectedAvatar;
                if (ava != null)
                {
                    var eodConnection = ava.Thread.EODConnection;
                    if (eodConnection != null)
                    {
                        var entity = LotController.vm.GetObjectById(eodConnection.ObjectID);
                        if (entity is VMGameObject)
                        {
                            var objects = entity.MultitileGroup.Objects;
                            ObjectComponent[] objComps = new ObjectComponent[objects.Count];
                            for (int i = 0; i < objects.Count; i++)
                            {
                                objComps[i] = (ObjectComponent)objects[i].WorldUI;
                            }
                            var thumb = LotController.World.GetObjectThumb(objComps, entity.MultitileGroup.GetBasePositions(), GameFacade.GraphicsDevice);

                            EODImage.Texture = thumb;
                        }
                    }
                }
                if (EODImage.Texture != null)
                {
                    var imgScale = 22f / Math.Max(EODImage.Texture.Width, EODImage.Texture.Height);
                    EODImage.SetSize(EODImage.Texture.Width * imgScale, EODImage.Texture.Height * imgScale);

                    EODImage.Position = EODButton.Position + new Vector2((EODButton.Texture.Width / 4 - EODImage.Width) / 2, (EODButton.Texture.Height - EODImage.Height) / 2);
                }
                


            } else
            {
                Size = new Vector2(DefaultBGImage.Width, EODPanelTall.Size.Y);
            }

            //this.Y = (inEOD && options.Height == EODHeight.Tall) ? 41: 61;

            Divider.Visible = !inEOD;
            MotiveDisplay.Visible = !inEOD;
            PersonGrid.Visible = !inEOD;
            MotivesLabel.Visible = !inEOD;
            PeopleListBg.Visible = !inEOD;
            PreviousPageButton.Visible = !inEOD;
            NextPageButton.Visible = !inEOD;
            Background.Visible = !inEOD;

            PersonGrid.Columns = (eodPresent || Small800) ?4:9;
            PersonGrid.DrawPage();
            PeopleListBg.Texture = (eodPresent && PeopleListEODBackgroundImg != null) ? PeopleListEODBackgroundImg : PeopleListBackgroundImg;
            PeopleListBg.SetSize(PeopleListBg.Texture.Width, PeopleListBg.Texture.Height);

            NextPageButton.Position = (eodPresent && !Small800) ? (Vector2)Script.GetControlProperty("NextPageEODButton") : DefaultNextPagePos;
            Background.Texture = (eodPresent) ? BackgroundEODImg : DefaultBGImage;
            Background.SetSize(Background.Texture.Width, Background.Texture.Height);

            UpdateThumbPosition();
        }

        public void UpdateThumbPosition()
        {
            if (Thumb != null) Thumb.Position = MoodPanelButton.Position + new Vector2(33, 10);
        }

        public override void Update(FSO.Common.Rendering.Framework.Model.UpdateState state)
        {
            if (SelectedAvatar != null)
            {
                if (SelectedAvatar != LastSelected)
                {
                    if (Thumb != null) Remove(Thumb);
                    Thumb = new UIVMPersonButton(SelectedAvatar, LotController.vm, false);
                    UpdateThumbPosition();
                    Add(Thumb);
                    LastSelected = SelectedAvatar;
                }

                UpdateMotives();
            }
            base.Update(state);

            var jobMode = JobUI != null;
            StatusBarTimerTextEntry.Visible = jobMode;
            StatusBarMsgWinStraight.Visible = jobMode;
            StatusBarMsgWinTextEntry.Visible = jobMode;
            StatusBarTimerStraight.Visible = jobMode;

            if (jobMode)
            {
                JobUI = LotController.vm.TSOState.JobUI;
                bool textDirty = false;
                if (StatusBarMsgWinTextEntry.Items.Count != JobUI.MessageText.Count)
                    textDirty = true;
                else
                {
                    for (int i=0; i<JobUI.MessageText.Count; i++)
                    {
                        if (!StatusBarMsgWinTextEntry.Items[i].Columns[0].Equals(JobUI.MessageText[i]))
                        {
                            textDirty = true;
                            break;
                        }
                    }
                }

                if (textDirty)
                {
                    StatusBarMsgWinTextEntry.Items = JobUI.MessageText.Select(x => new UIListBoxItem(x, x)).ToList();
                    StatusBarMsgWinTextEntry.ScrollOffset = 0;
                    StatusBarCycleTime = 0;
                }

                StatusBarTimerBreakIcon.Visible = JobUI.Mode == VMTSOJobMode.Intermission;
                StatusBarTimerWorkIcon.Visible = JobUI.Mode == VMTSOJobMode.Round;
                var timeText = " " + JobUI.Minutes + ":" + (JobUI.Seconds.ToString().PadLeft(2, '0'));
                if (StatusBarTimerTextEntry.CurrentText != timeText) StatusBarTimerTextEntry.CurrentText = timeText;

                if (StatusBarCycleTime++ > 60*4 && StatusBarMsgWinTextEntry.Items.Count > 0)
                {
                    StatusBarMsgWinTextEntry.ScrollOffset = (StatusBarMsgWinTextEntry.ScrollOffset + 1) % StatusBarMsgWinTextEntry.Items.Count;
                    StatusBarCycleTime = 0;
                }
            } else
            {
                StatusBarTimerBreakIcon.Visible = false;
                StatusBarTimerWorkIcon.Visible = false;

                JobUI = LotController.vm.TSOState.JobUI;
            }

            if (LastEODConfig != LotController.EODs.DisplayMode)
            {
                HideEOD = false;
                SetInEOD(LotController.EODs.DisplayMode, LotController.EODs.ActiveEOD);
            }

            if (LotController.EODs.EODMessage != (string)MsgWinTextEntry.Items[0].Data)
            {
                MsgWinTextEntry.Items[0].Data = LotController.EODs.EODMessage;
                MsgWinTextEntry.Items[0].Columns[0] = LotController.EODs.EODMessage;
            }
            if (LotController.EODs.EODTime != TimerTextEntry.CurrentText)
                TimerTextEntry.CurrentText = LotController.EODs.EODTime;
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            var selected = SelectedAvatar;
            if (selected != null) {
                var moodPos = MoodPanelButton.Position;
                var moodFrac = selected.GetMotiveData(VMMotive.Mood) / 4;
                if (moodFrac >= 0) {
                    DrawLocalTexture(batch, MoodPositiveImg, new Rectangle(0, 0, moodFrac, 33), moodPos + new Vector2(69, 11));
                } else
                {
                    DrawLocalTexture(batch, MoodNegativeImg, new Rectangle(25+moodFrac, 0, -moodFrac, 33), moodPos + new Vector2(30+moodFrac, 11));
                }
            }
        }

        private void UpdateMotives()
        {
            MotiveDisplay.MotiveValues[0] = SelectedAvatar.GetMotiveData(VMMotive.Hunger);
            MotiveDisplay.MotiveValues[1] = SelectedAvatar.GetMotiveData(VMMotive.Comfort);
            MotiveDisplay.MotiveValues[2] = SelectedAvatar.GetMotiveData(VMMotive.Hygiene);
            MotiveDisplay.MotiveValues[3] = SelectedAvatar.GetMotiveData(VMMotive.Bladder);
            MotiveDisplay.MotiveValues[4] = SelectedAvatar.GetMotiveData(VMMotive.Energy);
            MotiveDisplay.MotiveValues[5] = SelectedAvatar.GetMotiveData(VMMotive.Fun);
            MotiveDisplay.MotiveValues[6] = SelectedAvatar.GetMotiveData(VMMotive.Social);
            MotiveDisplay.MotiveValues[7] = SelectedAvatar.GetMotiveData(VMMotive.Room);
        }




    }



    public class UIEODLayout
    {
        public float ScreenBottom { get; internal set; }
        public Vector2 Baseline { get; internal set; }
        private UIScript Script;

        public UIEODLayout(UIScript script)
        {
            this.Script = script;

            //EOD baseline should be 114 from the bottom of the screen
            this.ScreenBottom = 149;
            this.Baseline = new Vector2(0, ScreenBottom - 114);
        }

        public string GetHeightSuffix(EODHeight height)
        {
            return GetHeightSuffix(height, false);
        }

        public string GetHeightSuffix(EODHeight height, bool considerDoubleAsTall)
        {
            switch (height)
            {
                case EODHeight.Tall:
                    return "Tall";
                case EODHeight.TallTall:
                    if (considerDoubleAsTall)
                    {
                        return "Tall";
                    }
                    else
                    {
                        return "";
                    }
                default:
                    return "";
            }
        }


        public Vector2 GetOffset(EODHeight height)
        {
            switch (height)
            {
                case EODHeight.Normal:
                    return (Vector2)Script.GetControlProperty("EODActiveOffset");
                case EODHeight.Tall:
                    return (Vector2)Script.GetControlProperty("EODActiveOffsetTall");
                case EODHeight.Trade:
                    return (Vector2)Script.GetControlProperty("EODActiveOffsetTrade");
                case EODHeight.TallTall:
                    return (Vector2)Script.GetControlProperty("EODActiveOffsetTallTall");
            }
            throw new Exception("Unknown eod height");
        }

        /// <summary>
        /// Top left of the EOD, this is where the EOD plugin itself is placed
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        public Vector2 GetTopLeft(EODHeight height)
        {
            return Baseline - GetOffset(height);
        }

        public Vector2 GetPanelPosition(EODHeight height)
        {
            switch (height)
            {
                case EODHeight.TallTall:
                    return GetTopLeft(EODHeight.TallTall) + (Vector2)Script.GetControlProperty("EODBackgroundOffsetTallTall");
                default:
                    return GetTopLeft(height);
            }
        }

        public Vector2 GetChromePosition(string control, EODHeight height)
        {
            return Baseline + (Vector2)Script.GetControlProperty(control, "position") + GetChromeOffset(height);
        }

        public Vector2 GetExpandButtonPosition(EODHeight height)
        {
            return Baseline + (Vector2)Script.GetControlProperty("EODExpandButton", "position");
        }

        public Vector2 GetContractButtonPosition(EODHeight height)
        {
            return Baseline + (Vector2)Script.GetControlProperty("EODContractButton", "position");
        }

        public Vector2 GetExpandBackPosition(EODHeight height)
        {
            return GetPanelPosition(EODHeight.Tall) + (Vector2)Script.GetControlProperty("EODExpandBack", "position");
        }

        public Vector2 GetTimerPosition(EODHeight height)
        {
            return GetTopLeft(height) + (Vector2)Script.GetControlProperty("EODTimer", "position");
        }

        public Vector2 GetTimerTextPosition(EODHeight height)
        {
            return GetTopLeft(height) + (Vector2)Script.GetControlProperty("TimerTextEntry", "position");
        }

        public Vector2 GetMessageWindowPosition(EODHeight height, EODTextTips tips, bool expanded)
        {
            var topLeftHeight = height;
            if(height == EODHeight.TallTall)
            {
                topLeftHeight = EODHeight.Tall;
            }
            var position = GetTopLeft(topLeftHeight);
            if(tips == EODTextTips.Long){
                position += (Vector2)Script.GetControlProperty("EODMsgWinLong", "position");
            }else{
                position += (Vector2)Script.GetControlProperty("EODMsgWinShort", "position");
            }

            if(height == EODHeight.TallTall && expanded)
            {
                position += (Vector2)Script.GetControlProperty("EODDoublePanelMsgOffset");
            }
            return position;
        }

        public Vector2 GetMessageWindowTextPosition(EODHeight height, bool expanded)
        {
            var topLeftHeight = height;
            if (height == EODHeight.TallTall)
            {
                topLeftHeight = EODHeight.Tall;
            }
            var position = GetTopLeft(topLeftHeight) + (Vector2)Script.GetControlProperty("MsgWinTextEntry", "position");
            if (height == EODHeight.TallTall && expanded)
            {
                position += (Vector2)Script.GetControlProperty("EODDoublePanelMsgOffset");
            }
            return position;
        }

        public Vector2 GetChromeOffset(EODHeight height)
        {
            switch (height)
            {
                case EODHeight.Tall:
                case EODHeight.TallTall:
                    return new Vector2(0, -20);
            }
            return Vector2.Zero;
        }

        public Vector2 HelpButtonPosition
        {
            get
            {
                return Baseline + (Vector2)Script.GetControlProperty("EODHelpButton", "position");
            }
        }

        public Vector2 EODButtonPosition
        {
            get
            {
                return Baseline + (Vector2)Script.GetControlProperty("EODButtonPosition");
            }
        }
    }
}
