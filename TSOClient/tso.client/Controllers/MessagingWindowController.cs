using FSO.Client.UI.Panels;
using FSO.Common.DataService;
using FSO.Common.Utils;
using FSO.Files.Formats.tsodata;
using FSO.Server.DataService.Model;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class MessagingWindowController
    {
        private UIMessageWindow View;
        private MessagingController Parent;
        private Message Message;
        private Network.Network Network;
        private IClientDataService DataService;

        public MessagingWindowController(UIMessageWindow view, Network.Network network, IClientDataService dataService)
        {
            this.View = view;
            this.Network = network;
            this.DataService = dataService;
        }

        public void Init(Message message, MessagingController parent){
            Message = message;
            Parent = parent;
            View.User.Value = message.User;
            View.SetType(message.Type);

        }

        public void SendIM(string body){
            var cref = Network.MyCharacterRef;
            View.AddMessage(cref, body, IMEntryType.MESSAGE_OUT);

            if (View.MyUser.Value == null)
            {
                View.MyUser.Value = cref;
                DataService.Request(MaskedStruct.Messaging_Message_Avatar, Network.MyCharacter).ContinueWith(x =>
                {
                    GameThread.NextUpdate(y =>
                    {
                        View.RenderMessages();
                    });
                });
            }

            if (Message.User.Type != Common.Enum.UserReferenceType.AVATAR){
                return;
            }

            Network.CityClient.Write(new InstantMessage {
                FromType = Common.Enum.UserReferenceType.AVATAR,
                From = Network.MyCharacter,
                Message = body,
                To = Message.User.Id,
                Type = InstantMessageType.MESSAGE,
                AckID = Guid.NewGuid().ToString()
            });
        }

        public void SendLetter(MessageItem letter)
        {
            var cref = Network.MyCharacterRef;

            if (View.MyUser.Value == null)
            {
                View.MyUser.Value = cref;
                DataService.Request(MaskedStruct.Messaging_Message_Avatar, Network.MyCharacter).ContinueWith(x =>
                {
                    GameThread.NextUpdate(y =>
                    {
                        SendLetter(letter);
                    });
                });
                return;
            }

            letter.SenderID = cref.Id;
            if (letter.SenderName == null) letter.SenderName = cref.Name;
            letter.TargetID = Message.User.Id;

            Network.CityClient.Write(new MailRequest
            {
                Type = MailRequestType.SEND,
                Item = letter
            });

            Close();
        }

        public void Close(){
            Parent.CloseWindow(Message);
        }

        public void Hide()
        {
            Parent.ToggleWindow(Message);
        }
    }
}
