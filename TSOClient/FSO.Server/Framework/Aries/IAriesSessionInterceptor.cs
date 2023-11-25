namespace FSO.Server.Framework.Aries
{
    public interface IAriesSessionInterceptor
    {
        void SessionCreated(IAriesSession session);
        void SessionUpgraded(IAriesSession oldSession, IAriesSession newSession);
        void SessionClosed(IAriesSession session);
        void SessionMigrated(IAriesSession session);
    }
}
