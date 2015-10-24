using FSO.Client.Model;
using FSO.Client.UI.Panels;
using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class MessagingController
    {
        private CoreGameScreenController Game;
        private List<Message> ActiveMessages = new List<Message>();

        private Dictionary<Message, UIMessageWindow> MessageWindows = new Dictionary<Message, UIMessageWindow>();
        private UIMessageTray Tray;

        public MessagingController(CoreGameScreenController game, UIMessageTray messageTray){
            this.Game = game;
            this.Tray = messageTray;
            this.Tray.SetController(this);
        }

        public void SendLetter(MessageAuthor author){

        }

        public void Call(UserReference user)
        {
            AddMessage(new Message
            {
                Type = MessageType.Call,
                User = user
            });
        }

        public void ToggleWindow(Message message) {
            var window = GetWindow(message);
            window.Visible = !window.Visible;
        }

        public UIMessageWindow GetWindow(Message message){
            return MessageWindows[message];
        }

        private Message AddMessage(Message message)
        {
            if (ActiveMessages.Count >= 3)
            {
                //TODO: Play a sound
                return null;
            }

            var existing = ActiveMessages.FirstOrDefault(x => x.User.Type == message.User.Type &&
                                                              x.User.Id == message.User.Id);

            if (existing != null){
                return existing;
            }

            var window = new UIMessageWindow();
            window.BindController<MessagingWindowController>().Init(message);
            Game.AddWindow(window);
            MessageWindows.Add(message, window);

            ActiveMessages.Add(message);
            Tray.SetItems(ActiveMessages);

            return message;
        }
    }

    public class Message : AbstractModel
    {
        public MessageType Type { get; set; }
        public UserReference User { get; set; }
    }

    public enum MessageType
    {
        Call,
        ReadLetter,
        WriteLetter
    }
    
}
