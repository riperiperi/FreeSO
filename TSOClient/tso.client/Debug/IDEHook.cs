using FSO.Client.UI.Framework;
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        void InjectIDEInto(UIScreen screen, VM vm, BHAV targetBhav, GameObject targetObj);
        void IDEBreakpointHit(VM vm, VMEntity targetEnt);
    }
}
