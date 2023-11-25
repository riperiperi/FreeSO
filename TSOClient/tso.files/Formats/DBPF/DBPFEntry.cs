namespace FSO.Files.Formats.DBPF
{
    /// <summary>
    /// GroupIDs for DBPF archives, defined in sys\\tsosounddata.ini
    /// </summary>
    public enum DBPFGroupID : uint
    {
        Multiplayer = 0x29dd0888,
        Custom = 0x29daa4a6,
        CustomTrks = 0x29d9359d,
        Tracks = 0xa9c6c89a,
        TrackDefs = 0xfdbdbf87,
        tsov2 = 0x69c6c943,
        Samples = 0x9dbdbf89,
        HitLists = 0x9dbdbf74,
        HitListsTemp = 0xc9c6c9b3,
        Stings = 0xddbdbf8c,
        HitLabUI = 0x1d6962cf,
        HitLabTestSamples = 0x1d8a8b4f,
        HitLabTest = 0xbd6e5937,
        EP2 = 0xdde8f5c6,
        EP5Samps = 0x8a6fcc30
    }

    /// <summary>
    /// TypeIDs for DBPF archives, defined in sys\\tsoaudio.ini
    /// </summary>
    public enum DBPFTypeID : uint
    {
        XA = 0x1d07eb4b,
        UTK = 0x1b6b9806,
        WAV = 0xbb7051f5,
        MP3 = 0x3cec2b47,
        TRK = 0x5D73A611,
        HIT = 0x7b1acfcd,
        SoundFX = 0x2026960b, 
    }

    /// <summary>
    /// Represents an entry in a DBPF archive.
    /// </summary>
    public class DBPFEntry
    {
        //A 4-byte integer describing what type of file is held
        public DBPFTypeID TypeID;

        //A 4-byte integer identifying what resource group the file belongs to
        public DBPFGroupID GroupID;

        //A 4-byte ID assigned to the file which, together with the Type ID and the second instance ID (if applicable), is assumed to be unique all throughout the game
        public uint InstanceID;
        //too bad we're not using a version with a second instance id!!

        //A 4-byte unsigned integer specifying the offset to the entry's data from the beginning of the archive
        public uint FileOffset;

        //A 4-byte unsigned integer specifying the size of the entry's data
        public uint FileSize;
    }
}
