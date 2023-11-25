using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.SimAntics.NetPlay.Model;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Text;

namespace FSO.Client.UI.Panels
{
    public class UIPropertyLog : UIDialog
    {
        public UISlider ChatHistorySlider { get; set; }
        public UIButton ChatHistoryScrollUpButton { get; set; }
        public UIButton ChatHistoryScrollDownButton { get; set; }
        public UITextEdit ChatEntryTextEdit { get; set; }
        public UITextEdit ChatHistoryText { get; set; }
        public UIImage ChatHistoryBackground { get; set; }

        private List<VMChatEvent> History;

        public int Visitors;
        public string LotName = "Test Lot";

        public UIPropertyLog()
            : base(UIDialogStyle.Standard | UIDialogStyle.Close, true)
        {
            //todo: this dialog is resizable. The elements use offests from each side to size and position themselves.
            //right now we're just using positions.

            History = new List<VMChatEvent>();

            this.RenderScript("chatdialog.uis");
            this.SetSize(400, 255);

            this.Caption = "Property Log";

            ChatHistoryBackground = new UIImage(GetTexture((ulong)0x7A400000001)).With9Slice(13, 13, 13, 13);
            ChatHistoryBackground.Position = new Vector2(19, 39);
            ChatHistoryBackground.SetSize(341, 166+30);
            AddAt(3, ChatHistoryBackground);

            ChatHistorySlider.AttachButtons(ChatHistoryScrollUpButton, ChatHistoryScrollDownButton, 1);
            ChatHistorySlider.SetSize(ChatHistorySlider.Size.X, 138 + 30);
            ChatHistoryScrollDownButton.Position += new Vector2(0, 30);
            ChatHistoryText.AttachSlider(ChatHistorySlider);

            ChatHistoryText.Position = new Vector2(29, 47);
            var histStyle = ChatHistoryText.TextStyle.Clone();
            histStyle.Size = 8;
            ChatHistoryText.Size = new Vector2(322, 150+30);
            ChatHistoryText.MaxLines = 13;
            ChatHistoryText.TextStyle = histStyle;

            Remove(ChatEntryTextEdit);
            CloseButton.OnButtonClick += CloseButton_OnButtonClick;
        }

        private void CloseButton_OnButtonClick(UIElement button)
        {
            //hide self.
            Visible = false;
        }

        public void ReceiveEvent(VMChatEvent evt)
        {
            History.Add(evt);
            if (History.Count > 100) History.RemoveAt(0);
            RenderEvents();
        }

        public void RenderEvents()
        {
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
                default:
                    return "";
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            base.Draw(batch);
        }
    }
}