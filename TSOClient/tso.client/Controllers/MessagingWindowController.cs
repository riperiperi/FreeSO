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

        public MessagingWindowController(UIMessageWindow view)
        {
            this.View = view;
        }

        public void Init(UIMessageType type, uint avatarId){
            View.SetType(type);
        }

        public void Send(string body){

        }

        public void Close(){
        }
    }
}
