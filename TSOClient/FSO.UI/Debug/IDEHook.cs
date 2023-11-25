using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics;

namespace FSO.Client.Debug
{
    public static class IDEHook
    {
        public static IDEInjector IDE;
        public static void SetIDE(IDEInjector ide)
        {
            IDE = ide;
        }
    }

    public interface IDEInjector
    {
        void StartIDE(VM vm);
        void IDEOpenBHAV(BHAV targetBhav, GameObject targetObj);
        void IDEBreakpointHit(VM vm, VMEntity targetEnt);
    }
}
