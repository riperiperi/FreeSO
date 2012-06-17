using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Iffinator.Flash
{
    /// <summary>
    /// This chunk type holds a number of constants that behavior code can refer to. 
    /// Information about these values can be included in a TRCN chunk with the same ID.
    /// </summary>
    public class BCON : IffChunk
    {
        private byte m_NumConstants;
        private byte m_Type;        //A 1-byte integer typically either 0x00 or 0x80. The purpose of this field is unknown.
        private List<short> m_Constants = new List<short>();

        /// <summary>
        /// A 1-byte unsigned integer specifying the number of constants defined in this chunk.
        /// </summary>
        public byte NumConstants
        {
            get { return m_NumConstants; }
        }

        /// <summary>
        /// The constants in this BCON chunk.
        /// </summary>
        public List<short> Constants
        {
            get { return m_Constants; }
        }

        public BCON(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_NumConstants = Reader.ReadByte();
            m_Type = Reader.ReadByte();

            for (byte i = 0; i < m_NumConstants; i++)
            {
                short Const = Reader.ReadInt16();
                m_Constants.Add(Const);
            }
        }
    }
}
