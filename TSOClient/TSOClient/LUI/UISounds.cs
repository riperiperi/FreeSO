/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;

namespace TSOClient.LUI
{
    public class UISound
    {
        private int m_ID = 0x00;
        private int m_Channel;

        public UISound(int ID, int Sound)
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

    /// <summary>
    /// Static class that contains preloaded UI-sounds.
    /// So far there's only one sound, the sound of a buttonclick.
    /// </summary>
    class UISounds
    {
        private static List<UISound> m_Sounds = new List<UISound>();

        public static void AddSound(UISound Sound)
        {
            m_Sounds.Add(Sound);
        }

        public static UISound GetSound(int ID)
        {
            foreach (UISound Sound in m_Sounds)
            {
                if(ID == Sound.ID)
                    return Sound;
            }

            return null;
        }
    }
}
