using System;
using System.Collections.Generic;
using System.Text;

namespace TSOClient
{
    public class MusicTrack
    {
        private int m_ID = 0x00;
        private int m_Channel;

        public MusicTrack(int ID, int Sound)
        {
            m_ID = ID;
            m_Channel = Sound;
        }

        public int ID
        {
            get { return m_ID; }
        }

        public int ThisChannel
        {
            get { return m_Channel; }
        }
    }
}
