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
using Microsoft.Xna.Framework.Graphics;
using TSOClient.LUI;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Model;
using TSOClient.Code.Utils;
using tso.common.rendering.framework.model;

namespace TSOClient.Code.UI.Panels
{
    public class UIMessage : UIContainer
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
        public string Author;

        public UIMessage(UIMessageType type, string author)
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

            UIUtils.MakeDraggable(Background, this);
            UIUtils.MakeDraggable(TypeBackground, this);

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

            SetType(type);
            SetMessageAuthor(author);
        }

        void SendMessageEnter(UIElement element)
        {
            //remove newline first
            MessageTextEdit.CurrentText = MessageTextEdit.CurrentText.Substring(0, MessageTextEdit.CurrentText.Length - 2);
            SendMessage(this);
        }

        private void RespondLetterButton_OnButtonClick(UIElement button)
        {
            
            SetType(UIMessageType.Compose);
        }

        private void SendMessage(UIElement button)
        {
            SendMessageButton.Disabled = true;
            if (MessageTextEdit.CurrentText.Length == 0) return; //if they somehow get past the disabled button or press enter, don't send an empty message.

            AddMessage("Current User", MessageTextEdit.CurrentText);

            UIMessageController controller = (UIMessageController)Parent.Parent;
            controller.SendMessage(MessageTextEdit.CurrentText, Author);
            MessageTextEdit.CurrentText = "";
        }

        private void SendLetter(UIElement button)
        {
            UIMessageController controller = (UIMessageController)Parent.Parent;
            UIMessageGroup group = (UIMessageGroup)Parent;

            controller.SendLetter(LetterTextEdit.CurrentText, LetterSubjectTextEdit.CurrentText, Author);
            group.Close(this);
        }

        private void MessageTextEdit_OnChange(UIElement TextEdit)
        {
            UITextEdit edit = (UITextEdit)TextEdit;
            SendMessageButton.Disabled = (edit.CurrentText.Length == 0);
        }

        public void SetMessageAuthor(string name)
        {
            Author = name;
            SimNameText.Caption = name;
        }

        public void AddMessage(string name, string message)
        {
            Messages.Add(new IMEntry(name, message));
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
                sb.Append(elem.Name);
                sb.Append("]: ");
                sb.Append(elem.MessageBody);
                if (i != Messages.Count - 1) sb.Append("\r\n");
            }
            HistoryTextEdit.CurrentText = sb.ToString();
            HistoryTextEdit.ComputeDrawingCommands();
            HistoryTextEdit.VerticalScrollPosition = Int32.MaxValue;
        }

        public void SetType(UIMessageType type)
        {

            bool showMess = (type == UIMessageType.IM);
            bool showLetter = (type == UIMessageType.Compose || type == UIMessageType.Read);

            MessageTextEdit.Visible = showMess;
            MessageScrollDownButton.Visible = showMess;
            MessageScrollUpButton.Visible = showMess;
            MessageSlider.Visible = showMess;
            HistoryTextEdit.Visible = showMess;
            HistorySlider.Visible = showMess;
            HistoryScrollUpButton.Visible = showMess;
            HistoryScrollDownButton.Visible = showMess;
            SendMessageButton.Visible = (type == UIMessageType.IM);

            LetterSubjectTextEdit.Visible = showLetter;
            LetterTextEdit.Visible = showLetter;
            LetterSlider.Visible = showLetter;
            LetterScrollUpButton.Visible = showLetter;
            LetterScrollDownButton.Visible = showLetter;

            SendLetterButton.Visible = (type == UIMessageType.Compose);
            RespondLetterButton.Visible = (type == UIMessageType.Read);

            TypeBackground.Texture = (type == UIMessageType.IM) ? backgroundMessageImage : (type == UIMessageType.Read) ? backgroundLetterReadImage : backgroundLetterComposeImage;

            LetterSubjectTextEdit.Mode = (type == UIMessageType.Read) ? UITextEditMode.ReadOnly : UITextEditMode.Editor;
            LetterTextEdit.Mode = (type == UIMessageType.Read) ? UITextEditMode.ReadOnly : UITextEditMode.Editor;

            if (type == UIMessageType.Compose)
            {
                LetterSubjectTextEdit.CurrentText = "";
                LetterTextEdit.CurrentText = "";
            }
        }
    }

    public enum UIMessageType
    {
        IM = 0,
        Compose = 1,
        Read = 2
    }

    public struct IMEntry 
    {
        public string Name;
        public string MessageBody;
        public IMEntry(string name, string message)
        {
            Name = name;
            MessageBody = message;
        }
    }

    public class UIMessageGroup : UIContainer
    {
        public UIMessage window;
        public UIMessageIcon icon;
        public UIMessageType type;
        public bool Shown;
        public string name;
        private UIMessageController parent;
        private int Ticks;
        private bool Alert;

        public UIMessageGroup(UIMessageType type, string name, UIMessageController parent)
        {
            this.parent = parent;
            this.name = name;
            this.type = type;

            window = new UIMessage(type, name);
            this.Add(window);
            window.X = GlobalSettings.Default.GraphicsWidth / 2 - 194;
            window.Y = GlobalSettings.Default.GraphicsHeight / 2 - 125;
            icon = new UIMessageIcon(type);
            this.Add(icon);

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

        public void AddMessage(string message)
        {
            window.AddMessage(this.name, message);
            if (!Shown) Alert = true;
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
                icon.Y = 50 + 45 * pos; //should be 10 without debug button
            }
        }
    }

    public class UIMessageIcon : UIContainer
    {
        public UIButton button;
        public Texture2D BackgroundImageCall { get; set; }
        public Texture2D BackgroundImageLetter { get; set; }

        public UIMessageIcon(UIMessageType type)
        {
            var script = this.RenderScript("messageicon.uis");
            button = new UIButton((type == UIMessageType.IM)?BackgroundImageCall:BackgroundImageLetter);
            button.ImageStates = 3;
            this.Add(button);
        }
    }
}
