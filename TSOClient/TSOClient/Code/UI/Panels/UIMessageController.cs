/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.LUI;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Model;
using TSOClient.Code.Utils;
using TSO.Common.rendering.framework.model;

namespace TSOClient.Code.UI.Panels
{
    /// <summary>
    /// A controller for messages.
    /// </summary>
    public class UIMessageController : UIContainer
    {
        public List<UIMessageGroup> MessageWindows;
        public List<EmailStore> PendingEmails;

        /// <summary>
        /// Fired when an IM UIMessage element sends a message. Should be wired up to the server. 
        /// If you need any more information than message and destination, feel free to edit in new functionality.
        /// </summary>
        public event MessageSendDelegate OnSendMessage;

        /// <summary>
        /// Fired when an Letter Compose UIMessage element sends a letter. Should be wired up to the server. 
        /// If you need any more information than message and destination, feel free to edit in new functionality.
        /// </summary>
        public event LetterSendDelegate OnSendLetter;

        public UIMessageController()
        {
            MessageWindows = new List<UIMessageGroup>();
            PendingEmails = new List<EmailStore>();
            this.AddUpdateHook(new UpdateHookDelegate(MCUpdate));
        }

        /// <summary>
        /// Fires OnSendMessage event with the passed message.
        /// </summary>
        public void SendMessage(string message, string GUID)
        {
            if (OnSendMessage != null) OnSendMessage(message, GUID);
        }

        /// <summary>
        /// Fires OnSendLetter event with the passed letter.
        /// </summary>
        public void SendLetter(string message, string subject, string destinationUser)
        {
            if (OnSendLetter != null) OnSendLetter(message, subject, destinationUser);
        }

        /// <summary>
        /// Display an IM message in its currently open window. If there is no window, this will create a new one.
        /// </summary>
        public void PassMessage(MessageAuthor Sender, string Message) 
        {
            UIMessageGroup group = GetMessageGroup(Sender.Author, UIMessageType.IM);
            if (group == null) {
                group = new UIMessageGroup(UIMessageType.IM, Sender, this);
                MessageWindows.Add(group);
                this.Add(group);
                ReorderIcons();
            }
            group.AddMessage(Message);
        }

        /// <summary>
        /// Brings up a "You've got mail!" dialog and upon confirming that you want to see it, opens the message.
        /// If multiple messages are recieved while the dialog is open it will be updated.
        /// </summary>
        public void PassEmail(MessageAuthor sender, string subject, string message)
        {
            //PendingEmails.Add(new EmailStore(sender, message));
            OpenEmail(sender, subject, message); //will eventually show alert asking if you want to do this...
        }

        /// <summary>
        /// Opens mail without the confirmation dialog. Use when manually opening mail from the inbox.
        /// </summary>
        public void OpenEmail(MessageAuthor sender, string subject, string message)
        {
            bool GroupExisted = false;

            for (int i = 0; i < MessageWindows.Count; i++)
            {
                //Did conversation already exist?
                if (MessageWindows[i].name.Equals(sender.Author, StringComparison.InvariantCultureIgnoreCase))
                {
                    GroupExisted = true;
                    MessageWindows[i].AddMessage(message);
                    break;
                }
            }

            if (!GroupExisted)
            {
                var group = new UIMessageGroup(UIMessageType.Read, sender, this);
                MessageWindows.Add(group);
                this.Add(group);

                group.SetEmail(subject, message);
            }

            ReorderIcons();
        }

        /// <summary>
        /// Remove a UIMessageGroup from the Message UI.
        /// </summary>
        public void RemoveMessageGroup(UIMessageGroup grp)
        {
            MessageWindows.Remove(grp);
            this.Remove(grp);
            ReorderIcons();
        }

        /// <summary>
        /// Get the first message group with the specified recipient name and type.
        /// </summary>
        private UIMessageGroup GetMessageGroup(string name, UIMessageType type)
        {
            for (int i = 0; i < MessageWindows.Count; i++)
            {
                var elem = MessageWindows.ElementAt(i);
                if (elem.name == name && elem.type == type) return elem;
            }
            return null;
        }

        /// <summary>
        /// Fix order of UIMessageGroup minimized icons. Called after every message minimize/restore/creation/deletion.
        /// </summary>
        public void ReorderIcons() 
        {
            int pos = 0;
            for (int i = 0; i < MessageWindows.Count; i++)
            {
                var elem = MessageWindows.ElementAt(i);
                if (!elem.Shown)
                {
                    elem.MoveIcon(pos++);
                }
                else
                {
                    elem.MoveIcon(-1);
                }
            }
        }

        //WIP test stuff. needs Yes/No Alerts.
        public void MCUpdate(UpdateState update)
        {
            //Async(new AsyncHandler(hi));
        }
    }

    public struct EmailStore
    {
        string name;
        string message;
        string subject;

        public EmailStore(string name, string subject, string message) 
        {
            this.name = name;
            this.subject = subject;
            this.message = message;
        }
    }

    public delegate void MessageSendDelegate(string message, string GUID);
    public delegate void LetterSendDelegate(string message, string subject, string GUID);
}
