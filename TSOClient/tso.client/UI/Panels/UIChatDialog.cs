using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.SimAntics.NetPlay.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.Client.UI.Panels.Chat;

namespace FSO.Client.UI.Panels
{
    public class UIChatDialog : UIDialog
    {
        public event UISendMessageDelegate OnSendMessage;

        public UISlider ChatHistorySlider { get; set; }
        public UIButton ChatHistoryScrollUpButton { get; set; }
        public UIButton ChatHistoryScrollDownButton { get; set; }
        public UITextEdit ChatEntryTextEdit { get; set; }
        public UITextEdit ChatHistoryText { get; set; }
        public UIImage ChatEntryBackground { get; set; }
        public UIImage ChatHistoryBackground { get; set; }

        private List<VMChatEvent> History;

        public byte ShowChannels = 255;

        private int chatSize = 8;

        public int Visitors;
        public string LotName = "Test Lot";
        public UILotControl Owner;
        public UIChatCategoryList Categories;
        public bool LastCategories;

        private ITTSContext TTSContext;

        public UIChatDialog(UILotControl owner)
            : base(UIDialogStyle.Standard | UIDialogStyle.OK | UIDialogStyle.Close, false)
        {
            //todo: this dialog is resizable. The elements use offests from each side to size and position themselves.
            //right now we're just using positions.

            CloseButton.Tooltip = GameFacade.Strings.GetString("f113", "43");
            Owner = owner;
            History = new List<VMChatEvent>();

            this.RenderScript("chatdialog.uis");
            this.SetSize(400, 255);

            this.Caption = "Property Chat (?) - ???";

            ChatEntryBackground = new UIImage(GetTexture((ulong)0x7A400000001)).With9Slice(13, 13, 13, 13);
            ChatEntryBackground.Position = new Vector2(25, 211);
            ChatEntryBackground.SetSize(323, 26);
            AddAt(5, ChatEntryBackground);

            ChatHistoryBackground = new UIImage(GetTexture((ulong)0x7A400000001)).With9Slice(13, 13, 13, 13);
            ChatHistoryBackground.Position = new Vector2(19, 39);
            ChatHistoryBackground.SetSize(341, 166);
            AddAt(5, ChatHistoryBackground);

            ChatHistorySlider.AttachButtons(ChatHistoryScrollUpButton, ChatHistoryScrollDownButton, 1);
            ChatHistoryText.AttachSlider(ChatHistorySlider);

            ChatHistoryText.Position = new Vector2(29, 47);
            ChatHistoryText.BBCodeEnabled = true;
            var histStyle = ChatHistoryText.TextStyle.Clone();
            histStyle.Size = chatSize;
            ChatHistoryText.Size = new Vector2(ChatHistoryBackground.Size.X - 19, ChatHistoryBackground.Size.Y - 16);
            ChatHistoryText.MaxLines = 10;
            ChatHistoryText.TextStyle = histStyle;

            ChatEntryTextEdit.OnChange += ChatEntryTextEdit_OnChange;
            ChatEntryTextEdit.Position = new Vector2(38, 216);
            ChatEntryTextEdit.Size = new Vector2(295, 17);

            OKButton.Disabled = true;
            OKButton.OnButtonClick += SendMessage;

            CloseButton.OnButtonClick += CloseButton_OnButtonClick;

            var emojis = new UIEmojiSuggestions(ChatEntryTextEdit);
            DynamicOverlay.Add(emojis);
            emojis.Parent = this;
            ChatEntryTextEdit.OnEnterPress += SendMessageEnter;

            Background.ListenForMouse(new UIMouseEvent(DragMouseEvents));

            Categories = new UIChatCategoryList(this);
            Categories.Position = new Vector2(31, 29);
            Add(Categories);
        }

        private ITTSContext GetOrCreateTTS()
        {
            if (TTSContext == null && ITTSContext.Provider != null)
            {
                TTSContext = ITTSContext.Provider();
            }
            return TTSContext;
        }

        private void ChangeSizeTo(Vector2 size)
        {
            if (Size == size && LastCategories == Categories.HasButtons) return;

            var wasAtBottom = ChatHistoryText.VerticalScrollPosition == ChatHistoryText.VerticalScrollMax;

            var delta = size.ToPoint().ToVector2() - Size;

            if (LastCategories != Categories.HasButtons)
            {
                var yDelta = Categories.HasButtons ? 10 : -10;
                foreach (var child in Children)
                {
                    if (child != Categories && child != Background)
                        child.Y += yDelta;
                }
                LastCategories = Categories.HasButtons;
                delta.Y -= yDelta;
            }

            this.SetSize((int)size.X, (int)size.Y);

            ChatHistoryBackground.SetSize(ChatHistoryBackground.Size.X + delta.X, ChatHistoryBackground.Size.Y + delta.Y);
            ChatHistoryText.SetSize(ChatHistoryBackground.Size.X - 19, ChatHistoryBackground.Size.Y - 16);

            ChatHistorySlider.Position = new Vector2(ChatHistorySlider.Position.X + delta.X, ChatHistorySlider.Position.Y);
            ChatHistorySlider.SetSize(ChatHistorySlider.Size.X, ChatHistoryBackground.Size.Y - 26);
            ChatHistoryScrollUpButton.Position = new Vector2(ChatHistoryScrollUpButton.Position.X + delta.X, ChatHistoryScrollUpButton.Position.Y);
            ChatHistoryScrollDownButton.Position = new Vector2(ChatHistoryScrollDownButton.Position.X + delta.X, ChatHistoryScrollDownButton.Position.Y + delta.Y);

            ChatEntryTextEdit.Position = new Vector2(ChatEntryTextEdit.Position.X, ChatEntryTextEdit.Position.Y + delta.Y);
            ChatEntryTextEdit.SetSize(ChatEntryTextEdit.Size.X + delta.X, ChatEntryTextEdit.Size.Y);
            ChatEntryBackground.Position = new Vector2(ChatEntryBackground.Position.X, ChatEntryBackground.Position.Y + delta.Y);
            ChatEntryBackground.SetSize(ChatEntryBackground.Size.X + delta.X, ChatEntryBackground.Size.Y);

            ChatEntryTextEdit.ComputeDrawingCommands();
            ChatHistoryText.ComputeDrawingCommands();

            if (wasAtBottom) ChatHistoryText.VerticalScrollPosition = ChatHistoryText.VerticalScrollMax;
        }

        /// <summary>
        /// Handle mouse events for dragging and resizing
        /// </summary>
        /// <param name="evt"></param>
        private void DragMouseEvents(UIMouseEventType evt, UpdateState state)
        {
            switch (evt)
            {
                case UIMouseEventType.MouseDown:
                    /** Start drag **/
                    var position = this.GetMousePosition(state.MouseState);

                    m_dragOffset = position;
                    m_doResizeX = position.X > Width - 20;
                    m_doResizeY = position.Y > Height - 20;
                    m_doDrag = !(m_doResizeX || m_doResizeY);
                    if (!m_doDrag)
                        m_dragOffset = Size - m_dragOffset;
                    break;

                case UIMouseEventType.MouseUp:
                    /** Stop drag **/
                    m_doDrag = false;
                    m_doResizeX = false;
                    m_doResizeY = false;
                    break;
            }
        }

        private void CloseButton_OnButtonClick(UIElement button)
        {
            //hide self.
            Visible = false;
        }

        private void ChatEntryTextEdit_OnChange(UIElement TextEdit)
        {
            UITextEdit edit = (UITextEdit)TextEdit;
            OKButton.Disabled = (edit.CurrentText.Length == 0);
        }

        private void SendMessageEnter(UIElement element)
        {
            //remove newline first
            //ChatEntryTextEdit.CurrentText = ChatEntryTextEdit.CurrentText.Substring(0, ChatEntryTextEdit.CurrentText.Length - 2);
            if (ChatEntryTextEdit.EventSuppressed) return;
            SendMessage(this);
        }

        private void SendMessage(UIElement button)
        {
            OKButton.Disabled = true;
            if (ChatEntryTextEdit.CurrentText.Length == 0) return;

            if (OnSendMessage != null) OnSendMessage(ChatEntryTextEdit.CurrentText); 
            ChatEntryTextEdit.CurrentText = "";
        }

        private VMTSOChatChannel GetChannelInfo(int id)
        {
            var channel = Owner.vm.TSOState.ChatChannels.FirstOrDefault(x => x.ID == id);
            if (id == 7) channel = VMTSOChatChannel.AdminChannel;
            else if (channel == null) channel = VMTSOChatChannel.MainChannel;
            return channel;
        }

        public void ReceiveEvent(VMChatEvent evt)
        {
            History.Add(evt);

            //play TTS for this event?
            if (evt.Type == VMChatEventType.Message || evt.Type == VMChatEventType.MessageMe) {
                if (GlobalSettings.Default.ChatOnlyEmoji)
                {
                    evt.Text[1] = GameFacade.Emojis.EmojiOnly(evt.Text[1]);
                }

                var ttsmode = GlobalSettings.Default.TTSMode;
                if (ttsmode > 0)
                {
                    if (ttsmode == 2 || (GetChannelInfo(evt.ChannelID).Flags & VMTSOChatChannelFlags.EnableTTS) > 0)
                    {
                        var avatar = Owner.vm.GetAvatarByPersist(evt.SenderUID);
                        if (avatar != null)
                        {
                            var tts = GetOrCreateTTS();
                            var gender = avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.Gender) > 0;
                            tts?.Speak(evt.Text[1].Replace('_', ' '), gender, ((VMTSOAvatarState)avatar.TSOState).ChatTTSPitch);
                        }
                    }
                }
            }

            if (History.Count > 100) History.RemoveAt(0);
            RenderEvents();
        }

        private bool m_doDrag;
        private bool m_doResizeX;
        private bool m_doResizeY;
        private Vector2 m_dragOffset;

        public override void Update(UpdateState state)
        {
            if (ShowChannels == 255 && Owner.vm.Ready)
            {
                //show default channels
                ShowChannels = 0x01; //show main by default
                foreach (var channel in Owner.vm.TSOState.ChatChannels)
                {
                    if ((channel.Flags & VMTSOChatChannelFlags.ShowByDefault) > 0) ShowChannels |= (byte)(1 << channel.ID);
                }
                RenderEvents();
            }
            if (m_doDrag)
            {
                /** Drag the dialog box **/
                var position = Parent.GetMousePosition(state.MouseState);

                if ((position.X - m_dragOffset.X) < (GlobalSettings.Default.GraphicsWidth - m_DragTolerance) && (position.X - m_dragOffset.X) > 0)
                    this.X = position.X - m_dragOffset.X;
                if ((position.Y - m_dragOffset.Y) < (GlobalSettings.Default.GraphicsHeight - m_DragTolerance) && (position.Y - m_dragOffset.Y) > 0)
                    this.Y = position.Y - m_dragOffset.Y;
            } else if (m_doResizeX || m_doResizeY)
            {
                var position = GetMousePosition(state.MouseState);
                var newSize = Size;

                if (m_doResizeX)
                    newSize.X = (position.X + m_dragOffset.X);
                if (m_doResizeY)
                    newSize.Y = (position.Y + m_dragOffset.Y);
                newSize.X = Math.Max(newSize.X, 400);
                newSize.Y = Math.Max(newSize.Y, 255);

                ChangeSizeTo(newSize);
            }
            if (LastCategories != Categories.HasButtons) ChangeSizeTo(Size);
            base.Update(state);
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            base.Draw(batch);
        }

        public void ResizeChatDialogByDelta(int delta)
        {
            if (chatSize + delta < 7 || chatSize + delta > 11)
            {
                return;
            }
            chatSize += delta;

            var histStyle = ChatHistoryText.TextStyle.Clone();
            histStyle.Size = chatSize;
            ChatHistoryText.TextStyle = histStyle;
            ChatEntryTextEdit.ComputeDrawingCommands();
            ChatHistoryText.ComputeDrawingCommands();
            Invalidate();
        }

        public void RenderTitle()
        {
            Caption = GameFacade.Strings.GetString("261", "1", new string[] { Visitors.ToString(), LotName });
        }

        public void RenderEvents()
        {

            StringBuilder txt = new StringBuilder();
            bool first = true;
            var channels = Owner.vm.TSOState.ChatChannels;
            foreach (var evt in History)
            {
                if (evt.Type == VMChatEventType.Message || evt.Type == VMChatEventType.MessageMe)
                {
                    //can be filtered out if we're not viewing that channel
                    if ((ShowChannels & (1 << evt.ChannelID)) == 0) continue;
                    //else let's try pass our channel info
                    if (evt.ChannelID == 7) evt.Channel = VMTSOChatChannel.AdminChannel;
                    else if (evt.ChannelID > 0) evt.Channel = channels.FirstOrDefault(x => x.ID == evt.ChannelID);
                }
                if (!first) txt.Append("\n");
                first = false;
                txt.Append(RenderEvent(evt));
            }

            bool scroll = Math.Abs(ChatHistoryText.VerticalScrollMax - ChatHistoryText.VerticalScrollPosition) < 2 ;
            ChatHistoryText.CurrentText = txt.ToString();
            ChatHistoryText.ComputeDrawingCommands();
            if (scroll)
            {
                ChatHistoryText.VerticalScrollPosition = ChatHistoryText.VerticalScrollMax;
            }
            else
            {
                ChatHistoryText.VerticalScrollPosition = ChatHistoryText.VerticalScrollPosition;
            }
        }

        public string SanitizeBB(string input)
        {
            return input.Replace("[", "\\[");
        }

        public string CleanUserMessage(string msg, VMChatEvent evt)
        {
            var sanitary = GameFacade.Emojis.EmojiToBB(SanitizeBB(msg));
            if (evt.Channel != null)
                return evt.Channel.TextColorString + sanitary + "[/color]";
            else
                return sanitary;
        }

        public string RenderEvent(VMChatEvent evt)
        {
            var colorBefore = "[color=lightgray]";
            var avatarColor = "[color=#" + evt.Color.R.ToString("x2") + evt.Color.G.ToString("x2") + evt.Color.B.ToString("x2") + "][s]";
            var colorAfter = "[/s][/color]";
            var timestamp = evt.Timestamp;
            var showTimestamp = GlobalSettings.Default.ChatShowTimestamp;
            var avatar = avatarColor + evt.Text[0] + colorAfter; //avatar names cannot normally contain bbcode
            switch (evt.Type)
            {
                case VMChatEventType.Message:
                    return ((showTimestamp) ? SanitizeBB("[" + timestamp + "] ") : "") + colorBefore + ((evt.Channel == null) ? "" : ("(" + evt.Channel.Name + ") "))
                        + GameFacade.Strings.GetString("261", "8").Replace("%", avatar)
                        + colorAfter + CleanUserMessage(evt.Text[1], evt);
                case VMChatEventType.MessageMe:
                    return ((showTimestamp) ? SanitizeBB("[" + timestamp + "] ") : "") + colorBefore + ((evt.Channel == null) ? "" : ("(" + evt.Channel.Name + ") "))
                        + GameFacade.Strings.GetString("261", "9")
                        + colorAfter + CleanUserMessage(evt.Text[1], evt);
                case VMChatEventType.Join:
                    return ((showTimestamp) ? SanitizeBB("[" + timestamp + "] ") : "") + colorBefore + GameFacade.Strings.GetString("261", "6").Replace("%", avatar) + colorAfter;
                case VMChatEventType.Leave:
                    return ((showTimestamp) ? SanitizeBB("[" + timestamp + "] ") : "") + colorBefore + GameFacade.Strings.GetString("261", "7").Replace("%", avatar) + colorAfter;
                case VMChatEventType.Arch:
                    return ((showTimestamp) ? SanitizeBB("[" + timestamp + "] ") : "") + colorBefore + "<" + avatar + " (" + evt.Text[1] + ")" + "> " + evt.Text[2] + colorAfter;
                case VMChatEventType.Generic:
                case VMChatEventType.Debug:
                    return ((showTimestamp) ? SanitizeBB("[" + timestamp + "] ") : "") + colorBefore + GameFacade.Emojis.EmojiToBB(evt.Text[0]) + colorAfter;
                default:
                    return "";
            }
        }
    }

    public delegate void UISendMessageDelegate(string msg);
}