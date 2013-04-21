/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO MeshViewer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SimsLib;

namespace TSOClient.Code.Data.Model
{
    public class Outfit
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



        private Appearance m_LightAppearance;
        public Appearance LightAppearance
        {
            get
            {
                if (m_LightAppearance == null)
                {
                    m_LightAppearance = new Appearance(ContentManager.GetResourceFromLongID(m_LightAppearanceID));
                }
                return m_LightAppearance;
            }
        }


        private Appearance m_MediumAppearance;
        public Appearance MediumAppearance
        {
            get
            {
                if (m_MediumAppearance == null)
                {
                    m_MediumAppearance = new Appearance(ContentManager.GetResourceFromLongID(m_MediumAppearanceID));
                }
                return m_MediumAppearance;
            }
        }


        private Appearance m_DarkAppearance;
        public Appearance DarkAppearance
        {
            get
            {
                if (m_DarkAppearance == null)
                {
                    m_DarkAppearance = new Appearance(ContentManager.GetResourceFromLongID(m_DarkAppearanceID));
                }
                return m_DarkAppearance;
            }
        }


        public Appearance GetAppearance(AppearanceType type)
        {
            switch (type)
            {
                case AppearanceType.Light:
                    return LightAppearance;

                case AppearanceType.Medium:
                    return MediumAppearance;

                case AppearanceType.Dark:
                    return DarkAppearance;
            }

            return null;
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
