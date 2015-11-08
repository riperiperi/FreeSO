using FSO.Client.UI.Panels;
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
        private Message Message;
        private Network.Network Network;

        public MessagingWindowController(UIMessageWindow view, Network.Network network)
        {
            this.View = view;
            this.Network = network;
        }

        public void Init(Message message){
            Message = message;
            View.SetType(message.Type);
            View.User.Value = message.User;
        }

        public void SendIM(string body){
            View.AddMessage(Network.MyCharacterRef, body, IMEntryType.MESSAGE_OUT);

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

        public void Close(){
        }
    }
}
