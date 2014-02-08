using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using GonzoNet;
using GonzoNet.Encryption;
using LoginDataModel;
using LoginDataModel.Entities;
using ProtocolAbstractionLibraryD;
using ProtocolAbstractionLibraryD.VM;
using SimsLib.ThreeD;

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
            string AccountName = SanitizeAccount(Encoding.ASCII.GetString(AccountNameBuf));
            Logger.LogInfo("Accountname: " + AccountName + "\r\n");

            byte HashLength = (byte)P.ReadByte();
            byte[] HashBuf = new byte[HashLength];
            P.Read(HashBuf, 0, HashLength);

            if (AccountName == "")
            {
                PacketStream OutPacket = new PacketStream((byte)PacketType.LOGIN_FAILURE, 2);
                OutPacket.WriteHeader();
                OutPacket.WriteByte(0x01);
                Client.Send(OutPacket.ToArray());

                Logger.LogInfo("Bad accountname - sent SLoginFailResponse!\r\n");
                Client.Disconnect();
                return;
            }

            using (var db = DataAccess.Get())
            {
                var account = db.Accounts.GetByUsername(AccountName);
                byte KeyLength = (byte)P.ReadByte();
                byte[] EncKey = new byte[KeyLength];
                P.Read(EncKey, 0, KeyLength);

                //TODO: Do something with this...
                byte Version1 = (byte)P.ReadByte();
                byte Version2 = (byte)P.ReadByte();
                byte Version3 = (byte)P.ReadByte();
                byte Version4 = (byte)P.ReadByte();

                string ClientVersion = Version1.ToString() + "." + Version2.ToString() + "." + Version3.ToString() +
                    "." + Version4.ToString();

                if (ClientVersion != GlobalSettings.Default.ClientVersion)
                {
                    PacketStream OutPacket = new PacketStream((byte)PacketType.INVALID_VERSION, 2);
                    OutPacket.WriteHeader();
                    OutPacket.WriteByte(0x01);
                    Client.Send(OutPacket.ToArray());

                    Logger.LogInfo("Bad version - sent SInvalidVersion!\r\n");
                    Client.Disconnect();
                    return;
                }

                if (!GlobalSettings.Default.CreateAccountsOnLogin)
                {
                    Logger.LogInfo("Done reading LoginRequest, checking account...\r\n");

                    if (account == null)
                    {
                        PacketStream OutPacket = new PacketStream((byte)PacketType.LOGIN_FAILURE, 2);
                        OutPacket.WriteHeader();
                        OutPacket.WriteByte(0x01);
                        Client.Send(OutPacket.ToArray());

                        Logger.LogInfo("Bad accountname - sent SLoginFailResponse!\r\n");
                        Client.Disconnect();
                        return;
                    }
                    else
                        Client.ClientEncryptor = new ARC4Encryptor(account.Password, EncKey);
                }
                else
                {
                    if (account == null)
                    {
                        try
                        {
                            db.Accounts.Create(new Account
                            {
                                AccountName = AccountName.ToLower(),
                                Password = Convert.ToBase64String(HashBuf)
                            });
                        }
                        catch (Exception)
                        {
                            PacketStream OutPacket = new PacketStream((byte)PacketType.LOGIN_FAILURE, 2);
                            OutPacket.WriteHeader();
                            OutPacket.WriteByte(0x01);
                            Client.Send(OutPacket.ToArray());

                            Logger.LogInfo("Bad accountname - sent SLoginFailResponse!\r\n");
                            Client.Disconnect();
                            return;
                        }

                        account = db.Accounts.GetByUsername(AccountName);
                    }

                    Client.ClientEncryptor = new ARC4Encryptor(account.Password, EncKey);
                }

                if (account.IsCorrectPassword(AccountName, HashBuf))
                {
                    //0x01 = InitLoginNotify
                    PacketStream OutPacket = new PacketStream((byte)PacketType.LOGIN_NOTIFY, 1);
                    OutPacket.WriteHeader();
                    OutPacket.WriteByte(0x01);
                    Client.ClientEncryptor.Username = AccountName;
                    Client.Send(OutPacket.ToArray());

                    Logger.LogInfo("Sent InitLoginNotify!\r\n");
                }
                else
                {
                    PacketStream OutPacket = new PacketStream((byte)PacketType.LOGIN_FAILURE, 2);
                    OutPacket.WriteHeader();
                    OutPacket.WriteByte(0x02);
                    Client.Send(OutPacket.ToArray());

                    Logger.LogInfo("Bad password - sent SLoginFailResponse!\r\n");
                    Client.Disconnect();
                    return;
                }
            }

            //Client was modified, update it.
            NetworkFacade.ClientListener.UpdateClient(Client);
        }

        public static void HandleCharacterInfoRequest(NetworkClient Client, ProcessedPacket P)
        {
            Logger.LogInfo("Received CharacterInfoRequest!");

            DateTime Timestamp = DateTime.Parse(P.ReadPascalString());

            //Database.CheckCharacterTimestamp(Client.Username, Client, TimeStamp);

            Character[] Characters = new Character[] { };

            using (var db = DataAccess.Get())
            {
                var account = db.Accounts.GetByUsername(Client.ClientEncryptor.Username);
                Characters = db.Characters.GetForAccount((int)account.AccountID).ToArray();
            }

            if (Characters != null)
            {
                PacketStream Packet = new PacketStream((byte)PacketType.CHARACTER_LIST, 0);
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
                    PacketWriter.Write(avatar.Description);
                    PacketWriter.Write((ulong)avatar.HeadOutfitID);
                    PacketWriter.Write((ulong)avatar.BodyOutfitID);
                    PacketWriter.Write((byte)avatar.AppearanceType);
                    PacketWriter.Write((string)avatar.CityName);
                    PacketWriter.Write((ulong)avatar.CityThumb);
                    PacketWriter.Write((string)avatar.City);
                    PacketWriter.Write((ulong)avatar.CityMap);
                    PacketWriter.Write((string)avatar.CityIp);
                    PacketWriter.Write((int)avatar.CityPort);
                }

                Packet.Write(PacketData.ToArray(), 0, (int)PacketData.Length);
                PacketWriter.Close();
                Client.SendEncrypted(0x05, Packet.ToArray());
            }
            else //No characters existed for the account.
            {
                PacketStream Packet = new PacketStream(0x05, 0);
                Packet.WriteByte(0x00); //0 characters.

                Client.SendEncrypted((byte)PacketType.CHARACTER_LIST, Packet.ToArray());
            }
        }

        public static void HandleCityInfoRequest(NetworkClient Client, ProcessedPacket P)
        {
            //This packet only contains a dummy byte, don't bother reading it.
            PacketStream Packet = new PacketStream((byte)PacketType.CITY_LIST, 0);
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

            Client.SendEncrypted((byte)PacketType.CITY_LIST, Packet.ToArray());
        }

        public static void HandleCharacterCreate(NetworkClient Client, ProcessedPacket P)
        {
            Logger.LogInfo("Received CharacterCreate!");

            string AccountName = SanitizeAccount(P.ReadPascalString());

            using (var db = DataAccess.Get())
            {
                Account Acc = db.Accounts.GetByUsername(AccountName);

                //TODO: Send GUID to client...
                SimBase Char = new SimBase(Guid.NewGuid());
                Char.Timestamp = P.ReadPascalString();
                Char.Name = P.ReadPascalString();
                Char.Sex = P.ReadPascalString();
                Char.Description = P.ReadPascalString();
                Char.HeadOutfitID = P.ReadUInt64();
                Char.BodyOutfitID = P.ReadUInt64();
                Char.Appearance = (AppearanceType)P.ReadByte();
                Char.ResidingCity = new CityInfo(P.ReadPascalString(), "", P.ReadUInt64(), P.ReadPascalString(),
                    P.ReadUInt64(), P.ReadPascalString(), P.ReadInt32());
                Char.CreatedThisSession = true;

                var characterModel = new Character();
                characterModel.Name = Char.Name;
                characterModel.Sex = Char.Sex;
                characterModel.Description = Char.Description;
                characterModel.LastCached = Char.Timestamp;
                characterModel.GUID = Char.GUID;
                characterModel.HeadOutfitID = (long)Char.HeadOutfitID;
                characterModel.BodyOutfitID = (long)Char.BodyOutfitID;
                characterModel.AccountID = Acc.AccountID;
                characterModel.AppearanceType = (int)Char.Appearance;
                characterModel.City = Char.ResidingCity.UUID;
                characterModel.CityName = Char.ResidingCity.Name;
                characterModel.CityThumb = (long)Char.ResidingCity.Thumbnail;
                characterModel.CityMap = (long)Char.ResidingCity.Map;
                characterModel.CityIp = Char.ResidingCity.IP;
                characterModel.CityPort = Char.ResidingCity.Port;

                var status = db.Characters.CreateCharacter(characterModel);
                //Need to be variable length, because the success packet contains a token.
                PacketStream CCStatusPacket = new PacketStream((byte)PacketType.CHARACTER_CREATION_STATUS, 0);

                switch (status)
                {
                    case LoginDataModel.Entities.CharacterCreationStatus.NameAlreadyExisted:
                        CCStatusPacket.WriteByte((int)LoginDataModel.Entities.CharacterCreationStatus.NameAlreadyExisted);
                        Client.SendEncrypted(CCStatusPacket.PacketID, CCStatusPacket.ToArray());
                        break;
                    case LoginDataModel.Entities.CharacterCreationStatus.ExceededCharacterLimit:
                        CCStatusPacket.WriteByte((int)LoginDataModel.Entities.CharacterCreationStatus.ExceededCharacterLimit);
                        Client.SendEncrypted(CCStatusPacket.PacketID, CCStatusPacket.ToArray());
                        break;
                    case LoginDataModel.Entities.CharacterCreationStatus.Success:
                        CCStatusPacket.WriteByte((int)LoginDataModel.Entities.CharacterCreationStatus.Success);
                        CCStatusPacket.WritePascalString(Char.GUID.ToString());
                        Guid Token = Guid.NewGuid();
                        CCStatusPacket.WritePascalString(Token.ToString());
                        Client.SendEncrypted(CCStatusPacket.PacketID, CCStatusPacket.ToArray());

                        foreach (CityServerClient CServer in NetworkFacade.CServerListener.CityServers)
                        {
                            if (CServer.ServerInfo.UUID == Char.ResidingCity.UUID)
                            {
                                PacketStream CServerPacket = new PacketStream(0x01, 0);
                                CServerPacket.WriteHeader();

                                ushort PacketLength = (ushort)(PacketHeaders.UNENCRYPTED + 4 + (Client.RemoteIP.Length + 1)
                                    + (Char.GUID.ToString().Length + 1) + (Token.ToString().Length + 1));
                                CServerPacket.WriteUInt16(PacketLength);

                                CServerPacket.WriteInt32(Acc.AccountID);
                                CServerPacket.WritePascalString(Client.RemoteIP);
                                CServerPacket.WritePascalString(Char.GUID.ToString());
                                CServerPacket.WritePascalString(Token.ToString());
                                CServer.Send(CServerPacket.ToArray());

                                break;
                            }
                        }

                        break;
                }
            }

            //Client was modified, so update it.
            NetworkFacade.ClientListener.UpdateClient(Client);
        }

        public static void HandleCityTokenRequest(NetworkClient Client, ProcessedPacket P)
        {
            string AccountName = P.ReadPascalString();
            string CityGUID = P.ReadPascalString();
            string CharGUID = P.ReadPascalString();
            string Token = new Guid().ToString();

            foreach (CityServerClient CServer in NetworkFacade.CServerListener.CityServers)
            {
                if (CityGUID == CServer.ServerInfo.UUID)
                {
                    using (var db = DataAccess.Get())
                    {
                        Account Acc = db.Accounts.GetByUsername(AccountName);

                        PacketStream CServerPacket = new PacketStream(0x01, 0);
                        CServerPacket.WriteHeader();

                        ushort PacketLength = (ushort)(PacketHeaders.UNENCRYPTED + 4 + (Client.RemoteIP.Length + 1)
                            + (CharGUID.ToString().Length + 1) + (Token.ToString().Length + 1));
                        CServerPacket.WriteUInt16(PacketLength);

                        CServerPacket.WriteInt32(Acc.AccountID);
                        CServerPacket.WritePascalString(Client.RemoteIP);
                        CServerPacket.WritePascalString(CharGUID.ToString());
                        CServerPacket.WritePascalString(Token.ToString());
                        CServer.Send(CServerPacket.ToArray());

                        break;
                    }
                }
            }

            PacketStream Packet = new PacketStream((byte)PacketType.REQUEST_CITY_TOKEN, 0);
            Packet.WritePascalString(Token);
            Client.SendEncrypted((byte)PacketType.REQUEST_CITY_TOKEN, Packet.ToArray());
        }

        private static string SanitizeAccount(string AccountName)
        {
            return AccountName.Replace("İ", "I");
        }
    }
}