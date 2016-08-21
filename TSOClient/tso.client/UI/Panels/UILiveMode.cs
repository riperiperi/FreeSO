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

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Live Mode Panel
    /// </summary>
    public class UILiveMode : UIDestroyablePanel
    {
        public UIImage Background;
        public UIImage Divider;
        public UIPersonIcon Thumb;
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

        public Texture2D BackgroundEODImg { get; set; } //live mode with backgrounded eod
        public Texture2D EODPanelImg { get; set; }
        public Texture2D EODPanelTallImg { get; set; }
        public Texture2D EODDoublePanelTallImg { get; set; }
        
        public UIImage EODPanel { get; set; }
        public UIImage EODPanelTall { get; set; }
        public UIImage EODDoublePanelTall { get; set; }

        public UIImage EODButtonLayoutNone { get; set; }
        public UIImage EODButtonLayoutNoneTall { get; set; }
        public UIImage EODButtonLayoutOne { get; set; }
        public UIImage EODButtonLayoutOneTall { get; set; }
        public UIImage EODButtonLayoutTwo { get; set; }
        public UIImage EODButtonLayoutTwoTall { get; set; }

        public UIImage EODSubFullLength { get; set; }
        public UIImage EODSubFullLengthTall { get; set; }
        public UIImage EODSubMediumLength { get; set; }
        public UIImage EODSubMediumLengthTall { get; set; }
        public UIImage EODSubShortLength { get; set; }
        public UIImage EODSubShortLengthTall { get; set; }

        public UIImage EODMsgWinLong { get; set; }
        public UIImage EODMsgWinShort { get; set; }
        public UIImage EODTimer { get; set; }

        public Texture2D EODButtonImg { get; set; }

        public UIListBox MsgWinTextEntry { get; set; }
        public UITextEdit TimerTextEntry { get; set; }

        //normal stuff
        public UIButton MoodPanelButton;

        public UIButton PreviousPageButton { get; set; }
        public UIButton NextPageButton { get; set; }

        public UILabel MotivesLabel { get; set; }
        public UIImage PeopleListBg;

        public UIPersonGrid PersonGrid;

        UILotControl LotController;
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

        private Vector2 EODCloseBase;
        private Vector2 EODHelpBase;
        public Vector2 DefaultNextPagePos;

        public UIButton EODButton;
        public UIImage EODImage;
        public Texture2D DefaultBGImage;

        public UILiveMode (UILotControl lotController) {
            var small800 = (GlobalSettings.Default.GraphicsWidth < 1024) || FSOEnvironment.SmallScreen;
            var script = this.RenderScript("livepanel"+(small800?"":"1024")+".uis");
            Script = script;
            LotController = lotController;

            DefaultBGImage = GetTexture(small800 ? (ulong)0x000000D800000002 : (ulong)0x0000018300000002);
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

            PersonGrid = new UIPersonGrid(LotController.vm);
            Add(PersonGrid);
            PersonGrid.Position = new Vector2(409, 51);
            if (small800) {
                PersonGrid.Columns = 4;
                PersonGrid.DrawPage();
            }
            
            EODPanel = new UIImage(EODPanelImg);
            EODPanel.Y = 20;
            EODPanelTall = new UIImage(EODPanelTallImg);
            //EODDoublePanelTall = new UIImage(EODDoublePanelTallImg);

            AddAt(0, EODPanel);
            AddAt(0, EODPanelTall);
            //Add(EODDoublePanelTall);

            EODButtonLayoutNone = script.Create<UIImage>("EODButtonLayoutNone");
            EODButtonLayoutNoneTall = script.Create<UIImage>("EODButtonLayoutNoneTall");
            EODButtonLayoutOne = script.Create<UIImage>("EODButtonLayoutOne");
            EODButtonLayoutOneTall = script.Create<UIImage>("EODButtonLayoutOneTall");
            EODButtonLayoutTwo = script.Create<UIImage>("EODButtonLayoutTwo");
            EODButtonLayoutTwoTall = script.Create<UIImage>("EODButtonLayoutTwoTall");

            EODSubFullLength = script.Create<UIImage>("EODSubFullLength");
            EODSubFullLengthTall = script.Create<UIImage>("EODSubFullLengthTall");
            EODSubMediumLength = script.Create<UIImage>("EODSubMediumLength");
            EODSubMediumLengthTall = script.Create<UIImage>("EODSubMediumLengthTall");
            EODSubShortLength = script.Create<UIImage>("EODSubShortLength");
            EODSubShortLengthTall = script.Create<UIImage>("EODSubShortLengthTall");

            Add(EODButtonLayoutNone);
            Add(EODButtonLayoutNoneTall);
            Add(EODButtonLayoutOne);
            Add(EODButtonLayoutOneTall);
            Add(EODButtonLayoutTwo);
            Add(EODButtonLayoutTwoTall);

            Add(EODSubFullLength);
            Add(EODSubFullLengthTall);
            Add(EODSubMediumLength);
            Add(EODSubMediumLengthTall);
            Add(EODSubShortLength);
            Add(EODSubShortLengthTall);

            EODMsgWinLong = script.Create<UIImage>("EODMsgWinLong");
            EODMsgWinShort = script.Create<UIImage>("EODMsgWinShort");
            EODTimer = script.Create<UIImage>("EODTimer");

            AddAt(0, EODTimer);
            AddAt(0, EODMsgWinLong);
            AddAt(0, EODMsgWinShort);

            EODButton = new UIButton(EODButtonImg);
            Add(EODButton);
            EODButton.OnButtonClick += EODToggle;
            EODImage = script.Create<UIImage>("EODButtonImageSize");
            Add(EODImage);

            NextPageButton.OnButtonClick += (UIElement btn) => { PersonGrid.NextPage(); };
            DefaultNextPagePos = NextPageButton.Position;
            PreviousPageButton.OnButtonClick += (UIElement btn) => { PersonGrid.PreviousPage(); };

            MsgWinTextEntry.Items.Add(new UIListBoxItem("", ""));

            EODCloseBase = EODCloseButton.Position;
            EODHelpBase = EODHelpButton.Position;
            SetInEOD(null, null);
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
            bool eodPresent = (options != null);
            bool inEOD = eodPresent && !HideEOD;
            if (ActiveEOD != null) Remove(ActiveEOD);

            LastEODConfig = options;
            ActiveEOD = eod;
            EODHelpButton.Visible = inEOD;
            EODCloseButton.Visible = inEOD;
            EODExpandButton.Visible = false; //todo
            EODContractButton.Visible = false;
            EODButton.Visible = eodPresent;

            bool tall = inEOD && options.Height == EODHeight.Tall;
            
            EODPanel.Visible = inEOD && !tall;
            EODPanelTall.Visible = inEOD && tall;

            EODButtonLayoutNone.Visible = inEOD && !tall && options.Buttons == 0;
            EODButtonLayoutNoneTall.Visible = inEOD && tall && options.Buttons == 0;
            EODButtonLayoutOne.Visible = inEOD && !tall && options.Buttons == 1;
            EODButtonLayoutOneTall.Visible = inEOD && tall && options.Buttons == 1;
            EODButtonLayoutTwo.Visible = inEOD && !tall && options.Buttons == 2;
            EODButtonLayoutTwoTall.Visible = inEOD && tall && options.Buttons == 2;

            EODSubFullLength.Visible = inEOD && !tall && options.Length == EODLength.Full;
            EODSubFullLengthTall.Visible = inEOD && tall && options.Length == EODLength.Full;
            EODSubMediumLength.Visible = inEOD && !tall && options.Length == EODLength.Medium;
            EODSubMediumLengthTall.Visible = inEOD && tall && options.Length == EODLength.Medium;
            EODSubShortLength.Visible = inEOD && !tall && options.Length == EODLength.Short;
            EODSubShortLengthTall.Visible = inEOD && tall && options.Length == EODLength.Short;

            EODMsgWinLong.Visible = inEOD && options.Tips == EODTextTips.Long;
            EODMsgWinShort.Visible = inEOD && options.Tips == EODTextTips.Short;
            EODTimer.Visible = inEOD && options.Timer == EODs.EODTimer.Normal;

            MsgWinTextEntry.Visible = inEOD && options.Tips != EODTextTips.None;
            TimerTextEntry.Visible = inEOD && options.Timer != EODs.EODTimer.None;

            MoodPanelButton.Position = (eodPresent) ? new Vector2(20, 7) : new Vector2(31, 63);
            if (EODImage.Texture != null) EODImage.Texture.Dispose();
            EODImage.Texture = null;

            if (inEOD)
            {
                Add(ActiveEOD);
                ActiveEOD.Position = new Vector2(120, 0);
            }

            if (eodPresent)
            {
                Vector2 TopXOffset = new Vector2();
                Vector2 MoodButtonOff = new Vector2();
                var offHeight = options.Height;
                if (HideEOD) offHeight = EODHeight.Normal;
                switch (offHeight)
                {
                    case EODHeight.Normal:
                        TopXOffset = (Vector2)Script.GetControlProperty("EODActiveOffset");
                        MoodButtonOff = TopXOffset;
                        break;
                    case EODHeight.Tall:
                        TopXOffset = (Vector2)Script.GetControlProperty("EODActiveOffset");
                        MoodButtonOff = (Vector2)Script.GetControlProperty("EODActiveOffsetTall");
                        break;
                }
                MoodPanelButton.Position += MoodButtonOff;
                EODCloseButton.Position = EODCloseBase + TopXOffset;
                EODHelpButton.Position = EODHelpBase + MoodButtonOff;

                EODButton.Position = (Vector2)Script.GetControlProperty("EODButtonPosition") + MoodButtonOff;

                var ava = SelectedAvatar;
                if (ava != null)
                {
                    var blockInfo = ava.Thread.BlockingState;
                    if (blockInfo is VMEODPluginThreadState)
                    {
                        var eodInfo = (VMEODPluginThreadState)blockInfo;
                        var entity = LotController.vm.GetObjectById(eodInfo.ObjectID);
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
            }

            this.Y = (inEOD && options.Height == EODHeight.Tall) ? 41: 61;

            Divider.Visible = !inEOD;
            MotiveDisplay.Visible = !inEOD;
            PersonGrid.Visible = !inEOD;
            MotivesLabel.Visible = !inEOD;
            PeopleListBg.Visible = !inEOD;
            PreviousPageButton.Visible = !inEOD;
            NextPageButton.Visible = !inEOD;
            Background.Visible = !inEOD;

            PersonGrid.Columns = (eodPresent || (GlobalSettings.Default.GraphicsWidth < 1024)) ?4:9;
            PersonGrid.DrawPage();
            PeopleListBg.Texture = (eodPresent && PeopleListEODBackgroundImg != null) ? PeopleListEODBackgroundImg : PeopleListBackgroundImg;
            PeopleListBg.SetSize(PeopleListBg.Texture.Width, PeopleListBg.Texture.Height);

            var small800 = (GlobalSettings.Default.GraphicsWidth < 1024) || FSOEnvironment.SmallScreen;
            NextPageButton.Position = (eodPresent && !small800) ? (Vector2)Script.GetControlProperty("NextPageEODButton") : DefaultNextPagePos;
            Background.Texture = (eodPresent) ? BackgroundEODImg : DefaultBGImage;
            Background.SetSize(Background.Texture.Width, Background.Texture.Height);

            UpdateThumbPosition();
        }

        public override void Destroy()
        {
            //nothing to detach from here
        }

        public void UpdateThumbPosition()
        {
            if (Thumb != null) Thumb.Position = MoodPanelButton.Position + new Vector2(33, 10);
        }

        public override void Update(FSO.Common.Rendering.Framework.Model.UpdateState state)
        {
            base.Update(state);
            if (SelectedAvatar != null)
            {
                if (SelectedAvatar != LastSelected)
                {
                    if (Thumb != null) Remove(Thumb);
                    Thumb = new UIPersonIcon(SelectedAvatar, LotController.vm, false);
                    UpdateThumbPosition();
                    Add(Thumb);
                    LastSelected = SelectedAvatar;
                }
                
                UpdateMotives();
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
}
