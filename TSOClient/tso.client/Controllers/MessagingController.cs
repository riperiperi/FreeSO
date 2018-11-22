using FSO.Client.Model;
using FSO.Client.UI.Panels;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.DataService;
using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using FSO.Common.Enum;
using FSO.Common.Utils;
using FSO.Files.Formats.tsodata;
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

        public Message ReadLetter(UserReference user, MessageItem item)
        {
            return AddMessage(new Message
            {
                Type = MessageType.ReadLetter,
                User = user,
                LetterID = item.ID
            });
        }


        public Message WriteLetter(UserReference user)
        {
            return AddMessage(new Message
            {
                Type = MessageType.WriteLetter,
                User = user,
            });
        }


        public void ToggleWindow(Message message) {
            var window = GetWindow(message);
            window.Visible = !window.Visible;
            message.Read = true;
            UpdateTray();
        }

        public static Tuple<string, string, string, uint> CSTReplace(string sender, string subject, string body)
        {
            if (sender == null || sender.Length == 0 || sender[0] != ';')
                return new Tuple<string, string, string, uint>(sender, subject, body, 0);

            if (sender == ";default") //default name
                sender = "The Sims Online";
            else
                sender = CSTReplaceString(sender, false);

            if (subject.Length > 0 && subject[0] == ';')
                subject = CSTReplaceString(subject, false);

            uint expire = 0;
            if (body.Length > 0 && body[0] == ';') {
                var ind = body.IndexOf(';', 1);
                if (ind != -1)
                    uint.TryParse(body.Substring(1, ind - 1), out expire);
                body = CSTReplaceString(body, true);
            }

            return new Tuple<string, string, string, uint>(sender, subject, body, expire);
        }

        public static string CSTReplaceString(string data, bool hasExpire)
        {
            var split = data.Substring(1).Split(';');
            if (split.Length < (hasExpire ? 3 : 2)) return data;

            var args = split.Skip((hasExpire)?3:2).ToArray();
            if (hasExpire)
            {
                uint expire;
                if (uint.TryParse(split[0], out expire) && expire > 0)
                {
                    var date = ClientEpoch.ToDate(expire);
                    date = date.ToLocalTime();
                    var dateString = date.ToShortTimeString() + " " + date.ToShortDateString();
                    for (int i=0; i<args.Length; i++)
                    {
                        if (args[i] == split[0]) args[i] = dateString;
                    }
                }
            }
            int off = hasExpire ? 1 : 0;

            return GameFacade.Strings.GetString(split[off], split[1+off], args);
        }

        public void SetEmailMessage(Message message, MessageItem item)
        {
            var window = GetWindow(message);
            var tuple = CSTReplace(item.SenderName, item.Subject, item.Body);
            window.SetEmail(tuple.Item2, tuple.Item3, item.SenderID == item.TargetID, (MessageSpecialType)item.Subtype, tuple.Item4);
        }

        public void ShowWindow(Message message)
        {
            var window = GetWindow(message);
            window.Visible = true;
            message.Read = true;
            UpdateTray();
        }

        public UIMessageWindow GetWindow(Message message){
            return MessageWindows[message];
        }

        private Message GetMessageByUser(MessageType mtype, UserReferenceType type, uint id)
        {
            var existing = ActiveMessages.FirstOrDefault(x => x.User.Type == type &&
                                                              x.User.Id == id && x.Type == mtype);
            return existing;
        }

        private Message GetLetterByID(int id)
        {
            var existing = ActiveMessages.FirstOrDefault(x => x.LetterID == id);
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
            if (ActiveMessages.Count(x => x.Type == message.Type) >= 3)
            {
                HIT.HITVM.Get().PlaySoundEvent("ui_call_q_full");
                return null;
            }

            var existing = (message.Type == MessageType.ReadLetter)?GetLetterByID(message.LetterID):GetMessageByUser(message.Type, message.User.Type, message.User.Id);
            if (existing != null){
                return existing;
            }

            var window = new UIMessageWindow();
            ControllerUtils.BindController<MessagingWindowController>(window).Init(message, this);
            Game.AddWindow(window);
            MessageWindows.Add(message, window);
            ActiveMessages.Add(message);

            Tray.SetItems(ActiveMessages);
            UpdateTray();

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

            var existing = GetMessageByUser(MessageType.Call, message.FromType, message.From);
            if (existing != null)
            {
                var window = GetWindow(existing);
                existing.Read = window.Visible;
                window.AddMessage(existing.User, message.Message, message.Color, IMEntryType.MESSAGE_IN);

                if (message.FromType == UserReferenceType.AVATAR && sendAck)
                {
                    Network.CityClient.Write(new InstantMessage {
                        Type = InstantMessageType.SUCCESS_ACK,
                        AckID = message.AckID,
                        To = message.From,
                        FromType = UserReferenceType.AVATAR,
                        From = Network.MyCharacter
                    });
                    HIT.HITVM.Get().PlaySoundEvent("ui_call_rec_next");
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
            // in TSO, new message windows were received in the tray and had to be opened 
            newMessage.Read = false;
            newWindow.Visible = false;
            UpdateTray();
            newWindow.AddMessage(newMessage.User, message.Message, message.Color, IMEntryType.MESSAGE_IN);

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
                    HIT.HITVM.Get().PlaySoundEvent("ui_call_rec_first");
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
        public int LetterID;
        public bool Read;
    }

    public enum MessageType
    {
        Call,
        ReadLetter,
        WriteLetter
    }
    
}
