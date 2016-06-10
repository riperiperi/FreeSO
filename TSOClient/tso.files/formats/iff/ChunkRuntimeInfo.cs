namespace FSO.Files.Formats.IFF
{
    public enum ChunkRuntimeState
    {
        Normal,
        Patched, //unmodified, but still save when outputting PIFF
        Modified, //modified. save when outputting PIFF
        Delete //this chunk should not be saved, or should be saved as a deletion.
    }
}