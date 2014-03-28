using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Files.formats.dbpf
{
    /// <summary>
    /// Represents an entry in a DBPF archive.
    /// </summary>
    public class DBPFEntry
    {
        //A 4-byte integer describing what type of file is held
        public uint TypeID;

        //A 4-byte integer identifying what resource group the file belongs to
        public uint GroupID;

        //A 4-byte ID assigned to the file which, together with the Type ID and the second instance ID (if applicable), is assumed to be unique all throughout the game
        public uint InstanceID;
        //too bad we're not using a version with a second instance id!!

        //A 4-byte unsigned integer specifying the offset to the entry's data from the beginning of the archive
        public uint FileOffset;

        //A 4-byte unsigned integer specifying the size of the entry's data
        public uint FileSize;
    }
}
