/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SimsLib.ThreeD
{
    public struct Handset
    {
        public Hand LeftHand;
        public Hand RightHand;
    }

    public struct Hand
    {
        public ulong IdleGesture;
        public ulong FistGesture;
        public ulong PointingGesture;
    }

    /// <summary>
    /// Represents a HAndGroup for a Sim,
    /// containing a list of appearances
    /// for the Sim's hands.
    /// </summary>
    public class Hag
    {
        private uint m_Version;
        public Handset LightSkin = new Handset(), MediumSkin = new Handset(), DarkSkin = new Handset();

        /// <summary>
        /// Creates a new HAndGroup.
        /// </summary>
        /// <param name="Filedata">The data for the HAndGroup.</param>
        /*public Hag(byte[] Filedata)
        {
            MemoryStream MemStream = new MemoryStream(Filedata);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Reader.ReadUInt32();

            LightSkin.LeftHand = new Hand();
            LightSkin.LeftHand.IdleGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            LightSkin.LeftHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            LightSkin.LeftHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());

            LightSkin.RightHand = new Hand();
            LightSkin.RightHand.IdleGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            LightSkin.RightHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            LightSkin.RightHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());

            MediumSkin.LeftHand = new Hand();
            MediumSkin.LeftHand.IdleGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            MediumSkin.LeftHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            MediumSkin.LeftHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());

            MediumSkin.RightHand = new Hand();
            MediumSkin.RightHand.IdleGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            MediumSkin.RightHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            MediumSkin.RightHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());

            DarkSkin.LeftHand = new Hand();
            DarkSkin.LeftHand.IdleGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            DarkSkin.LeftHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            DarkSkin.LeftHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());

            DarkSkin.RightHand = new Hand();
            DarkSkin.RightHand.IdleGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            DarkSkin.RightHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());
            DarkSkin.RightHand.FistGesture = Endian.SwapUInt64(Reader.ReadUInt64());
        }*/

        public Hag(Stream Str)
        {
            using (IoBuffer Reader = IoBuffer.FromStream(Str))
            {
                Reader.ByteOrder = ByteOrder.BIG_ENDIAN;

                m_Version = Reader.ReadUInt32();

                LightSkin.LeftHand = new Hand();
                LightSkin.LeftHand.IdleGesture = Reader.ReadUInt64();
                LightSkin.LeftHand.FistGesture = Reader.ReadUInt64();
                LightSkin.LeftHand.PointingGesture = Reader.ReadUInt64();

                LightSkin.RightHand = new Hand();
                LightSkin.RightHand.IdleGesture = Reader.ReadUInt64();
                LightSkin.RightHand.FistGesture = Reader.ReadUInt64();
                LightSkin.RightHand.PointingGesture = Reader.ReadUInt64();

                MediumSkin.LeftHand = new Hand();
                MediumSkin.LeftHand.IdleGesture = Reader.ReadUInt64();
                MediumSkin.LeftHand.FistGesture = Reader.ReadUInt64();
                MediumSkin.LeftHand.PointingGesture = Reader.ReadUInt64();

                MediumSkin.RightHand = new Hand();
                MediumSkin.RightHand.IdleGesture = Reader.ReadUInt64();
                MediumSkin.RightHand.FistGesture = Reader.ReadUInt64();
                MediumSkin.RightHand.PointingGesture = Reader.ReadUInt64();

                DarkSkin.LeftHand = new Hand();
                DarkSkin.LeftHand.IdleGesture = Reader.ReadUInt64();
                DarkSkin.LeftHand.FistGesture = Reader.ReadUInt64();
                DarkSkin.LeftHand.PointingGesture = Reader.ReadUInt64();

                DarkSkin.RightHand = new Hand();
                DarkSkin.RightHand.IdleGesture = Reader.ReadUInt64();
                DarkSkin.RightHand.FistGesture = Reader.ReadUInt64();
                DarkSkin.RightHand.PointingGesture = Reader.ReadUInt64();
            }
        }
    }
}
