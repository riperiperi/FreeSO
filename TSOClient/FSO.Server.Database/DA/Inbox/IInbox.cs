using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Inbox
{
    public interface IInbox
    {
        List<DbInboxMsg> GetMessages(uint avatarID);
        List<DbInboxMsg> GetMessagesAfter(uint avatarID, DateTime after);
        DbInboxMsg Get(int msgID);
        int CreateMessage(DbInboxMsg msg);
        bool DeleteMessage(int msgID, uint avatarID);
        bool DeleteMessage(int msgID);
    }
}
