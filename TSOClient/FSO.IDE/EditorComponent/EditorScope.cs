using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine.Scopes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent
{
    public class EditorScope
    {
        public static GameGlobal Globals;
        public static IffFile Behaviour;

        public GameObject Object;
        public GameGlobalResource SemiGlobal;

        public EditorScope(GameObject obj)
        {
            Object = obj;
            var GLOBChunks = Object.Resource.List<GLOB>();
            if (GLOBChunks != null)
            {
                SemiGlobal = FSO.Content.Content.Get().WorldObjectGlobals.Get(GLOBChunks[0].Name).Resource;
            }
        }

        public string GetSubroutineName(ushort id)
        {
            string preface = (id >= 8192) ? "Semi-global: " : ((id >= 4096) ? "Private: " : "Global: ");

            var bhav = GetBHAV(id);
            if (bhav == null) return preface + "#" + id.ToString() + " (missing)";
            else return preface + bhav.ChunkLabel.Trim(new char[] { '\0' });
        }

        public string GetVarScopeName(VMVariableScope scope)
        {
            return Behaviour.Get<STR>(132).GetString((int)scope);
        }

        public BHAV GetBHAV(ushort id)
        {
            if (id >= 8192) return SemiGlobal.Get<BHAV>(id); //semiglobal
            else if (id >= 4096) return Object.Resource.Get<BHAV>(id); //private
            else return Globals.Resource.Get<BHAV>(id); //global
        }

        public T GetResource<T>(ushort id, ScopeSource source)
        {
            switch (source)
            {
                case ScopeSource.Private:
                    return Object.Resource.Get<T>(id);
                case ScopeSource.SemiGlobal:
                    return SemiGlobal.Get<T>(id);
                case ScopeSource.Global:
                    return Globals.Resource.Get<T>(id);
                default:
                    return default(T);
            }
        }

        public OBJD GetOBJD()
        {
            return Object.OBJ;
        }
    }

    public enum ScopeSource
    {
        Private,
        SemiGlobal,
        Global
    }
}
