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

            ushort PacketLength = (ushort)P.ReadUShort();

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
                Client.Username = AccountName;
                //This is neccessary to encrypt packets.
                Client.Password = Account.GetPassword(AccountName);
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
            ushort PacketLength = (ushort)P.ReadUShort();
            //Length of the unencrypted data, excluding the header (ID, length, unencrypted length).
            ushort UnencryptedLength = (ushort)P.ReadUShort();

            P.DecryptPacket(Client.EncKey, Client.CryptoService, UnencryptedLength);

            Logger.LogDebug("Received CharacterInfoRequest!");

            byte Length = (byte)P.ReadByte();
            byte[] StrBuf = new byte[Length];
            P.Read(StrBuf, 0, Length - 1);
            DateTime Timestamp = DateTime.Parse(Encoding.ASCII.GetString(StrBuf));

            //Database.CheckCharacterTimestamp(Client.Username, Client, TimeStamp);

            Character[] Characters = Character.GetCharacters(Client.Username);

            if (Characters != null)
            {
                PacketStream Packet = new PacketStream(0x05, 0);

                MemoryStream PacketData = new MemoryStream();
                BinaryWriter PacketWriter = new BinaryWriter(PacketData);

                //The timestamp for all characters should be equal, so just check the first character.
                if (Timestamp < DateTime.Parse(Characters[0].LastCached) ||
                    Timestamp > DateTime.Parse(Characters[0].LastCached))
                {
                    //Write the characterdata into a temporary buffer.
                    if (Characters.Length == 1)
                    {
                        PacketWriter.Write(Characters[0].CharacterID);
                        PacketWriter.Write(Characters[0].GUID);
                        PacketWriter.Write(Characters[0].LastCached);
                        PacketWriter.Write(Characters[0].Name);
                        PacketWriter.Write(Characters[0].Sex);

                        PacketWriter.Flush();
                    }
                    else if (Characters.Length == 2)
                    {
                        PacketWriter.Write(Characters[0].CharacterID);
                        PacketWriter.Write(Characters[0].GUID);
                        PacketWriter.Write(Characters[0].LastCached);
                        PacketWriter.Write(Characters[0].Name);
                        PacketWriter.Write(Characters[0].Sex);

                        PacketWriter.Write(Characters[1].CharacterID);
                        PacketWriter.Write(Characters[1].GUID);
                        PacketWriter.Write(Characters[1].LastCached);
                        PacketWriter.Write(Characters[1].Name);
                        PacketWriter.Write(Characters[1].Sex);

                        PacketWriter.Flush();
                    }
                    else if (Characters.Length == 3)
                    {
                        PacketWriter.Write(Characters[0].CharacterID);
                        PacketWriter.Write(Characters[0].GUID);
                        PacketWriter.Write(Characters[0].LastCached);
                        PacketWriter.Write(Characters[0].Name);
                        PacketWriter.Write(Characters[0].Sex);

                        PacketWriter.Write(Characters[1].CharacterID);
                        PacketWriter.Write(Characters[1].GUID);
                        PacketWriter.Write(Characters[1].LastCached);
                        PacketWriter.Write(Characters[1].Name);
                        PacketWriter.Write(Characters[1].Sex);

                        PacketWriter.Write(Characters[2].CharacterID);
                        PacketWriter.Write(Characters[2].GUID);
                        PacketWriter.Write(Characters[2].LastCached);
                        PacketWriter.Write(Characters[2].Name);
                        PacketWriter.Write(Characters[2].Sex);

                        PacketWriter.Flush();
                    }

                    Packet.WriteByte((byte)Characters.Length);      //Total number of characters.
                    Packet.Write(PacketData.ToArray(), 0, (int)PacketData.Length);
                    PacketWriter.Close();

                    Client.SendEncrypted(0x05, Packet.ToArray());
                }
            }
            else //No characters existed for the account.
            {
                PacketStream Packet = new PacketStream(0x05, 0);
                Packet.WriteByte(0x00); //0 characters.

                Client.SendEncrypted(0x05, Packet.ToArray());
            }
        }

        public static void HandleCityInfoRequest(PacketStream P, LoginClient Client)
        {
            ushort PacketLength = (ushort)P.ReadUShort();
            //Length of the unencrypted data, excluding the header (ID, length, unencrypted length).
            ushort UnencryptedLength = (ushort)P.ReadUShort();

            P.DecryptPacket(Client.EncKey, Client.CryptoService, UnencryptedLength);

            //This packet only contains a dummy byte, don't bother reading it.

            PacketStream Packet = new PacketStream(0x06, 0);

            MemoryStream PacketData = new MemoryStream();
            BinaryWriter PacketWriter = new BinaryWriter(PacketData);

            PacketWriter.Write((byte)NetworkFacade.CServerListener.CityServers.Count);

            foreach (CityServerClient City in NetworkFacade.CServerListener.CityServers)
            {
                PacketWriter.Write(City.ServerInfo.Name);
                PacketWriter.Write(City.ServerInfo.Description);
                PacketWriter.Write(City.ServerInfo.IP);
                PacketWriter.Write(City.ServerInfo.Port);
                PacketWriter.Write((byte)City.ServerInfo.Status);
                PacketWriter.Write(City.ServerInfo.Thumbnail);
                PacketWriter.Write(City.ServerInfo.UUID);

                PacketWriter.Flush();
            }

            Packet.Write(PacketData.ToArray(), 0, PacketData.ToArray().Length);
            PacketWriter.Close();

            Client.SendEncrypted(0x06, Packet.ToArray());
        }

        public static void HandleCharacterCreate(PacketStream P, ref LoginClient Client, 
            ref CityServerListener CServerListener)
        {
            ushort PacketLength = (ushort)P.ReadUShort();
            //Length of the unencrypted data, excluding the header (ID, length, unencrypted length).
            ushort UnencryptedLength = (ushort)P.ReadUShort();

            P.DecryptPacket(Client.EncKey, Client.CryptoService, UnencryptedLength);

            Logger.LogDebug("Received CharacterCreate!");

            string AccountName = P.ReadString();

            Sim Char = new Sim(P.ReadString());
            Char.Timestamp = P.ReadString();
            Char.Name = P.ReadString();
            Char.Sex = P.ReadString();
            Char.CreatedThisSession = true;

            Client.CurrentlyActiveSim = Char;

            switch (Character.CreateCharacter(Char))
            {
                case CharacterCreationStatus.NameAlreadyExisted:
                    //TODO: Send packet.
                    break;
                case CharacterCreationStatus.ExceededCharacterLimit:
                    //TODO: Send packet.
                    break;
            }
        }
    }
}
