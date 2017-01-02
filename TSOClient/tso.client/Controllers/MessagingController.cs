using FSO.Client.Model;
using FSO.Client.UI.Panels;
using FSO.Common.DataService;
using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using FSO.Common.Enum;
using FSO.Common.Utils;
using FSO.Server.Clients;
using FSO.Server.DataService.Model;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class MessagingController : IAriesMessageSubscriber, IDisposable
    {
        private CoreGameScreenController Game;
        private List<Message> ActiveMessages = new List<Message>();

        private Dictionary<Message, UIMessageWindow> MessageWindows = new Dictionary<Message, UIMessageWindow>();
        private UIMessageTray Tray;
        private Network.Network Network;
        private IClientDataService DataService;

        public MessagingController(CoreGameScreenController game, UIMessageTray messageTray, Network.Network network, IClientDataService dataService){
            this.Game = game;
            this.Tray = messageTray;
            this.Tray.SetController(this);
            this.Network = network;
            this.DataService = dataService;

            this.Network.CityClient.AddSubscriber(this);
        }

        public void SendLetter(MessageAuthor author){

        }

        public Message Call(UserReference user)
        {
            return AddMessage(new Message
            {
                Type = MessageType.Call,
                User = user
            });
        }

        public void ToggleWindow(Message message) {
            var window = GetWindow(message);
            window.Visible = !window.Visible;
            message.Read = true;
            UpdateTray();
        }

        public UIMessageWindow GetWindow(Message message){
            return MessageWindows[message];
        }

        private Message GetMessageByUser(UserReferenceType type, uint id)
        {
            var existing = ActiveMessages.FirstOrDefault(x => x.User.Type == type &&
                                                              x.User.Id == id);
            return existing;
        }

        public void CloseWindow(Message message)
        {
            //assumes the window still exists
            var window = GetWindow(message);
            Game.RemoveWindow(window);
            MessageWindows.Remove(message);
            ActiveMessages.Remove(message);
            UpdateTray();
        }

        private Message AddMessage(Message message)
        {
            if (ActiveMessages.Count >= 3)
            {
                //TODO: Play a sound
                return null;
            }

            var existing = GetMessageByUser(message.User.Type, message.User.Id);
            if (existing != null){
                return existing;
            }

            var window = new UIMessageWindow();
            window.BindController<MessagingWindowController>().Init(message, this);
            Game.AddWindow(window);
            MessageWindows.Add(message, window);
            ActiveMessages.Add(message);
            UpdateTray();
            Tray.SetItems(ActiveMessages);

            return message;
        }
        
        private void UpdateTray()
        {
            var trayItems = new List<Message>();
            foreach (var m in MessageWindows)
            {
                if (!m.Value.Visible) trayItems.Add(m.Key);
                else m.Key.Read = true;
            }
            Tray.SetItems(trayItems);
        }

        private void HandleInstantMessage(InstantMessage message)
        {
            var sendAck = message.Type == InstantMessageType.MESSAGE;

            if(message.Type == InstantMessageType.FAILURE_ACK){
                switch (message.Reason)
                {
                    case InstantMessageFailureReason.THEY_ARE_OFFLINE:
                        message.Message = GameFacade.Strings.GetString("195", "5");
                        break;
                    case InstantMessageFailureReason.MESSAGE_QUEUE_FULL:
                        message.Message = GameFacade.Strings.GetString("225", "4");
                        break;
                    case InstantMessageFailureReason.THEY_ARE_IGNORING_YOU:
                        message.Message = GameFacade.Strings.GetString("195", "19");
                        break;
                    case InstantMessageFailureReason.YOU_ARE_IGNORING_THEM:
                        message.Message = GameFacade.Strings.GetString("195", "18");
                        break;
                    default:
                        message.Message = message.Message = GameFacade.Strings.GetString("195", "11");
                        break;
                }
            }

            var existing = GetMessageByUser(message.FromType, message.From);
            if (existing != null)
            {
                var window = GetWindow(existing);
                existing.Read = window.Visible;
                window.AddMessage(existing.User, message.Message, IMEntryType.MESSAGE_IN);

                if (message.FromType == UserReferenceType.AVATAR && sendAck)
                {
                    Network.CityClient.Write(new InstantMessage {
                        Type = InstantMessageType.SUCCESS_ACK,
                        AckID = message.AckID,
                        To = message.From,
                        FromType = UserReferenceType.AVATAR,
                        From = Network.MyCharacter
                    });
                }
                return;
            }

            //Need to create an IM window for this user
            var reference = UserReference.Of(message.FromType, message.From);
            
            var newMessage = Call(reference);
            if (newMessage == null)
            {
                if (message.FromType == UserReferenceType.AVATAR && sendAck)
                {
                    Network.CityClient.Write(new InstantMessage
                    {
                        Type = InstantMessageType.FAILURE_ACK,
                        AckID = message.AckID,
                        To = message.From,
                        FromType = UserReferenceType.AVATAR,
                        From = Network.MyCharacter,
                        Reason = InstantMessageFailureReason.MESSAGE_QUEUE_FULL
                    });
                }
                return;
            }

            var newWindow = GetWindow(newMessage);
            newMessage.Read = true;
            newWindow.AddMessage(newMessage.User, message.Message, IMEntryType.MESSAGE_IN);

            //We need to make sure we have their name and icon
            if (message.FromType == UserReferenceType.AVATAR){
                if (sendAck)
                {
                    Network.CityClient.Write(new InstantMessage
                    {
                        Type = InstantMessageType.SUCCESS_ACK,
                        AckID = message.AckID,
                        To = message.From,
                        FromType = UserReferenceType.AVATAR,
                        From = Network.MyCharacter
                    });
                }

                DataService.Request(MaskedStruct.Messaging_Icon_Avatar, message.From);
                DataService.Request(MaskedStruct.Messaging_Message_Avatar, message.From).ContinueWith(x =>
                {
                    GameThread.NextUpdate(y =>
                    {
                        if (newWindow != null){
                            newWindow.RenderMessages();
                        }
                    });
                });
            }
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if(message is InstantMessage)
            {
                var instantMsg = (InstantMessage)message;
                if (instantMsg.Type == InstantMessageType.MESSAGE || instantMsg.Type == InstantMessageType.FAILURE_ACK)
                {
                    GameThread.NextUpdate((_) =>
                    {
                        HandleInstantMessage((InstantMessage)message);
                    });
                }
            }
        }

        public void Dispose()
        {
            this.Network.CityClient.RemoveSubscriber(this);
        }
    }

    public class Message : AbstractModel
    {
        public MessageType Type { get; set; }
        public UserReference User { get; set; }
        public bool Read;
    }

    public enum MessageType
    {
        Call,
        ReadLetter,
        WriteLetter
    }
    
}
