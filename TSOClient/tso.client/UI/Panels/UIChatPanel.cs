/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.LotView.Components;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common.Utils;
using FSO.SimAntics.NetPlay.Model;
using FSO.Common;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.Common.Rendering.Framework.IO;
using FSO.SimAntics.Model;

namespace FSO.Client.UI.Panels
{
    public class UIChatPanel : UIContainer
    {
        private VM vm;
        private TextStyle Style;
        private UITextBox TextBox;

        private Color m_SelectionFillColor;
        public Color SelectionFillColor
        {
            get
            {
                return m_SelectionFillColor;
            }
            set
            {
                m_SelectionFillColor = value;
            }
        }

        public List<UIChatBalloon> Labels;
        public List<Rectangle> InvalidAreas;
        private UILotControl Owner;
        private UIChatDialog HistoryDialog;
        private UIPropertyLog PropertyLog;
        private InputManager Inputs;

        public int ActiveChannel = 0;

        public UIChatPanel(VM vm, UILotControl owner)
        {
            this.vm = vm;
            this.Owner = owner;

            if (FSOEnvironment.SoftwareKeyboard)
            {
                //need a button to initiate chat history
                var btn = new UIButton();
                btn.Caption = "Chat";
                btn.Position = new Vector2(10, 10);
                btn.OnButtonClick += (state) =>
                {
                    HistoryDialog.Visible = !HistoryDialog.Visible;
                };
                Add(btn);
                
            }

            Style = TextStyle.DefaultTitle.Clone();
            Style.Size = 16;
            Style.Shadow = true;
            Labels = new List<UIChatBalloon>();

            TextBox = new UITextBox();
            TextBox.SetBackgroundTexture(null, 0, 0, 0, 0);
            TextBox.Visible = false;
            Add(TextBox);
            TextBox.Position = new Vector2(25, 25);
            TextBox.SetSize(GlobalSettings.Default.GraphicsWidth - 50, 25);

            var emojis = new UIEmojiSuggestions(TextBox);
            Add(emojis);
            emojis.Parent = this;

            TextBox.OnEnterPress += TextBox_OnEnterPress;

            SelectionFillColor = new Color(0, 25, 70);

            //-- populate invalid areas --
            //chat bubbles will be pushed out of these areas
            //when this happens, they will also begin displaying the name of the speaking avatar.

            InvalidAreas = new List<Rectangle>();
            InvalidAreas.Add(new Rectangle(-100000, -100000, 100020, 200000 + GlobalSettings.Default.GraphicsHeight)); //left
            InvalidAreas.Add(new Rectangle(-100000, -100000, 200000 + GlobalSettings.Default.GraphicsWidth, 100020)); //top
            InvalidAreas.Add(new Rectangle(GlobalSettings.Default.GraphicsWidth-20, -100000, 100020, 200000 + GlobalSettings.Default.GraphicsHeight)); //right
            InvalidAreas.Add(new Rectangle(-100000, GlobalSettings.Default.GraphicsHeight - 20, 200000 +GlobalSettings.Default.GraphicsWidth, 100020)); //bottom
            InvalidAreas.Add(new Rectangle(-100000, GlobalSettings.Default.GraphicsHeight - 230, 100230, 100230)); //ucp

            HistoryDialog = new UIChatDialog(owner);
            HistoryDialog.Position = new Vector2(GlobalSettings.Default.ChatLocationX, GlobalSettings.Default.ChatLocationY);
            HistoryDialog.Visible = true;
            HistoryDialog.Opacity = 0.8f;
            HistoryDialog.OnSendMessage += SendMessage;
            this.Add(HistoryDialog);

            PropertyLog = new UIPropertyLog();
            PropertyLog.Position = new Vector2(400, 20);
            PropertyLog.Visible = false;
            PropertyLog.Opacity = 0.8f;
            this.Add(PropertyLog);
        }

        public void SetVisitorCount(int visitors)
        {
            HistoryDialog.Visitors = visitors;
        }

        private bool JustHidTextbox;
        private void TextBox_OnEnterPress(UIElement element)
        {
            if (TextBox.EventSuppressed) return;
            SendMessageElem(TextBox);
            TextBox.Visible = !TextBox.Visible;

            Inputs.SetFocus(null);
            JustHidTextbox = true;
        }

        public override void GameResized()
        {
            base.GameResized();
            InvalidAreas = new List<Rectangle>();
            InvalidAreas.Add(new Rectangle(-100000, -100000, 100020, 200000 + GlobalSettings.Default.GraphicsHeight)); //left
            InvalidAreas.Add(new Rectangle(-100000, -100000, 200000 + GlobalSettings.Default.GraphicsWidth, 100020)); //top
            InvalidAreas.Add(new Rectangle(GlobalSettings.Default.GraphicsWidth - 20, -100000, 100020, 200000 + GlobalSettings.Default.GraphicsHeight)); //right
            InvalidAreas.Add(new Rectangle(-100000, GlobalSettings.Default.GraphicsHeight - 20, 200000 + GlobalSettings.Default.GraphicsWidth, 100020)); //bottom
            InvalidAreas.Add(new Rectangle(-100000, GlobalSettings.Default.GraphicsHeight - 230, 100230, 100230)); //ucp
            TextBox.SetSize(GlobalSettings.Default.GraphicsWidth - 50, 25);
        }

        private void SendMessage(string message)
        {
            if (GlobalSettings.Default.ChatOnlyEmoji > 0 && message != "")
            {
                message = GameFacade.Emojis.EmojiOnly(message, GlobalSettings.Default.ChatOnlyEmoji);
                if (message == "")
                {
                    HistoryDialog.ReceiveEvent
                        (new VMChatEvent(null, VMChatEventType.Generic,
                        ":no_good_man: :keyboard: :no_entry_sign: :arrow_forward: :grinning: :ok_hand: "));
                }
            }
            message = message.Replace("\n", "");
            if (message != "" && Owner.ActiveEntity != null)
            {
                if ((Owner.ActiveEntity.GetValue(VMStackObjectVariable.FlagField2) & (short)VMEntityFlags2.FSODisableChat) != 0)
                {
                    HistoryDialog.ReceiveEvent(new VMChatEvent(null, VMChatEventType.Generic, "You can't speak right now."));
                }
                else
                {
                    if (message[0] == '!')
                    {
                        Owner.Cheats.SubmitCommand(message);
                    }
                    else
                    {
                        if (message == "/trace")
                        {
                            vm.UseSchedule = false;
                            vm.Trace = new SimAntics.Engine.Debug.VMSyncTrace();
                        }
                        vm.SendCommand(new VMNetChatCmd
                        {
                            ActorUID = Owner.ActiveEntity.PersistID,
                            Message = message,
                            ChannelID = (byte)ActiveChannel
                        });
                    }
                }
            }
        }

        private void SendMessageElem(UIElement element)
        {
            string message = TextBox.CurrentText;
            SendMessage(message);
            TextBox.Clear();
        }

        public List<Rectangle> GetInvalid(UIChatBalloon label)
        {
            var to = Labels.IndexOf(label);
            var copy = new List<Rectangle>(InvalidAreas);
            for (int i=0; i<to; i++)
            {
                if (Labels[i].Visible && Labels[i].Alpha > 0) copy.Add(Labels[i].DisplayRect);
            }
            return copy;
        }

        public override void Update(UpdateState state)
        {
            if (this.HistoryDialog.Opacity != GlobalSettings.Default.ChatWindowsOpacity)
            {
                var opacity = GlobalSettings.Default.ChatWindowsOpacity;
                this.HistoryDialog.Opacity = opacity;
                this.PropertyLog.Opacity = opacity;
            }
            Inputs = state.InputManager;
            if (!VM.UseWorld || vm.FSOVAsyncLoading) return;

            var botRect = InvalidAreas[3];
            botRect.Y = GlobalSettings.Default.GraphicsHeight - ((Owner.PanelActive) ? 135 : 20);

            InvalidAreas[3] = botRect;

            var avatars = vm.Context.ObjectQueries.Avatars;
            while (avatars.Count < Labels.Count)
            {
                Remove(Labels[Labels.Count - 1]);
                Labels[Labels.Count - 1].Dispose();
                Labels.RemoveAt(Labels.Count - 1);
            }
            while (avatars.Count > Labels.Count)
            {
                var balloon = new UIChatBalloon(this);
                AddAt(Children.Count - 2, balloon); //behind chat dialog and text box
                Labels.Add(balloon);
            }

            var myAvatar = vm.GetAvatarByPersist(vm.MyUID);
            var myIgnoring = ((VMTSOAvatarState)myAvatar?.TSOState)?.IgnoredAvatars ?? new HashSet<uint>();

            for (int i = 0; i < Labels.Count; i++)
            {
                var label = Labels[i];
                var avatar = (VMAvatar)avatars[i];
                var tstate = ((VMTSOAvatarState)avatar.TSOState);

                if (label.Message != avatar.Message)
                    label.SetNameMessage(avatar);
                if (label.Color != tstate.ChatColor)
                    label.Color = tstate.ChatColor;
                if (myIgnoring.Contains(avatar.PersistID))
                {
                    label.Alpha = 0;
                }
                else
                {
                    if (avatar.MessageTimeout < 30)
                    {
                        label.FadeTime = avatar.MessageTimeout / 3;
                        label.Alpha = avatar.MessageTimeout / 30f;
                    }
                    else
                    {
                        if (label.FadeTime < 10) label.FadeTime++;
                        label.Alpha = label.FadeTime / 10f;
                    }
                }

                var world = vm.Context.World.State;
                var off2 = new Vector2(world.WorldSpace.WorldPxWidth, world.WorldSpace.WorldPxHeight);
                off2 = (off2 / world.PreciseZoom - off2) / 2;

                label.TargetPt = ((ZoomCorrect(avatar.WorldUI.GetScreenPos(vm.Context.World.State)) + new Vector2(0, -45) / (1 << (3 - (int)vm.Context.World.State.Zoom)))
                   + off2) * world.PreciseZoom / FSOEnvironment.DPIScaleFactor;

            }
            base.Update(state);

            var lastFocus = state.InputManager.GetFocus();
            if (HistoryDialog.Visible) TextBox.Visible = false;
            else
            {
                if (state.NewKeys.Contains(Keys.Enter) && (
                        lastFocus == null || lastFocus == TextBox ||
                        lastFocus == HistoryDialog.ChatEntryTextEdit
                        ))
                {
                    if (!TextBox.Visible && !JustHidTextbox) {
                        TextBox.Clear();
                        TextBox.Visible = !TextBox.Visible;

                        if (TextBox.Visible) state.InputManager.SetFocus(TextBox);
                        else if (lastFocus == TextBox) state.InputManager.SetFocus(null);
                    }
                }
            }
            JustHidTextbox = false;
            if (state.NewKeys.Contains(Keys.Escape))
            {
                if (TextBox.Visible)
                {
                    TextBox.Clear();
                    TextBox.Visible = !TextBox.Visible;
                }
                if (HistoryDialog.Visible)
                    if (HistoryDialog.ChatEntryTextEdit.CurrentText.Length > 0)
                    {
                        HistoryDialog.ChatEntryTextEdit.CurrentText = "";
                        HistoryDialog.OKButton.Disabled = true;
                    }
                    else if (lastFocus == HistoryDialog.ChatEntryTextEdit)
                        state.InputManager.SetFocus(null);
            }
            if (state.NewKeys.Contains(Keys.Enter) && HistoryDialog.Visible)
            {
                if (lastFocus == null)
                    state.InputManager.SetFocus(HistoryDialog.ChatEntryTextEdit);
                    /*
    if (HistoryDialog.ChatEntryTextEdit.CurrentText.Length < 1)
        if (lastFocus == HistoryDialog.ChatEntryTextEdit)
            state.InputManager.SetFocus(null);
        else if (lastFocus == null)
            state.InputManager.SetFocus(HistoryDialog.ChatEntryTextEdit);
            */
            }
            if (state.NewKeys.Contains(Keys.H) && state.CtrlDown)
            {
                HistoryDialog.Visible = !HistoryDialog.Visible;
                if (HistoryDialog.Visible) state.InputManager.SetFocus(HistoryDialog.ChatEntryTextEdit);
                else state.InputManager.SetFocus(null);
            }

            if (state.NewKeys.Contains(Keys.OemPlus) && state.CtrlDown && HistoryDialog.Visible) {
                HistoryDialog.ResizeChatDialogByDelta(1);
            }

            if (state.NewKeys.Contains(Keys.OemMinus) && state.CtrlDown && HistoryDialog.Visible) {
                HistoryDialog.ResizeChatDialogByDelta(-1);
            }

            if (state.NewKeys.Contains(Keys.P) && state.CtrlDown)
            {
                PropertyLog.Visible = !PropertyLog.Visible;
            }
        }

        private Vector2 ZoomCorrect(Vector2 vec)
        {
            var screenMiddle = new Vector2(
            (int)(GameFacade.Screens.CurrentUIScreen.ScreenWidth / (2 / FSOEnvironment.DPIScaleFactor)),
            (int)(GameFacade.Screens.CurrentUIScreen.ScreenHeight / (2 / FSOEnvironment.DPIScaleFactor))
            );

            return ((vec - screenMiddle) * Owner.BBScale) + screenMiddle;
        }

        public override void Draw(UISpriteBatch batch)
        {
            var whitePx = TextureGenerator.GetPxWhite(batch.GraphicsDevice);
            if (TextBox.Visible) DrawLocalTexture(batch, whitePx, null, TextBox.Position, TextBox.Size, new Color(0x00, 0x33, 0x66) * 0.75f);
            base.Draw(batch);
        }

        public void ReceiveEvent(VMChatEvent evt)
        {
            if (evt.Type == VMChatEventType.Arch) PropertyLog.ReceiveEvent(evt);
            else
            {
                var myAvatar = vm.GetAvatarByPersist(vm.MyUID);
                var myIgnoring = ((VMTSOAvatarState)myAvatar?.TSOState)?.IgnoredAvatars ?? new HashSet<uint>();
                if (!myIgnoring.Contains(evt.SenderUID))
                {
                    HistoryDialog.ReceiveEvent(evt);
                }
            }
        }

        public void SetLotName(string name)
        {
            HistoryDialog.LotName = name;
            HistoryDialog.RenderTitle();
        }
    }
}
