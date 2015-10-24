using FSO.Client.UI.Panels;
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

        public MessagingWindowController(UIMessageWindow view)
        {
            this.View = view;
        }

        public void Init(Message message){
            Message = message;
            View.SetType(message.Type);
            View.User.Value = message.User;
        }

        public void SendIM(string body){

        }

        public void Close(){
        }
    }
}
