using FSO.Client.UI.Panels;
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
        private List<UIMessageWindow> MessageWindows = new List<UIMessageWindow>();
        private Dictionary<uint, UIMessageWindow> MessageWindowsByAvatarId = new Dictionary<uint, UIMessageWindow>();
        
        public MessagingController(CoreGameScreenController game){
            this.Game = game;
        }


        public void OpenChat(uint avatarId)
        {
            if (MessageWindowsByAvatarId.ContainsKey(avatarId))
            {
                //Already exists, just show it
            }
            else
            {
                if(MessageWindows.Count >= 3)
                {
                    //Not allowed more than 3
                    return;
                }

                //Start a new one
                var window = new UIMessageWindow();
                window.BindController<MessagingWindowController>().Init(UIMessageType.IM, avatarId);
                Game.AddWindow(window);
            }
        }
    }
}
