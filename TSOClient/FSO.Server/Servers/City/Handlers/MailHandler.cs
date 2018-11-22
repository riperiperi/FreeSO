using FSO.Common.DataService;
using FSO.Common.Utils;
using FSO.Files.Formats.tsodata;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Inbox;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class MailHandler
    {
        private ISessions Sessions;
        private IDataService DataService;
        private IDAFactory DA;

        public MailHandler(ISessions sessions, IDataService dataService, IDAFactory da)
        {
            this.Sessions = sessions;
            this.DataService = dataService;
            this.DA = da;
        }

        public async void Handle(IVoltronSession session, MailRequest message)
        {
            if (session.IsAnonymous)  //CAS users can't do this.
                return;

            try
            {
                switch (message.Type)
                {
                    case MailRequestType.POLL_INBOX:
                        //when a user logs in they will send this request to recieve all messages after their last recieved message.
                        using (var da = DA.Get())
                        {
                            var msgs = da.Inbox.GetMessagesAfter(session.AvatarId, new DateTime(message.TimestampID));
                            session.Write(new MailResponse()
                            {
                                Type = MailResponseType.POLL_RESPONSE,
                                Messages = msgs.Select(x => ToItem(x)).ToArray()
                            });
                            return;
                        }
                    case MailRequestType.DELETE:
                        //when a user deletes a message from their pc, it should also be deleted from the server.
                        using (var da = DA.Get())
                        {
                            da.Inbox.DeleteMessage((int)message.TimestampID, session.AvatarId);
                            return;
                        }
                    case MailRequestType.SEND:
                        using (var da = DA.Get())
                        {
                            //admins get to change their message type, source, etc, and are never throttled.
                            var modLevel = da.Avatars.GetModerationLevel(session.AvatarId);

                            var them = await DataService.Get<FSO.Common.DataService.Model.Avatar>(message.Item.TargetID);
                            var toName = them.Avatar_Name;

                            if (modLevel == 0)
                            {
                                var bookmarks = them.Avatar_BookmarksVec;
                                if (bookmarks.Any(x => x.Bookmark_Type == 5 && x.Bookmark_TargetID == session.AvatarId))
                                {
                                    session.Write(new MailResponse()
                                    {
                                        Type = MailResponseType.SEND_IGNORING_YOU,
                                    });
                                    return;
                                }

                                var you = await DataService.Get<FSO.Common.DataService.Model.Avatar>(session.AvatarId);
                                bookmarks = you.Avatar_BookmarksVec;
                                if (bookmarks.Any(x => x.Bookmark_Type == 5 && x.Bookmark_TargetID == message.Item.TargetID))
                                {
                                    session.Write(new MailResponse()
                                    {
                                        Type = MailResponseType.SEND_IGNORING_THEM,
                                    });
                                    return;
                                }
                                message.Item.Type = 0;
                                message.Item.SenderID = session.AvatarId;
                                message.Item.SenderName = you.Avatar_Name;
                            }
                            message.Item.ReplyID = null; //currently unused, but may be used in future to track conversations.

                            var body = BBCodeParser.SanitizeBB(message.Item.Body);
                            if (body.Length > 1500) body = body.Substring(0, 1500);
                            message.Item.Body = body;

                            if (SendEmail(message.Item, true))
                            {
                                //give the sent message back to the sender for safe keeping.
                                if (toName != null)
                                {
                                    message.Item.SenderName = "To: " + toName;
                                    message.Item.SenderID = message.Item.TargetID;
                                    message.Item.ReadState = 1;
                                }
                                session.Write(new MailResponse()
                                {
                                    Type = MailResponseType.SEND_SUCCESS,
                                    Messages = new MessageItem[] { message.Item }
                                });
                                return;
                            }
                            else
                            {
                                session.Write(new MailResponse()
                                {
                                    Type = MailResponseType.SEND_FAILED,
                                    Messages = new MessageItem[] { message.Item }
                                });
                                return;
                            }
                        }
                }
            } catch
            {
                if (message.Type == MailRequestType.SEND) {
                    session.Write(new MailResponse()
                    {
                        Type = MailResponseType.SEND_FAILED,
                        Messages = new MessageItem[] { message.Item }
                    });
                }
                return;
            }
        }

        public bool SendSystemEmail(string cst, int subjectIndex, int msgIndex, int type, MessageSpecialType specialType,
            uint expireDate, uint targetID, params string[] args)
        {
            for (int i=0; i<args.Length; i++)
            {
                args[i] = args[i].Replace(';', ':');
            }
            var item = new MessageItem()
            {
                Subject = ";" + cst + ";" + subjectIndex,
                Body = ";" + expireDate + ";" + cst + ";" + msgIndex + ';' + string.Join(";", args),
                Type = type,
                Subtype = (int)specialType,
                SenderID = uint.MaxValue,
                TargetID = targetID,
                SenderName = ";default"
            };
            return SendEmail(item, true);
        }

        public bool SendEmail(MessageItem item, bool sendToSession)
        {
            //put it in the database first
            var dbitem = new DbInboxMsg()
            {
                sender_id = item.SenderID,
                target_id = item.TargetID,
                subject = item.Subject,
                body = item.Body,
                sender_name = item.SenderName,
                time = DateTime.UtcNow,
                msg_type = item.Type,
                msg_subtype = item.Subtype,
                read_state = 0,
            };

            using (var da = DA.Get())
            {
                try
                {
                    var id = da.Inbox.CreateMessage(dbitem);

                    //try to tell the target they have recieved a message.
                    //if they haven't we can serve it to them next time they log in.
                    dbitem.message_id = id;
                    item.ID = id;
                    item.Time = dbitem.time.Ticks;

                    if (sendToSession)
                    {
                        var targetSession = Sessions.GetByAvatarId(dbitem.target_id);
                        if (targetSession != null)
                        {
                            //send them the mail directly
                            targetSession.Write(new MailResponse
                            {
                                Type = MailResponseType.NEW_MAIL,
                                Messages = new MessageItem[] { item }
                            });
                        }
                    }

                    //it's in the database - next time the user logs in they will recieve the message if they dont have it

                    return true;
                } catch
                {
                    return false;
                }
            }
        }

        public MessageItem ToItem(DbInboxMsg msg)
        {
            var result = new MessageItem
            {
                ID = msg.message_id,
                SenderID = msg.sender_id,
                TargetID = msg.target_id,
                Subject = msg.subject,
                Body = msg.body,
                SenderName = msg.sender_name,
                Time = msg.time.Ticks,
                Type = msg.msg_type,
                Subtype = msg.msg_subtype,
                ReadState = msg.read_state,
                ReplyID = msg.reply_id
            };
            return result;
        }
    }

    public enum NeighMailStrings : int
    {
        NominateSubject = 1,
        Nominate = 2,

        VoteSubject = 3,
        Vote = 4,

        ElectionOverSubject = 5,
        ElectionOver = 6,

        NominationQuerySubject = 7,
        NominationQuery = 8,

        NominationAcceptedSubject = 9,
        NominationAccepted = 10,

        RunningForMayorSubject = 11,
        RunningForMayor = 12,

        TooFewNominationsSubject = 13,
        TooFewNominations = 14,

        NeighGameplayBanSubject = 15,
        NeighGameplayBan = 16,

        TermLengthSubject = 17,
        TermLength = 18,

        NoLongerMayorSubject = 19,
        NoLongerMayor = 20,

        NoLongerMayorModSubject = 21,
        NoLongerMayorMod = 22,

        NominationCountedSubject = 23,
        NominationCounted = 24,

        VoteCountedSubject = 25,
        VoteCounted = 26,

        YouWinSubject = 27,
        YouWin = 28,

        CancelledSubject = 29,
        Cancelled = 30,

        FailsafeSubject = 31,
        Failsafe = 32
    }
}
