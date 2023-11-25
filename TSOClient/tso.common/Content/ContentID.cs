namespace FSO.Common.Content
{
    /// <summary>
    /// Represents the ID of a content resource.
    /// Consists of two parts: TypeID (uint) and FileID (uint).
    /// </summary>
    public class ContentID
    {
        public uint TypeID;
        public uint FileID;
        public string FileName;
        private long v;


        /// <summary>
        /// Creates a new ContentID instance.
        /// </summary>
        /// <param name="typeID">The TypeID of the content resource.</param>
        /// <param name="fileID">The FileID of the content resource.</param>
        public ContentID(uint typeID, uint fileID)
        {
            this.TypeID = typeID;
            this.FileID = fileID;
        }

        public ContentID(string name)
        {
            this.FileName = name;
        }

        public ContentID(long v)
        {
            this.TypeID = (uint)v;
            this.FileID = (uint)(v >> 32);
        }

        public ulong Shift()
        {
            var fileIDLong = ((ulong)FileID) << 32;
            return fileIDLong | TypeID;
        }
    }
}
