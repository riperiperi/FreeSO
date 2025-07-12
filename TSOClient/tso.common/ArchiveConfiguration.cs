using System;

namespace FSO.Common
{
    [Flags]
    public enum ArchiveConfigFlags
    {
        None = 0,
        Offline = 1 << 0,
        UPnP = 1 << 1,
        HideNames = 1 << 2,
        Verification = 1 << 3,
        AllOpenable = 1 << 4,
        DebugFeatures = 1 << 5,
        AllowLotCreation = 1 << 6,
        AllowSimCreation = 1 << 7,
        LockArchivedSims = 1 << 8
    }

    public class ArchiveConfiguration
    {
        public ArchiveConfigFlags Flags { get; set; }
        public string ArchiveDataDirectory { get; set; } // Effectively equal to the nfs
        public ushort CityPort { get; set; }
        public ushort LotPort { get; set; }
        public string ServerKey { get; set; }
        public float GameScale { get; set; } = 1;

        // Runtime
        public IDisposable[] Disposables;
    }
}
