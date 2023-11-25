using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.HIT;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// A controller for messages.
    /// </summary>
    public class UIMessageController : UIContainer
    {
        public List<UIMessageGroup> MessageWindows;
        public List<EmailStore> PendingEmails;
        private bool soundAlt = true;

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
            HITVM.Get().PlaySoundEvent(UISounds.CallSend);
            if (OnSendMessage != null) OnSendMessage(message, GUID);
        }

        /// <summary>
        /// Fires OnSendLetter event with the passed letter.
        /// </summary>
        public void SendLetter(string message, string subject, string destinationUser)
        {
            HITVM.Get().PlaySoundEvent(UISounds.LetterSend);
            if (OnSendLetter != null) OnSendLetter(message, subject, destinationUser);
        }
        

        /// <summary>
        /// Brings up a "You've got mail!" dialog and upon confirming that you want to see it, opens the message.
        /// If multiple messages are recieved while the dialog is open it will be updated.
        /// </summary>
        public void PassEmail(MessageAuthor sender, string subject, string message)
        {
            HITVM.Get().PlaySoundEvent(UISounds.LetterRecieve);
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
                    //MessageWindows[i].AddMessage(message);
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
