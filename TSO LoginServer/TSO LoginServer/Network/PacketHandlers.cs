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
using TSODataModel;
using System.Linq;
using TSODataModel.Entities;

namespace TSO_LoginServer.Network
{
    /// <summary>
    /// Contains static methods for handling incoming packets.
    /// </summary>
    class PacketHandlers
    {
        public static void Init()
        {
            Register(0x00, 0, new OnPacketReceive(HandleLoginRequest));
            Register(0x05, 0, new OnPacketReceive(HandleCharacterInfoRequest));
            Register(0x06, 0, new OnPacketReceive(HandleCityInfoRequest));
            Register(0x07, 0, new OnPacketReceive(HandleCharacterCreate));
        }

        /**
         * Framework
         */
        private static Dictionary<byte, PacketHandler> m_Handlers = new Dictionary<byte, PacketHandler>();
        public static void Register(byte id, int size, OnPacketReceive handler)
        {
            m_Handlers.Add(id, new PacketHandler (id, size, handler));
        }

        public static void Handle(PacketStream stream, LoginClient session)
        {
            byte ID = (byte)stream.ReadByte();
            if (m_Handlers.ContainsKey(ID))
            {
                m_Handlers[ID].Handler(ref session, stream);
            }
        }

        public static PacketHandler Get(byte id)
        {
            return m_Handlers[id];
        }

        /**
         * Actual packet handlers
         */
        public static void HandleLoginRequest(ref LoginClient Client, PacketStream P)
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

            using (var db = DataAccess.Get())
            {
                var account = db.Accounts.GetByUsername(AccountName);
                if (account == null)
                {
                    PacketStream OutPacket = new PacketStream(0x02, 2);
                    OutPacket.WriteHeader();
                    OutPacket.WriteByte(0x01);
                    Client.Send(OutPacket);

                    Logger.LogInfo("Bad accountname - sent SLoginFailResponse!\r\n");
                    Client.Disconnect();
                    return;
                }

                if (account.IsCorrectPassword(AccountName, HashBuf))
                {
                    //0x01 = InitLoginNotify
                    PacketStream OutPacket = new PacketStream(0x01, 1);
                    OutPacket.WriteHeader();
                    Client.Username = AccountName;
                    //This is neccessary to encrypt packets.
                    //TODO: Put something else here
                    //Client.Password = Account.GetPassword(AccountName);
                    Client.Send(OutPacket.ToArray());

                    Logger.LogInfo("Sent InitLoginNotify!\r\n");
                }
            }
        }

        public static void HandleCharacterInfoRequest(ref LoginClient Client, PacketStream P)
        {
            ushort PacketLength = (ushort)P.ReadUShort();
            //Length of the unencrypted data, excluding the header (ID, length, unencrypted length).
            ushort UnencryptedLength = (ushort)P.ReadUShort();

            P.DecryptPacket(Client.EncKey, Client.CryptoService, UnencryptedLength);

            Logger.LogDebug("Received CharacterInfoRequest!");

            DateTime Timestamp = DateTime.Parse(P.ReadASCII());

            //Database.CheckCharacterTimestamp(Client.Username, Client, TimeStamp);

            Character[] Characters = new Character[]{};

            using (var db = DataAccess.Get())
            {
                Characters = db.Characters.GetForAccount(Client.AccountID).ToArray();
            }

            if (Characters != null)
            {
                PacketStream Packet = new PacketStream(0x05, 0);
                MemoryStream PacketData = new MemoryStream();
                BinaryWriter PacketWriter = new BinaryWriter(PacketData);

                /**
                 * Whats the point of checking a timestamp here? It saves a few bytes on a packet
                 * sent once per user session. Premature optimization.
                 */
                PacketWriter.Write((byte)Characters.Length);
                foreach(Character avatar in Characters){
                    PacketWriter.Write(avatar.CharacterID);
                    PacketWriter.Write(avatar.GUID.ToString());
                    PacketWriter.Write(avatar.LastCached);
                    PacketWriter.Write(avatar.Name);
                    PacketWriter.Write(avatar.Sex);
                }

                Packet.Write(PacketData.ToArray(), 0, (int)PacketData.Length);
                PacketWriter.Close();
                Client.SendEncrypted(0x05, Packet.ToArray());
            }
            else //No characters existed for the account.
            {
                PacketStream Packet = new PacketStream(0x05, 0);
                Packet.WriteByte(0x00); //0 characters.

                Client.SendEncrypted(0x05, Packet.ToArray());
            }
        }

        public static void HandleCityInfoRequest(ref LoginClient Client, PacketStream P)
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
                PacketWriter.Write((string)City.ServerInfo.Name);
                PacketWriter.Write((string)City.ServerInfo.Description);
                PacketWriter.Write((string)City.ServerInfo.IP);
                PacketWriter.Write((int)City.ServerInfo.Port);

                //Hack (?) to ensure status is written correctly.
                switch (City.ServerInfo.Status)
                {
                    case CityInfoStatus.Ok:
                        PacketWriter.Write((byte)1);
                        break;
                    case CityInfoStatus.Busy:
                        PacketWriter.Write((byte)2);
                        break;
                    case CityInfoStatus.Full:
                        PacketWriter.Write((byte)3);
                        break;
                    case CityInfoStatus.Reserved:
                        PacketWriter.Write((byte)4);
                        break;
                }

                PacketWriter.Write((ulong)City.ServerInfo.Thumbnail);
                PacketWriter.Write((string)City.ServerInfo.UUID);
                PacketWriter.Write((ulong)City.ServerInfo.Map);

                PacketWriter.Flush();
            }

            Packet.Write(PacketData.ToArray(), 0, PacketData.ToArray().Length);
            PacketWriter.Close();

            Client.SendEncrypted(0x06, Packet.ToArray());
        }

        public static void HandleCharacterCreate(ref LoginClient Client, PacketStream P)
        {
            ushort PacketLength = (ushort)P.ReadUShort();
            //Length of the unencrypted data, excluding the header (ID, length, unencrypted length).
            ushort UnencryptedLength = (ushort)P.ReadUShort();

            P.DecryptPacket(Client.EncKey, Client.CryptoService, UnencryptedLength);

            Logger.LogDebug("Received CharacterCreate!");

            string AccountName = P.ReadString();

            //GUID generation should always be done on the server side
            //You cant trust the client side, it may have been hacked
            Sim Char = new Sim(Guid.NewGuid());
            Char.Timestamp = P.ReadString();
            Char.Name = P.ReadString();
            Char.Sex = P.ReadString();
            Char.CreatedThisSession = true;

            Client.CurrentlyActiveSim = Char;

            using (var db = DataAccess.Get())
            {
                var characterModel = new Character();
                characterModel.Name = Char.Name;
                characterModel.Sex = Char.Sex;
                characterModel.LastCached = Char.Timestamp;
                characterModel.GUID = Char.GUID;

                var status = db.Characters.CreateCharacter(characterModel);

                switch (status)
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

    public delegate void OnPacketReceive(ref LoginClient Session, PacketStream Packet);

    public class PacketHandler
    {
        private ushort m_ID;
        private int m_Length;
        private OnPacketReceive m_Handler;

        public PacketHandler(ushort id, int size, OnPacketReceive handler)
        {
            this.m_ID = id;
            this.m_Length = size;
            this.m_Handler = handler;
        }

        public ushort ID
        {
            get{ return m_ID; }
        }

        public int Length {
            get { return m_Length; }
        }

        public OnPacketReceive Handler
        {
            get
            {
                return m_Handler;
            }
        }
    }
}
