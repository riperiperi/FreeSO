using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace XNAWinForms
{
    class Outfit
    {
        private uint m_Version;
        private ulong m_LightAppearanceID, m_MediumAppearanceID, m_DarkAppearanceID;

        public ulong LightAppearanceID
        {
            get { return m_LightAppearanceID; }
        }

        public ulong MediumAppearanceID
        {
            get { return m_MediumAppearanceID; }
        }

        public ulong DarkAppearanceID
        {
            get { return m_DarkAppearanceID; }
        }

        public Outfit(byte[] FileData)
        {
            MemoryStream MemStream = new MemoryStream(FileData);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());

            Reader.ReadUInt32(); //Unknown.

            m_LightAppearanceID = Endian.SwapUInt64(Reader.ReadUInt64());
            m_MediumAppearanceID = Endian.SwapUInt64(Reader.ReadUInt64());
            m_DarkAppearanceID = Endian.SwapUInt64(Reader.ReadUInt64());

            Reader.Close();
        }
    }
}
