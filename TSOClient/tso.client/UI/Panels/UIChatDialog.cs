using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.SimAntics.NetPlay.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        private int chatSize = 8;

        public int Visitors;
        public string LotName = "Test Lot";

        public UIChatDialog()
            : base(UIDialogStyle.Standard | UIDialogStyle.OK | UIDialogStyle.Close, true)
        {
            //todo: this dialog is resizable. The elements use offests from each side to size and position themselves.
            //right now we're just using positions.

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

            ChatEntryTextEdit.OnEnterPress += SendMessageEnter;

            ChatHistoryText.Position = new Vector2(29, 47);
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
            SendMessage(this);
        }

        private void SendMessage(UIElement button)
        {
            OKButton.Disabled = true;
            if (ChatEntryTextEdit.CurrentText.Length == 0) return;

            if (OnSendMessage != null) OnSendMessage(ChatEntryTextEdit.CurrentText); 
            ChatEntryTextEdit.CurrentText = "";
        }

        public void ReceiveEvent(VMChatEvent evt)
        {
            Visitors = evt.Visitors;
            History.Add(evt);
            if (History.Count > 100) History.RemoveAt(0);
            RenderEvents();
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

            var updatedSize = delta * 30; //Incease or decrease by 30px

            var histStyle = ChatHistoryText.TextStyle.Clone();
            histStyle.Size = chatSize;
            ChatHistoryText.TextStyle = histStyle;

            this.SetSize((int)Math.Round(this.Size.X) + updatedSize, (int)Math.Round(this.Size.Y) + updatedSize);

            ChatHistoryBackground.SetSize(ChatHistoryBackground.Size.X + updatedSize, ChatHistoryBackground.Size.Y + updatedSize);
            ChatHistoryText.SetSize(ChatHistoryBackground.Size.X - 19, ChatHistoryBackground.Size.Y - 16);

            ChatHistorySlider.Position = new Vector2(ChatHistorySlider.Position.X + updatedSize, ChatHistorySlider.Position.Y);
            ChatHistorySlider.SetSize(ChatHistorySlider.Size.X, ChatHistoryBackground.Size.Y - 26);
            ChatHistoryScrollUpButton.Position = new Vector2(ChatHistoryScrollUpButton.Position.X + updatedSize, ChatHistoryScrollUpButton.Position.Y);
            ChatHistoryScrollDownButton.Position = new Vector2(ChatHistoryScrollDownButton.Position.X + updatedSize, ChatHistoryScrollDownButton.Position.Y + updatedSize);

            ChatEntryTextEdit.Position = new Vector2(ChatEntryTextEdit.Position.X, ChatEntryTextEdit.Position.Y + updatedSize);
            ChatEntryTextEdit.SetSize(ChatEntryTextEdit.Size.X, ChatEntryTextEdit.Size.Y);
            ChatEntryBackground.Position = new Vector2(ChatEntryBackground.Position.X, ChatEntryBackground.Position.Y + updatedSize);
            ChatEntryBackground.SetSize(ChatEntryBackground.Size.X + updatedSize, ChatEntryBackground.Size.Y);

            ChatEntryTextEdit.ComputeDrawingCommands();
            ChatHistoryText.ComputeDrawingCommands();
        }

        public void RenderEvents()
        {
            Caption = GameFacade.Strings.GetString("261", "1", new string[] { Visitors.ToString(), LotName });

            StringBuilder txt = new StringBuilder();
            bool first = true;
            foreach (var evt in History)
            {
                if (!first) txt.Append("\r\n");
                first = false;
                txt.Append(RenderEvent(evt));
            }

            bool scroll = ChatHistoryText.VerticalScrollMax == ChatHistoryText.VerticalScrollPosition;
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
           
        public string RenderEvent(VMChatEvent evt)
        {
            switch (evt.Type)
            {
                case VMChatEventType.Message:
                    return GameFacade.Strings.GetString("261", "8").Replace("%", evt.Text[0]) + evt.Text[1];
                case VMChatEventType.MessageMe:
                    return GameFacade.Strings.GetString("261", "9") + evt.Text[1];
                case VMChatEventType.Join:
                    return GameFacade.Strings.GetString("261", "6").Replace("%", evt.Text[0]);
                case VMChatEventType.Leave:
                    return GameFacade.Strings.GetString("261", "7").Replace("%", evt.Text[0]);
                case VMChatEventType.Arch:
                    return "<" + evt.Text[0] + " (" + evt.Text[1] + ")" + "> " + evt.Text[2];
                case VMChatEventType.Generic:
                    return evt.Text[0];
                default:
                    return "";
            }
        }
    }

    public delegate void UISendMessageDelegate(string msg);
}