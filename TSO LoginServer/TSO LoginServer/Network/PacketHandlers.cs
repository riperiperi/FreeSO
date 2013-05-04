/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;
using TSO_LoginServer.Network.Encryption;

namespace TSO_LoginServer.Network
{
    /// <summary>
    /// Contains static methods for handling incoming packets.
    /// </summary>
    class PacketHandlers
    {
        public static void HandleLoginRequest(PacketStream P, ref LoginClient Client)
        {
            Logger.LogInfo("Received LoginRequest!\r\n");

            byte PacketLength = (byte)P.ReadByte();

            byte AccountStrLength = (byte)P.ReadByte();

            byte[] AccountNameBuf = new byte[AccountStrLength];
            P.Read(AccountNameBuf, 0, AccountStrLength);
            string AccountName = Encoding.ASCII.GetString(AccountNameBuf);
            Logger.LogInfo("Accountname: " + AccountName + "\r\n");

            byte HashLength = (byte)P.ReadByte();
            byte[] HashBuf = new byte[HashLength];
            P.Read(HashBuf, 0, HashLength);

            Client.Hash = HashBuf;

            byte KeyLength = (byte)P.ReadByte();
            Client.EncKey = new byte[KeyLength];
            P.Read(Client.EncKey, 0, KeyLength);

            byte Version1 = (byte)P.ReadByte();
            byte Version2 = (byte)P.ReadByte();
            byte Version3 = (byte)P.ReadByte();
            byte Version4 = (byte)P.ReadByte();

            Logger.LogInfo("Done reading LoginRequest, checking account...\r\n");

            //Database.CheckAccount(AccountName, Client, HashBuf);

            if (Account.DoesAccountExist(AccountName) && Account.IsCorrectPassword(AccountName, HashBuf))
            {
                //0x01 = InitLoginNotify
                PacketStream OutPacket = new PacketStream(0x01, 2);
                OutPacket.WriteByte(0x01);
                OutPacket.WriteByte(0x01);
                Client.Send(OutPacket.ToArray());

                Logger.LogInfo("Sent InitLoginNotify!\r\n");
            }
            else
            {
                PacketStream OutPacket = new PacketStream(0x02, 2);
                P.WriteByte(0x02);
                P.WriteByte(0x01);
                Client.Send(P.ToArray());

                Logger.LogInfo("Bad accountname - sent SLoginFailResponse!\r\n");
                Client.Disconnect();
            }
        }

        public static void HandleCharacterInfoRequest(PacketStream P, LoginClient Client)
        {
            byte PacketLength = (byte)P.ReadByte();
            //Length of the unencrypted data, excluding the header (ID, length, unencrypted length).
            byte UnencryptedLength = (byte)P.ReadByte();

            P.DecryptPacket(Client.EncKey, Client.CryptoService, UnencryptedLength);

            Logger.LogDebug("Received CharacterInfoRequest!");

            byte Length = (byte)P.ReadByte();
            byte[] StrBuf = new byte[Length];
            P.Read(StrBuf, 0, Length - 1);
            DateTime TimeStamp = DateTime.Parse(Encoding.ASCII.GetString(StrBuf));

            Database.CheckCharacterTimestamp(Client.Username, Client, TimeStamp);
        }

        public static void HandleCharacterCreate(PacketStream P, ref LoginClient Client, 
            ref CityServerListener CServerListener)
        {
            byte PacketLength = (byte)P.ReadByte();
            //Length of the unencrypted data, excluding the header (ID, length, unencrypted length).
            byte UnencryptedLength = (byte)P.ReadByte();

            P.DecryptPacket(Client.EncKey, Client.CryptoService, UnencryptedLength);

            Logger.LogDebug("Received CharacterCreate!");

            string AccountName = P.ReadString();

            Sim Character = new Sim(P.ReadString());
            Character.Timestamp = P.ReadString();
            Character.Name = P.ReadString();
            Character.Sex = P.ReadString();
            Character.CreatedThisSession = true;

            Client.CurrentlyActiveSim = Character;

            Database.CreateCharacter(Client, Character, ref CServerListener);
        }
    }
}
