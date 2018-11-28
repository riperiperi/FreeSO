/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Client.Utils;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client.Controllers;
using FSO.Common.Utils;
using FSO.Common.DataService.Model;
using FSO.Client.Model;
using Microsoft.Xna.Framework;
using FSO.Files.Formats.tsodata;
using FSO.Common;
using FSO.Server.Protocol.Electron.Packets;

namespace FSO.Client.UI.Panels
{
    public class UIMessageWindow : UICachedContainer
    {
        public Texture2D backgroundMessageImage { get; set; }
        public Texture2D backgroundLetterComposeImage { get; set; }
        public Texture2D backgroundLetterReadImage { get; set; }
        public Texture2D backgroundBtnImage { get; set; }
        public Texture2D backgroundImage { get; set; }
        public UIButton MinimizeButton { get; set; }
        public UIButton CloseButton { get; set; }

        // Text Edits
        public UITextEdit MessageTextEdit { get; set; }
        public UITextEdit HistoryTextEdit { get; set; }
        public UITextEdit LetterTextEdit { get; set; }
        public UITextEdit LetterSubjectTextEdit { get; set; }

        // Message Elements
        public UISlider MessageSlider { get; set; }
        public UISlider HistorySlider { get; set; }
        public UIButton HistoryScrollUpButton { get; set; }
        public UIButton HistoryScrollDownButton { get; set; }
        public UIButton MessageScrollUpButton { get; set; }
        public UIButton MessageScrollDownButton { get; set; }
        public UIButton SendMessageButton { get; set; }

        // Letter Elements
        public UISlider LetterSlider { get; set; }
        public UIButton LetterScrollUpButton { get; set; }
        public UIButton LetterScrollDownButton { get; set; }
        public UIButton SendLetterButton { get; set; }
        public UIButton RespondLetterButton { get; set; }

        public UILabel SimNameText { get; set; }

        public UIButton SpecialButton;
        public MessageSpecialType SpecialType;

        private UIImage TypeBackground;
        private UIImage Background;
        private UIImage BtnBackground;
        public List<IMEntry> Messages;
        public MessageType MessageType;

        private UIPersonButton PersonButton;
        public Binding<UserReference> User;
        public Binding<UserReference> MyUser;

        public override Vector2 Size { get; set; }

        /// <summary>
        /// Creates a new UIMessage instance.
        /// </summary>
        /// <param name="type">The type of message (IM, compose or read).</param>
        /// <param name="author">Author if type is read or IM, recipient if type is compose.</param>
        public UIMessageWindow()
        {
            var script = this.RenderScript("message.uis");

            Messages = new List<IMEntry>();

            BtnBackground = new UIImage(backgroundBtnImage);
            BtnBackground.X = 313;
            BtnBackground.Y = 216;
            this.AddAt(0, BtnBackground);

            TypeBackground = new UIImage(backgroundMessageImage);
            TypeBackground.X = 10;
            TypeBackground.Y = 12;
            this.AddAt(0, TypeBackground);

            Background = new UIImage(backgroundImage);
            this.AddAt(0, Background);

            UIUtils.MakeDraggable(Background, this, true);
            UIUtils.MakeDraggable(TypeBackground, this, true);

            LetterSubjectTextEdit.MaxLines = 1;
            LetterSubjectTextEdit.TextMargin = new Microsoft.Xna.Framework.Rectangle(2, 2, 2, 2);
            LetterSubjectTextEdit.MaxChars = 128;

            MessageSlider.AttachButtons(MessageScrollUpButton, MessageScrollDownButton, 1);
            MessageTextEdit.AttachSlider(MessageSlider);
            MessageTextEdit.OnChange += new ChangeDelegate(MessageTextEdit_OnChange);
            SendMessageButton.OnButtonClick += new ButtonClickDelegate(SendMessage);

            var emojis = new UIEmojiSuggestions(MessageTextEdit);
            DynamicOverlay.Add(emojis);
            MessageTextEdit.OnEnterPress += new KeyPressDelegate(SendMessageEnter);

            SendMessageButton.Disabled = true;

            LetterSlider.AttachButtons(LetterScrollUpButton, LetterScrollDownButton, 1);
            LetterTextEdit.AttachSlider(LetterSlider);
            LetterTextEdit.MaxChars = 1000;

            var emojis2 = new UIEmojiSuggestions(LetterTextEdit);
            DynamicOverlay.Add(emojis2);

            RespondLetterButton.OnButtonClick += new ButtonClickDelegate(RespondLetterButton_OnButtonClick);
            SendLetterButton.OnButtonClick += new ButtonClickDelegate(SendLetter);

            HistorySlider.AttachButtons(HistoryScrollUpButton, HistoryScrollDownButton, 1);
            HistoryTextEdit.AttachSlider(HistorySlider);
            HistoryTextEdit.BBCodeEnabled = true;
            HistoryTextEdit.TextStyle = HistoryTextEdit.TextStyle.Clone();
            HistoryTextEdit.TextStyle.Size = 8;
            HistoryTextEdit.TextMargin = new Microsoft.Xna.Framework.Rectangle(3, 3, 3, 3);
            HistoryTextEdit.SetSize(333, 100);

            CloseButton.OnButtonClick += new ButtonClickDelegate(CloseButton_OnButtonClick);
            MinimizeButton.OnButtonClick += MinimizeButton_OnButtonClick;

            PersonButton = script.Create<UIPersonButton>("AvatarThumbnail");
            PersonButton.FrameSize = UIPersonButtonSize.SMALL;
            Add(PersonButton);

            SpecialButton = new UIButton();
            SpecialButton.Visible = false;
            SpecialButton.OnButtonClick += SpecialButton_OnButtonClick;
            Add(SpecialButton);

            User = new Binding<UserReference>()
                .WithBinding(SimNameText, "Caption", "Name");

            MyUser = new Binding<UserReference>();

            User.ValueChanged += (x) => PersonButton.User.Value = x;
            Size = Background.Size.ToVector2();


            this.Opacity = GlobalSettings.Default.ChatWindowsOpacity;
            this.AddUpdateHook(ChatOpacityChangedListener);
        }

        private void ChatOpacityChangedListener(UpdateState state)
        {
            if (this.Opacity == GlobalSettings.Default.ChatWindowsOpacity) return;

            FindController<MessagingWindowController>().UpdateOpacity();
        }

        private void SpecialButton_OnButtonClick(UIElement button)
        {
            if (SpecialType == MessageSpecialType.Normal) return;
            SpecialButton.Disabled = true;

            var controller = FindController<CoreGameScreenController>();
            controller?.FindMyNhood((nhoodID) =>
            {
                switch (SpecialType)
                {
                    case MessageSpecialType.Nominate:
                        controller.NeighborhoodProtocol.BeginNominations(nhoodID, SpecialResult);
                        break;
                    case MessageSpecialType.Vote:
                        controller.NeighborhoodProtocol.BeginVoting(nhoodID, SpecialResult);
                        break;
                    case MessageSpecialType.AcceptNomination:
                        controller.NeighborhoodProtocol.AcceptNominations(nhoodID, SpecialResult);
                        break;
                }
            });
        }

        private void SpecialResult(NhoodResponseCode code)
        {
            SpecialButton.Disabled = false;
            if (code == NhoodResponseCode.SUCCESS)
            {
                string title = GameFacade.Strings.GetString("f118", "1");
                string message = "";
                switch (SpecialType)
                {
                    case MessageSpecialType.Nominate:
                        message = GameFacade.Strings.GetString("f118", "16");
                        break;
                    case MessageSpecialType.Vote:
                        message = GameFacade.Strings.GetString("f118", "11");
                        break;
                    case MessageSpecialType.AcceptNomination:
                        message = GameFacade.Strings.GetString("f118", "19");
                        break;
                }

                UIAlert.Alert(title, message, true);
            }
        }

        private void MinimizeButton_OnButtonClick(UIElement button)
        {
            FindController<MessagingWindowController>().Hide();
        }

        /// <summary>
        /// User closed the UIMessage window.
        /// </summary>
        private void CloseButton_OnButtonClick(UIElement button)
        {
            FindController<MessagingWindowController>().Close();
        }

        private void SendMessageEnter(UIElement element)
        {
            //remove newline first
            if (MessageType != Controllers.MessageType.Call || MessageTextEdit.EventSuppressed) return; //cannot send on enter for letters (or during read mode :|)
            MessageTextEdit.CurrentText = MessageTextEdit.CurrentText.TrimEnd('\n');
            SendMessage(this);
        }

        private void RespondLetterButton_OnButtonClick(UIElement button)
        {
            if (User.Value?.Type != Common.Enum.UserReferenceType.AVATAR) return;
            FindController<CoreGameScreenController>().WriteEmail(User.Value.Id, ClipString("RE: "+LetterSubjectTextEdit.CurrentText, 128));

            /*if (MessageType != Controllers.MessageType.ReadLetter) return;
            SetType(Controllers.MessageType.WriteLetter);*/
        }

        private string ClipString(string str, int length)
        {
            if (str.Length > length) return str.Substring(0, length);
            else return str;
        }

        private void SendMessage(UIElement button)
        {
            if (MessageType != MessageType.Call) { return; }
            SendMessageButton.Disabled = true;
            if (MessageTextEdit.CurrentText.Length == 0) return;

            var msg = MessageTextEdit.CurrentText;
            MessageTextEdit.CurrentText = "";

            HIT.HITVM.Get().PlaySoundEvent(UISounds.CallSend);

            FindController<MessagingWindowController>().SendIM(msg);
        }

        private void SendLetter(UIElement button)
        {
            var msg = new MessageItem
            {
                Subject = LetterSubjectTextEdit.CurrentText,
                Body = LetterTextEdit.CurrentText,
                Type = 0,
                Subtype = 0,
            };
            FindController<MessagingWindowController>().SendLetter(msg);
        }

        private void MessageTextEdit_OnChange(UIElement TextEdit)
        {
            UITextEdit edit = (UITextEdit)TextEdit;
            SendMessageButton.Disabled = (edit.CurrentText.Length == 0);
        }

        public void AddMessage(UserReference user, string message, uint color, IMEntryType type)
        {
            Messages.Add(new IMEntry(user, message, new Color(color), type));
            RenderMessages();
        }

        public void SetEmail(string subject, string message, bool to)
        {
            SetEmail(subject, message, to, MessageSpecialType.Normal, 0);
        }

        public void SetEmail(string subject, string message, bool to, MessageSpecialType specialType, uint typeExpiry)
        {
            LetterSubjectTextEdit.CurrentText = subject;
            LetterTextEdit.CurrentText = GameFacade.Emojis.EmojiToBB(message);

            if (to)
            {
                RespondLetterButton.Disabled = true;
            }

            SetSpecialTypeButton(specialType, typeExpiry);
        }
        
        private void SetSpecialTypeButton(MessageSpecialType type, uint typeExpiry)
        {
            var now = ClientEpoch.Now;

            SpecialButton.Disabled = (typeExpiry != 0 && now > typeExpiry);
            SpecialType = type;
            if (type == MessageSpecialType.Normal)
            {
                SpecialButton.Visible = false;
            }
            else
            {
                SpecialButton.Visible = true;
                SpecialButton.Caption = GameFacade.Strings.GetString("f119", ((int)type).ToString());
                SpecialButton.Position = new Vector2(
                    (int)(MessageTextEdit.X + (MessageTextEdit.Size.X - SpecialButton.Width) / 2), 
                    Size.Y - 36
                    );
            }
        }

        public void RenderMessages()
        {
            var sb = new StringBuilder();
            var emojis = GameFacade.Emojis;
            for (int i = 0; i < Messages.Count; i++)
            {
                var elem = Messages.ElementAt(i);
                sb.Append("[color=lightgray]<");

                var avatarColor = "[color=#" + elem.Color.R.ToString("x2") + elem.Color.G.ToString("x2") + elem.Color.B.ToString("x2") + "][s]";
                var colorAfter = "[/s][/color]";

                sb.Append(avatarColor+elem.User.Name+colorAfter);
                sb.Append(">:[/color] ");
                sb.Append(emojis.EmojiToBB(BBCodeParser.SanitizeBB(elem.MessageBody)));
                if (i != Messages.Count - 1) sb.Append("\n");
            }
            HistoryTextEdit.CurrentText = sb.ToString();
            HistoryTextEdit.ComputeDrawingCommands();
            HistoryTextEdit.VerticalScrollPosition = Int32.MaxValue;
        }

        public void SetType(MessageType type)
        {
            bool showMess = (type == Controllers.MessageType.Call);
            bool showLetter = (type == Controllers.MessageType.ReadLetter|| type == Controllers.MessageType.WriteLetter);

            MessageTextEdit.Visible = showMess;
            MessageScrollDownButton.Visible = showMess;
            MessageScrollUpButton.Visible = showMess;
            MessageSlider.Visible = showMess;
            HistoryTextEdit.Visible = showMess;
            HistorySlider.Visible = showMess;
            HistoryScrollUpButton.Visible = showMess;
            HistoryScrollDownButton.Visible = showMess;
            SendMessageButton.Visible = (type == Controllers.MessageType.Call);

            LetterSubjectTextEdit.Visible = showLetter;
            LetterTextEdit.Visible = showLetter;
            LetterSlider.Visible = showLetter;
            LetterScrollUpButton.Visible = showLetter;
            LetterScrollDownButton.Visible = showLetter;

            SendLetterButton.Visible = (type == Controllers.MessageType.WriteLetter);
            RespondLetterButton.Visible = (type == Controllers.MessageType.ReadLetter);
            RespondLetterButton.Disabled = (User.Value?.Type != Common.Enum.UserReferenceType.AVATAR);

            TypeBackground.Texture = (type == Controllers.MessageType.Call) ? backgroundMessageImage : (type == Controllers.MessageType.ReadLetter) ? backgroundLetterReadImage : backgroundLetterComposeImage;

            LetterSubjectTextEdit.Mode = (type == Controllers.MessageType.ReadLetter) ? UITextEditMode.ReadOnly : UITextEditMode.Editor;
            LetterTextEdit.Mode = (type == Controllers.MessageType.ReadLetter) ? UITextEditMode.ReadOnly : UITextEditMode.Editor;
            LetterTextEdit.BBCodeEnabled = (type == Controllers.MessageType.ReadLetter);

            if (type == Controllers.MessageType.WriteLetter)
            {
                LetterSubjectTextEdit.CurrentText = "";
                LetterTextEdit.CurrentText = "";
            }

            MessageType = type;
        }
    }

    public enum UIMessageType
    {
        IM = 0,
        Compose = 1,
        Read = 2
    }

    public enum IMEntryType
    {
        MESSAGE_OUT,
        MESSAGE_IN,
        ERROR
    }

    public class IMEntry 
    {
        public UserReference User;
        public string MessageBody;
        public IMEntryType Type;
        public Color Color;

        public IMEntry(UserReference user, string message, Color color, IMEntryType type)
        {
            User = user;
            MessageBody = message;
            Type = type;
            Color = color;
        }
    }

    public class UIMessageGroup : UIContainer
    {
        public UIMessageWindow window;
        public UIMessageIcon icon;
        public UIMessageType type;
        public bool Shown;
        public string name;
        private UIMessageController parent;
        private int Ticks;
        private bool Alert;

        public UIMessageGroup(UIMessageType type, MessageAuthor author, UIMessageController parent)
        {
            this.parent = parent;
            this.name = author.Author;
            this.type = type;

            window = new UIMessageWindow();
            this.Add(window);
            window.X = GlobalSettings.Default.GraphicsWidth / 2 - 194;
            window.Y = GlobalSettings.Default.GraphicsHeight / 2 - 125;
            //icon = new UIMessageIcon(type);
            //this.Add(icon);

            icon.button.OnButtonClick += new ButtonClickDelegate(Show);
            window.MinimizeButton.OnButtonClick += new ButtonClickDelegate(Hide);
            window.CloseButton.OnButtonClick += new ButtonClickDelegate(Close);
            this.AddUpdateHook(new UpdateHookDelegate(ButtonAnim));
            Ticks = 0;
            Alert = false;

            Hide(this);
        }

        public void Close(UIElement button)
        {
            parent.RemoveMessageGroup(this);
        }

        public void ButtonAnim(UpdateState state)
        {
            icon.button.Selected = false;
            if (Alert && Ticks < 30) icon.button.Selected = true;

            if (++Ticks > 59)
            {
                Ticks = 0;
            }
        }

        public void Show(UIElement button)
        {
            Shown = true;
            icon.Visible = false;
            window.Visible = true;
            Alert = false;
            parent.ReorderIcons();
        }

        public void Hide(UIElement button)
        {
            Shown = false;
            icon.Visible = true;
            window.Visible = false;
            parent.ReorderIcons();
        }

        public void SetEmail(string subject, string message)
        {
            window.SetEmail(subject, message, false);
            if (!Shown) Alert = true;
        }

        public void MoveIcon(int pos)
        {
            if (pos == -1)
            {
                icon.X = GlobalSettings.Default.GraphicsWidth+5; //put offscreen
            }
            else
            {
                icon.X = GlobalSettings.Default.GraphicsWidth - 70;
                icon.Y = 80 + 45 * pos; //should be 10 without debug buttons
            }
        }
    }

    /// <summary>
    /// Author of a message - name and GUID.
    /// </summary>
    public class MessageAuthor
    {
        public string Author;
        public string GUID;
    }
}
