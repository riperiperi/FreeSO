using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Aries
{
    public interface ISessions
    {
        ISessionGroup GetOrCreateGroup(object id);
        

        ISessionProxy All();
    }

    public interface ISessionProxy {
        void Broadcast(params object[] messages);
    }

    public interface ISessionGroup : ISessionProxy
    {
        void Enroll(IAriesSession session);
        void UnEnroll(IAriesSession session);
    }
}
