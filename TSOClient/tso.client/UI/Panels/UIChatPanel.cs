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

        private List<UIChatBalloon> Labels;
        public List<Rectangle> InvalidAreas;
        private UILotControl Owner;
        private UIChatDialog HistoryDialog;
        private UIPropertyLog PropertyLog;

        private Color[] Colours = new Color[] {
            new Color(255, 255, 255),
            new Color(125, 255, 255),
            new Color(255, 125, 255),
            new Color(255, 255, 125),
            new Color(125, 125, 255),
            new Color(255, 125, 125),
            new Color(125, 255, 125),
            new Color(0, 255, 255),
            new Color(255, 255, 0)
        };

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

            HistoryDialog = new UIChatDialog();
            HistoryDialog.Position = new Vector2(20, 20);
            HistoryDialog.Visible = false;
            HistoryDialog.Opacity = 0.75f;
            HistoryDialog.OnSendMessage += SendMessage;
            this.Add(HistoryDialog);

            PropertyLog = new UIPropertyLog();
            PropertyLog.Position = new Vector2(400, 20);
            PropertyLog.Visible = false;
            PropertyLog.Opacity = 0.75f;
            this.Add(PropertyLog);
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
            message = message.Replace("\r\n", "");
            if (message != "" && Owner.ActiveEntity != null)
            {
                if (message[0] == '!')
                {
                    Owner.Cheats.SubmitCommand(message);
                }
                else
                {
                    vm.SendCommand(new VMNetChatCmd
                    {
                        ActorUID = Owner.ActiveEntity.PersistID,
                        Message = message
                    });
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
            if (!VM.UseWorld) return;

            var botRect = InvalidAreas[3];
            botRect.Y = GlobalSettings.Default.GraphicsHeight - ((Owner.PanelActive) ? 135 : 20);

            InvalidAreas[3] = botRect;
            var lastFocus = state.InputManager.GetFocus();
            if (HistoryDialog.Visible) TextBox.Visible = false;
            else
            {
                if (state.NewKeys.Contains(Keys.Enter) && (Owner.EODs.ActiveEOD == null ||
                        lastFocus == null || lastFocus == TextBox ||
                        lastFocus == HistoryDialog.ChatEntryTextEdit
                        ))
                {
                    if (!TextBox.Visible) TextBox.Clear();
                    else SendMessageElem(TextBox);
                    TextBox.Visible = !TextBox.Visible;

                    if (TextBox.Visible) state.InputManager.SetFocus(TextBox);
                    else if (lastFocus == TextBox) state.InputManager.SetFocus(null);
                }
            }
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
                if (HistoryDialog.ChatEntryTextEdit.CurrentText.Length < 1)
                    if (lastFocus == HistoryDialog.ChatEntryTextEdit)
                        state.InputManager.SetFocus(null);
                    else if (lastFocus == null)
                        state.InputManager.SetFocus(HistoryDialog.ChatEntryTextEdit);
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

            var avatars = vm.Entities.Where(x => (x is VMAvatar)).ToList();
            while (avatars.Count < Labels.Count)
            {
                Remove(Labels[Labels.Count - 1]);
                Labels[Labels.Count - 1].Dispose();
                Labels.RemoveAt(Labels.Count - 1);
            }
            while (avatars.Count > Labels.Count)
            { 
                var balloon = new UIChatBalloon(this);
                balloon.Color = Colours[Labels.Count % Colours.Length];
                AddAt(Children.Count - 2, balloon); //behind chat dialog and text box
                Labels.Add(balloon);
            }

            var myAvatar = vm.GetAvatarByPersist(vm.MyUID);
            var myIgnoring = ((VMTSOAvatarState)myAvatar?.TSOState)?.IgnoredAvatars ?? new HashSet<uint>();

            for (int i=0; i<Labels.Count; i++)
            {
                var label = Labels[i];
                var avatar = (VMAvatar)avatars[i];

                if (label.Message != avatar.Message)
                    label.SetNameMessage(avatar.Name, avatar.Message, avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.Gender)>0);
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

                label.TargetPt = ((avatar.WorldUI.GetScreenPos(vm.Context.World.State) + new Vector2(0, -45) / (1 << (3 - (int)vm.Context.World.State.Zoom)))
                   + off2) * world.PreciseZoom / FSOEnvironment.DPIScaleFactor;

            }
            base.Update(state);
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
        }
    }
}
