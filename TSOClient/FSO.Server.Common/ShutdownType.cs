namespace FSO.Server.Common
{
    public enum ShutdownType : byte
    {
        SHUTDOWN = 0,
        RESTART = 1,
        UPDATE = 2 //restart but runs an update task
    }
}
