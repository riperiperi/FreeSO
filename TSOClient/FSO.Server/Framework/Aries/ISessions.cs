using FSO.Common.Utils;
using FSO.Server.Framework.Voltron;
using System.Collections.Generic;

namespace FSO.Server.Framework.Aries
{
    public interface ISessions
    {
        T UpgradeSession<T>(IAriesSession session, Callback<T> init) where T : AriesSession;

        ISessionGroup GetOrCreateGroup(object id);
        IVoltronSession GetByAvatarId(uint id);
        ISessionProxy All();
        HashSet<IAriesSession> Clone();

        void Broadcast(params object[] messages);
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
