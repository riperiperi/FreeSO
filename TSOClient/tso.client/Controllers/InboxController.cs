using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Panels.MessageStore;
using FSO.Client.UI.Screens;
using FSO.Common.DatabaseService;
using FSO.Common.DatabaseService.Model;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using FSO.Files.Formats.tsodata;
using FSO.Server.Clients;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Client.Controllers
{
    public class InboxController : IAriesMessageSubscriber, IDisposable
    {
        private UIInbox View;
        private IClientDataService DataService;
        private IDatabaseService DatabaseService;
        private Network.Network Network;

        public UIMessageStore InboxStore;

        public InboxController(UIInbox view, IClientDataService dataService, IDatabaseService database, Network.Network net)
        {
            this.View = view;
            this.DataService = dataService;
            this.DatabaseService = database;
            this.Network = net;

            this.InboxStore = UIMessageStore.Store;
            try
            {
                InboxStore.Load((int)net.MyCharacter);
            } catch
            {
                //oops, we couldnt load the local data...
                //assuming it failed anywhere, it will download from server as if timestamp were 0.
            }

            this.Network.CityClient.AddSubscriber(this);

            Network.CityClient.Write(new MailRequest
            {
                Type = MailRequestType.POLL_INBOX, //we're ready to recieve any pending roommate requests
                TimestampID = InboxStore.LastMessageTime
            });

            GameThread.NextUpdate(x =>
            {
                View.UpdateInbox();
            });
        }

        public void DeleteEmail(MessageItem msg)
        {
            InboxStore.Delete(msg.ID);
            if (msg.SenderID != msg.TargetID)
            {
                //only try to delete on server if this mail isn't a copy of one we sent.
                Network.CityClient.Write(new MailRequest
                {
                    Type = MailRequestType.DELETE,
                    TimestampID = msg.ID
                });
            }
        }

        public List<MessageItem> GetSortedInbox(Func<MessageItem, object> sortFunc, bool descending)
        {
            return ((descending)?InboxStore.Items.OrderByDescending(sortFunc) : InboxStore.Items.OrderBy(sortFunc)).ToList();
        }

        public void Search(string query, bool exact)
        {
            DatabaseService.Search(new SearchRequest { Query = query, Type = SearchType.SIMS }, exact)
                .ContinueWith(x =>
                {
                    GameThread.InUpdate(() =>
                    {
                        object[] ids = x.Result.Items.Select(y => (object)y.EntityId).ToArray();
                        var results = x.Result.Items.Select(q =>
                        {
                            return new GizmoAvatarSearchResult() { Result = q };
                        }).ToList();

                        if (ids.Length > 0)
                        {
                            var avatars = DataService.GetMany<Avatar>(ids).Result;
                            foreach (var item in avatars)
                            {
                                results.First(f => f.Result.EntityId == item.Avatar_Id).Avatar = item;
                            }
                        }

                        View.SetAvatarResults(results, exact);
                    });
                });
        }

        public void MessageReceived(AriesClient client, object message)
        {
            if (message is MailResponse)
            {
                var msg = (MailResponse)message;
                GameThread.NextUpdate(x =>
                {
                    switch (msg.Type)
                    {
                        case MailResponseType.NEW_MAIL:
                        case MailResponseType.POLL_RESPONSE:
                        case MailResponseType.SEND_SUCCESS:
                            //cool. we got mail, or got the mail we sent back. 
                            if (msg.Messages.Length == 0) return; //didnt actually get anything
                            foreach (var mail in msg.Messages)
                            {
                                InboxStore.Save(mail);
                            }
                            View.UpdateInbox();
                            if (msg.Type == MailResponseType.POLL_RESPONSE)
                            {
                                //show a message indicating we recieved new mail while we were offline.
                                UIAlert alert = null;
                                alert = UIScreen.GlobalShowAlert(new UI.Controls.UIAlertOptions
                                {
                                    Title = GameFacade.Strings.GetString("225", "1"),
                                    Message = GameFacade.Strings.GetString("225", msg.Messages.Length > 1 ? "3" : "2", new string[] { msg.Messages.Length.ToString() }),
                                    Buttons = new UI.Controls.UIAlertButton[]
                                    {
                                        new UI.Controls.UIAlertButton(UI.Controls.UIAlertButtonType.Yes, (btn =>
                                        {
                                            //show the inbox
                                            ((CoreGameScreen)UIScreen.Current).OpenInbox();
                                            UIScreen.RemoveDialog(alert);
                                        })),
                                        new UI.Controls.UIAlertButton(UI.Controls.UIAlertButtonType.No, (btn =>
                                        {
                                            UIScreen.RemoveDialog(alert);
                                        }))
                                    }
                                }, true);
                            }

                            if (msg.Type != MailResponseType.SEND_SUCCESS)
                            {
                                //alert user that mail has been recieved via sound and flashing icon
                                HIT.HITVM.Get().PlaySoundEvent(UISounds.LetterRecieve);
                                ((CoreGameScreen)UIScreen.Current).FlashInbox(true);
                            } else
                            {
                                HIT.HITVM.Get().PlaySoundEvent(UISounds.LetterSend);
                            }
                            break;
                    }
                });
            }
        }

        public void Dispose()
        {
            Network.CityClient.RemoveSubscriber(this);
        }
    }
}
