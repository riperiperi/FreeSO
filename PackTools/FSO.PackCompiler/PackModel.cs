using System.Collections.Generic;
using FSO.PackCompiler.ArtGen;

namespace FSO.PackCompiler
{
    public class PackFile
    {
        public string Schema;
        public string Engine;
        public PackMeta Meta = new PackMeta();
        public List<PackObject> Objects = new List<PackObject>();
    }

    public class PackMeta
    {
        public string Id;
        public string Name;
        public string Author;
        public string Version;
        public string Description;
    }

    public class PackObject
    {
        public string Id;
        public uint Guid;
        public string Name;
        public int Price;
        public string Category;
        public List<string> Tags = new List<string>();
        public uint? CloneFromGuid;
        public PackGeneratedAppearance Generated;
        public List<string> Attributes = new List<string>();
        public SortedDictionary<int, string> DialogStrings = new SortedDictionary<int, string>();
        public List<PackInteraction> Interactions = new List<PackInteraction>();
        public List<PackTree> Trees = new List<PackTree>(); // declaration order = tree id order
        public string EntryMain;
        public string EntryInit;
        public string Path; // json path for diagnostics
    }

    /// <summary>appearance.generated: parametric art, in place of appearance.clone_from_guid.</summary>
    public class PackGeneratedAppearance
    {
        public string Generator; // "chair" | "table" | "bed" | "lamp" | "storage" | "sofa" | "primitives"
        public ChairGenerator.Params ChairParams;
        public TableGenerator.Params TableParams;
        public BedGenerator.Params BedParams;
        public LampGenerator.Params LampParams;
        public StorageGenerator.Params StorageParams;
        public SofaGenerator.Params SofaParams;
        public PartsGenerator.Params PartsParams;
    }

    public class PackInteraction
    {
        public string Name;
        public string ActionTree;
        public string TestTree;
        public string Path;

        // allow
        public bool AllowVisitors;
        public bool AllowOwner;
        public bool AllowRoommates;
        public bool AllowFriends;
        public bool AllowGhosts;
        public bool AllowCSRs;
        public bool AllowCats;
        public bool AllowDogs;
        public bool HasAllow;

        // flags
        public bool FlagDebug;
        public bool FlagAutoFirst;
        public bool FlagRunImmediately;
        public bool FlagMustRun;
        public bool FlagAllowConsecutive;
        public bool FlagJoinable;
        public bool FlagLeapfrog;
        public bool FlagCarrying;
        public bool FlagRepair;
        public bool FlagAlwaysCheck;
        public bool FlagWhenDead;

        // autonomy
        public Dictionary<byte, short> AdvertisedMotives = new Dictionary<byte, short>(); // motive index -> delta
        public uint AutonomyThreshold;
        public uint AttenuationCode;
        public float AttenuationValue;
        public int JoiningIndex;
    }

    public class PackTree
    {
        public string Name;
        public List<string> Args = new List<string>();
        public List<string> Locals = new List<string>();
        public List<PackNode> Nodes = new List<PackNode>();
        public string Path;
    }

    public class PackNode
    {
        public string Id;
        public string Prim;      // null when Call is set
        public string Call;      // called tree name, when this node is a tree call
        public string Then;
        public string Else;
        public JsonObj Fields;   // remaining primitive-specific fields (strict-read at compile time)
        public string Path;
    }
}
