using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Files.Formats.IFF
{
    public class IffRuntimeInfo
    {
        public IffRuntimeState State;
        public IffUseCase UseCase;
        public string Filename;
        public string Path;
        public bool Dirty;
        public List<IffFile> Patches = new List<IffFile>();
    }

    public enum IffRuntimeState
    {
        ReadOnly, //orignal game iff
        PIFFPatch, //replacement patch
        PIFFClone, //clone of original object
        Standalone //standalone, mutable iff
    }

    public enum IffUseCase
    {
        Object,
        ObjectSprites,
        Global
    }
}
