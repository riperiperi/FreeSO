using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GonzoNet;
using GonzoNet.Encryption;
using TSODataModel;
using TSODataModel.Entities;

namespace TSO_LoginServer.Network
{
    class LoginPacketHandlers
    {
        /**
        * Actual packet handlers
        */
        public static void HandleLoginRequest(NetworkClient Client, ProcessedPacket P)
        {
            Logger.LogInfo("Received LoginRequest!\r\n");

            byte AccountStrLength = (byte)P.ReadByte();
            byte[] AccountNameBuf = new byte[AccountStrLength];
            P.Read(AccountNameBuf, 0, AccountStrLength);
            string AccountName = Encoding.ASCII.GetString(AccountNameBuf);
            Logger.LogInfo("Accountname: " + AccountName + "\r\n");

            byte HashLength = (byte)P.ReadByte();
            byte[] HashBuf = new byte[HashLength];
            P.Read(HashBuf, 0, HashLength);

            using (var db = DataAccess.Get())
            {
                var account = db.Accounts.GetByUsername(AccountName);

                byte KeyLength = (byte)P.ReadByte();
                byte[] EncKey = new byte[KeyLength];
                P.Read(EncKey, 0, KeyLength);
                Client.ClientEncryptor = new ARC4Encryptor(account.Password, EncKey);

                byte Version1 = (byte)P.ReadByte();
                byte Version2 = (byte)P.ReadByte();
                byte Version3 = (byte)P.ReadByte();
                byte Version4 = (byte)P.ReadByte();

                Logger.LogInfo("Done reading LoginRequest, checking account...\r\n");

                if (account == null)
                {
                    PacketStream OutPacket = new PacketStream(0x02, 2);
                    OutPacket.WriteHeader();
                    OutPacket.WriteByte(0x01);
                    Client.Send(OutPacket.ToArray());

                    Logger.LogInfo("Bad accountname - sent SLoginFailResponse!\r\n");
                    Client.Disconnect();
                    return;
                }

                if (account.IsCorrectPassword(AccountName, HashBuf))
                {
                    //0x01 = InitLoginNotify
                    PacketStream OutPacket = new PacketStream(0x01, 1);
                    OutPacket.WriteHeader();
                    OutPacket.WriteByte(0x01);
                    Client.ClientEncryptor.Username = AccountName;
                    //This is neccessary to encrypt packets.
                    //TODO: Put something else here
                    //Client.Password = Account.GetPassword(AccountName);
                    Client.Send(OutPacket.ToArray());

                    Logger.LogInfo("Sent InitLoginNotify!\r\n");
                }
            }

            //Client was modified, update it.
            NetworkFacade.ClientListener.UpdateClient(Client);
        }

        public static void HandleCharacterInfoRequest(NetworkClient Client, ProcessedPacket P)
        {
            Logger.LogDebug("Received CharacterInfoRequest!");

            DateTime Timestamp = DateTime.Parse(P.ReadString());

            //Database.CheckCharacterTimestamp(Client.Username, Client, TimeStamp);

            Character[] Characters = new Character[] { };

            using (var db = DataAccess.Get())
            {
                var account = db.Accounts.GetByUsername(Client.ClientEncryptor.Username);
                Characters = db.Characters.GetForAccount((int)account.AccountID).ToArray();
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
                foreach (Character avatar in Characters)
                {
                    PacketWriter.Write((int)avatar.CharacterID);
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

        public static void HandleCityInfoRequest(NetworkClient Client, ProcessedPacket P)
        {
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

        public static void HandleCharacterCreate(NetworkClient Client, ProcessedPacket P)
        {
            Logger.LogDebug("Received CharacterCreate!");

            string AccountName = P.ReadString();

            //GUID generation should always be done on the server side
            //You cant trust the client side, it may have been hacked
            Sim Char = new Sim(Guid.NewGuid());
            Char.Timestamp = P.ReadPascalString();
            Char.Name = P.ReadPascalString();
            Char.Sex = P.ReadPascalString();
            Char.Description = P.ReadPascalString();
            Char.HeadOutfitID = P.ReadUInt64();
            Char.BodyOutfitID = P.ReadUInt64();
            Char.CreatedThisSession = true;

            using (var db = DataAccess.Get())
            {
                var characterModel = new Character();
                characterModel.Name = Char.Name;
                characterModel.Sex = Char.Sex;
                characterModel.LastCached = Char.Timestamp;
                characterModel.GUID = Char.GUID.ToString();

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

            //Client was modified, so update it.
            NetworkFacade.ClientListener.UpdateClient(Client);
        }
    }
}
