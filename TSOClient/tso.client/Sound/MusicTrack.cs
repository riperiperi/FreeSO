/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace FSO.Client.Sound
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
