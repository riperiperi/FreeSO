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

namespace FSO.Client.UI.Panels
{
    public class UIMessageWindow : UIContainer
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

        private UIImage TypeBackground;
        private UIImage Background;
        private UIImage BtnBackground;
        public List<IMEntry> Messages;
        public MessageType MessageType;

        private UIPersonButton PersonButton;
        public Binding<UserReference> User;

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

            MessageSlider.AttachButtons(MessageScrollUpButton, MessageScrollDownButton, 1);
            MessageTextEdit.AttachSlider(MessageSlider);
            MessageTextEdit.OnChange += new ChangeDelegate(MessageTextEdit_OnChange);
            SendMessageButton.OnButtonClick += new ButtonClickDelegate(SendMessage);
            MessageTextEdit.OnEnterPress += new KeyPressDelegate(SendMessageEnter);
            SendMessageButton.Disabled = true;

            LetterSlider.AttachButtons(LetterScrollUpButton, LetterScrollDownButton, 1);
            LetterTextEdit.AttachSlider(LetterSlider);
            RespondLetterButton.OnButtonClick += new ButtonClickDelegate(RespondLetterButton_OnButtonClick);
            SendLetterButton.OnButtonClick += new ButtonClickDelegate(SendLetter);

            HistorySlider.AttachButtons(HistoryScrollUpButton, HistoryScrollDownButton, 1);
            HistoryTextEdit.AttachSlider(HistorySlider);

            HistoryTextEdit.TextStyle = HistoryTextEdit.TextStyle.Clone();
            HistoryTextEdit.TextStyle.Size = 8;
            HistoryTextEdit.TextMargin = new Microsoft.Xna.Framework.Rectangle(3, 3, 3, 3);
            HistoryTextEdit.SetSize(333, 100);

            CloseButton.OnButtonClick += new ButtonClickDelegate(CloseButton_OnButtonClick);

            PersonButton = script.Create<UIPersonButton>("AvatarThumbnail");
            PersonButton.FrameSize = UIPersonButtonSize.SMALL;
            Add(PersonButton);

            User = new Binding<UserReference>()
                .WithBinding(SimNameText, "Caption", "Name");

            User.ValueChanged += (x) => PersonButton.User.Value = x;
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
            if (MessageType != Controllers.MessageType.Call) return; //cannot send on enter for letters (or during read mode :|)
            MessageTextEdit.CurrentText = MessageTextEdit.CurrentText.Substring(0, MessageTextEdit.CurrentText.Length - 2);
            SendMessage(this);
        }

        private void RespondLetterButton_OnButtonClick(UIElement button)
        {
            if (MessageType != Controllers.MessageType.ReadLetter) return;
            SetType(Controllers.MessageType.WriteLetter);
        }

        private void SendMessage(UIElement button)
        {
            if (MessageType != MessageType.Call) { return; }
            SendMessageButton.Disabled = true;
            if (MessageTextEdit.CurrentText.Length == 0) return;

            var msg = MessageTextEdit.CurrentText;
            MessageTextEdit.CurrentText = "";

            FindController<MessagingWindowController>().SendIM(msg);
        }

        private void SendLetter(UIElement button)
        {
            /*if (MessageType != UIMessageType.Compose) return;
            UIMessageController controller = (UIMessageController)GameFacade.MessageController;

            controller.SendLetter(LetterTextEdit.CurrentText, LetterSubjectTextEdit.CurrentText, Author.GUID);*/
        }

        private void MessageTextEdit_OnChange(UIElement TextEdit)
        {
            UITextEdit edit = (UITextEdit)TextEdit;
            SendMessageButton.Disabled = (edit.CurrentText.Length == 0);
        }

        public void AddMessage(UserReference user, string message, IMEntryType type)
        {
            Messages.Add(new IMEntry(user, message, type));
            RenderMessages();
        }

        public void SetEmail(string subject, string message)
        {
            LetterSubjectTextEdit.CurrentText = subject;
            LetterTextEdit.CurrentText = message;
        }

        public void RenderMessages()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Messages.Count; i++)
            {
                var elem = Messages.ElementAt(i);
                sb.Append("[");
                sb.Append(elem.User.Name);
                sb.Append("]: ");
                sb.Append(elem.MessageBody);
                if (i != Messages.Count - 1) sb.Append("\r\n");
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

            TypeBackground.Texture = (type == Controllers.MessageType.Call) ? backgroundMessageImage : (type == Controllers.MessageType.ReadLetter) ? backgroundLetterReadImage : backgroundLetterComposeImage;

            LetterSubjectTextEdit.Mode = (type == Controllers.MessageType.ReadLetter) ? UITextEditMode.ReadOnly : UITextEditMode.Editor;
            LetterTextEdit.Mode = (type == Controllers.MessageType.ReadLetter) ? UITextEditMode.ReadOnly : UITextEditMode.Editor;

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

        public IMEntry(UserReference user, string message, IMEntryType type)
        {
            User = user;
            MessageBody = message;
            Type = type;
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
            window.SetEmail(subject, message);
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
