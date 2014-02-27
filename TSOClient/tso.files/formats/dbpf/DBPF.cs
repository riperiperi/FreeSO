using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using tso.files.utils;

namespace tso.files.formats.dbpf
{
    public class DBPF : IDisposable
    {
        public int DateCreated;
        public int DateModified;

        private uint IndexMajorVersion;
        private uint NumEntries;

        private IoBuffer Io;

        public DBPF()
        {
        }

        /// <summary>
        /// Creates a DBPF instance from a path.
        /// </summary>
        /// <param name="file">The path to an DBPF archive.</param>
        public DBPF(string file)
        {
            var stream = File.OpenRead(file);
                Read(stream);
        }

        /// <summary>
        /// Reads a DBPF archive from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        public void Read(Stream stream)
        {
            var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN);
            this.Io = io;
            
            var magic = io.ReadCString(4);
            if (magic != "DBPF"){
                throw new Exception("Not a DBPF file");
            }

            var majorVersion = io.ReadUInt32();
            var minorVersion = io.ReadUInt32();
            var version = majorVersion + (((double)minorVersion)/10.0);

            /** Unknown, set to 0 **/
            io.Skip(12);

            if (version == 1.0){
                this.DateCreated = io.ReadInt32();
                this.DateModified = io.ReadInt32();
            }

            if (version < 2.0){
                IndexMajorVersion = io.ReadUInt32();
            }

            NumEntries = io.ReadUInt32();

            if (version < 2.0){
                var indexOffset = io.ReadUInt32();
            }
            var indexSize = io.ReadUInt32();

            if (version < 2.0){
                var trashEntryCount = io.ReadUInt32();
                var trashIndexOffset = io.ReadUInt32();
                var trashIndexSize = io.ReadUInt32();
                var indexMinor = io.ReadUInt32();
            }
            else if (version == 2.0)
            {
                var indexMinor = io.ReadUInt32();
                var indexOffset = io.ReadUInt32();
                io.Skip(4);
            }

            /** Padding **/
            io.Skip(32);
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes this DBPF instance.
        /// </summary>
        public void Dispose()
        {
            Io.Dispose();
        }

        #endregion
    }
}
