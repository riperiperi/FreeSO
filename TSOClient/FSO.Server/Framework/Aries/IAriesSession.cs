using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Aries
{
    public interface IAriesSession
    {
        uint UserId { get; }
        uint AvatarId { get; }
        bool IsAnonymous { get; }

        void Write(object message);
        void Close();
    }
}
