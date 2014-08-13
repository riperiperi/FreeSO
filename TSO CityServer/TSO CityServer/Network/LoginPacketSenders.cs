/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO CityServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using GonzoNet;

namespace TSO_CityServer.Network
{
    public class LoginPacketSenders
    {
        /// <summary>
        /// Send information about this CityServer to the LoginServer...
        /// </summary>
        /// <param name="Client">The client connected to the LoginServer.</param>
        public static void SendServerInfo(NetworkClient Client)
        {
            PacketStream Packet = new PacketStream(0x64, 0);
            Packet.WriteByte(0x64);

            MemoryStream PacketBody = new MemoryStream();
            BinaryWriter PacketWriter = new BinaryWriter(PacketBody);

            PacketWriter.Write((string)GlobalSettings.Default.CityName);
            PacketWriter.Write((string)GlobalSettings.Default.CityDescription);
            PacketWriter.Write((string)Settings.BINDING.Address.ToString());
            PacketWriter.Write((int)Settings.BINDING.Port);
            PacketWriter.Write((byte)1); //CityInfoStatus.OK
            PacketWriter.Write((ulong)GlobalSettings.Default.CityThumbnail);
            PacketWriter.Write((string)GlobalSettings.Default.ServerID);
            PacketWriter.Write((ulong)GlobalSettings.Default.Map);
            PacketWriter.Flush();

            Packet.WriteUInt16((ushort)(PacketBody.ToArray().Length + PacketHeaders.UNENCRYPTED));

            Packet.Write(PacketBody.ToArray(), 0, (int)PacketWriter.BaseStream.Length);
            Packet.Flush();

            PacketWriter.Close();

            Client.Send(Packet.ToArray());
        }
    }
}
