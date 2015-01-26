/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
            Packet.WriteHeader();

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

            Packet.Write(PacketBody.ToArray(), 0, (int)PacketBody.ToArray().Length);
            Packet.Flush();

            PacketWriter.Close();

            Client.Send(Packet.ToArray());
        }
    }
}