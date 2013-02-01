using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Dressup
{
    class Appearance
    {
        private uint m_Version;
        private ulong m_ThumbnailID;
        private List<ulong> m_BindingIDs = new List<ulong>();

        public ulong ThumbnailID
        {
            get { return m_ThumbnailID; }
        }

        public List<ulong> BindingIDs
        {
            get { return m_BindingIDs; }
        }

        public Appearance(byte[] FileData)
        {
            MemoryStream MemStream = new MemoryStream(FileData);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());
            m_ThumbnailID = Endian.SwapUInt64(Reader.ReadUInt64());
            uint Count = Endian.SwapUInt32(Reader.ReadUInt32());

            for (int i = 0; i < Count; i++)
                BindingIDs.Add(Endian.SwapUInt64(Reader.ReadUInt64()));
        }
    }
}
