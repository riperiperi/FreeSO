using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using GonzoNet;
using GonzoNet.Encryption;
using LoginDataModel;
using LoginDataModel.Entities;
using ProtocolAbstractionLibraryD;
using TSO.Vitaboy;

namespace TSO_LoginServer.Network
{
    class LoginPacketHandlers
    {
        private static RNGCryptoServiceProvider m_Random = new RNGCryptoServiceProvider();
        public static ECDiffieHellmanCng ServerKey; 

        /// <summary>
        /// Client wanted to log in!
        /// </summary>
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
                        Client.ClientEncryptor = new ARC4Encryptor(account.Password);
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

                    Client.ClientEncryptor = new ARC4Encryptor(account.Password);
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

        public static void HandleLoginRequest2(NetworkClient Client, ProcessedPacket P)
        {
            try
            {
                Logger.LogInfo("Received LoginRequest!\r\n");

                byte[] Nonce = P.ReadBytes(16);
                string AccountName = SanitizeAccount(P.ReadPascalString());

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
                    Account Acc = db.Accounts.GetByUsername(AccountName);

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

                        if (Acc == null)
                        {
                            PacketStream OutPacket = new PacketStream((byte)PacketType.LOGIN_FAILURE, 2);
                            OutPacket.WriteHeader();
                            OutPacket.WriteByte(0x01);
                            Client.Send(OutPacket.ToArray());

                            Logger.LogInfo("Bad accountname - sent SLoginFailResponse!\r\n");
                            Client.Disconnect();
                            return;
                        }
                    }

                    ECDiffieHellmanPublicKey ClientPub = ECDiffieHellmanCngPublicKey.FromByteArray(
                        Account.GetPublicKey(AccountName), CngKeyBlobFormat.EccPublicBlob);
                    byte[] ClientKey = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.Key;
                    byte[] ClientIV = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.IV;
                    byte[] ChallengeResponse = new byte[16];
                    m_Random.GetNonZeroBytes(ChallengeResponse);

                    //This is a slight hack, because the Challenge var doesn't belong in the Encryptor class.
                    AESEncryptor Enc = (AESEncryptor)Client.ClientEncryptor;
                    Enc.Challenge = ChallengeResponse;
                    Client.ClientEncryptor = Enc;

                    PacketStream EncryptedPacket = new PacketStream((byte)PacketType.LOGIN_NOTIFY, 0);
                    EncryptedPacket.WriteHeader();

                    MemoryStream StreamToEncrypt = new MemoryStream();
                    BinaryWriter Writer = new BinaryWriter(StreamToEncrypt);
                    Writer.Write((byte)ChallengeResponse.Length);
                    Writer.Write(ChallengeResponse, 0, ChallengeResponse.Length);
                    Writer.Write((byte)ClientKey.Length);
                    Writer.Write(ClientKey, 0, ClientKey.Length);
                    Writer.Write((byte)ClientIV.Length);
                    Writer.Write(ClientIV, 0, ClientIV.Length);
                    Writer.Flush();

                    byte[] EncryptedData = StaticStaticDiffieHellman.Encrypt(ServerKey, ClientPub, Nonce,
                        StreamToEncrypt.ToArray());

                    EncryptedPacket.WriteUInt16((ushort)(PacketHeaders.UNENCRYPTED + 1 + EncryptedData.Length));

                    EncryptedPacket.WriteByte((byte)EncryptedData.Length);
                    EncryptedPacket.Write(EncryptedData, 0, EncryptedData.Length);

                    Client.Send(EncryptedPacket.ToArray());
                }

                //Client was modified, update it.
                NetworkFacade.ClientListener.UpdateClient(Client);
            }
            catch (Exception E)
            {
                Logger.LogDebug("Error while handling login request, disconnecting client: " +
                    E.ToString());
            }
        }

        public static void HandleChallengeResponse(NetworkClient Client, ProcessedPacket P)
        {
            PacketStream OutPacket;

            byte[] CResponse = P.ReadBytes(P.ReadByte());

            AESEncryptor Enc = (AESEncryptor)Client.ClientEncryptor;

            if (Enc.Challenge.SequenceEqual(CResponse))
            {
                //TODO: Send authentication packet.
                OutPacket = new PacketStream((byte)PacketType.LOGIN_SUCCESS, 2);
                OutPacket.WriteHeader();
                OutPacket.WriteByte(0x01);
                Client.Send(OutPacket.ToArray());

                return;
            }

            OutPacket = new PacketStream((byte)PacketType.LOGIN_FAILURE, 2);
            OutPacket.WriteHeader();
            OutPacket.WriteByte(0x03); //Bad challenge response.
            Client.Send(OutPacket.ToArray());

            Logger.LogInfo("Bad challenge response - sent SLoginFailResponse!\r\n");
            Client.Disconnect();
            return;
        }

        /// <summary>
        /// Client requested information about its characters.
        /// </summary>
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

                int NumChars = 0;

                foreach (Character avatar in Characters)
                {
                    //Zero means same, less than zero means T1 is earlier than T2, more than zero means T1 is later.
                    if (DateTime.Compare(Timestamp, DateTime.Parse(avatar.LastCached)) < 0)
                    {
                        NumChars++;

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
                }

                Packet.WriteByte((byte)NumChars);
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

        /// <summary>
        /// Client requested information about a city.
        /// </summary>
        public static void HandleCityInfoRequest(NetworkClient Client, ProcessedPacket P)
        {
            //This packet only contains a dummy byte, don't bother reading it.
            PacketStream Packet = new PacketStream((byte)PacketType.CITY_LIST, 0);
            MemoryStream PacketData = new MemoryStream();
            BinaryWriter PacketWriter = new BinaryWriter(PacketData);
            PacketWriter.Write((byte)NetworkFacade.CServerListener.CityServers.Count);

            //foreach (CityServerClient City in NetworkFacade.CServerListener.CityServers)
            for(int i = 0; i < NetworkFacade.CServerListener.CityServers.Count; i++)
            {
                PacketWriter.Write((string)NetworkFacade.CServerListener.CityServers[i].ServerInfo.Name);
                PacketWriter.Write((string)NetworkFacade.CServerListener.CityServers[i].ServerInfo.Description);
                PacketWriter.Write((string)NetworkFacade.CServerListener.CityServers[i].ServerInfo.IP);
                PacketWriter.Write((int)NetworkFacade.CServerListener.CityServers[i].ServerInfo.Port);

                //Hack (?) to ensure status is written correctly.
                switch (NetworkFacade.CServerListener.CityServers[i].ServerInfo.Status)
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

                PacketWriter.Write((ulong)NetworkFacade.CServerListener.CityServers[i].ServerInfo.Thumbnail);
                PacketWriter.Write((string)NetworkFacade.CServerListener.CityServers[i].ServerInfo.UUID);
                PacketWriter.Write((ulong)NetworkFacade.CServerListener.CityServers[i].ServerInfo.Map);

                PacketWriter.Flush();
            }

            Packet.Write(PacketData.ToArray(), 0, PacketData.ToArray().Length);
            PacketWriter.Close();

            Client.SendEncrypted((byte)PacketType.CITY_LIST, Packet.ToArray());
        }

        /// <summary>
        /// Client created a character!
        /// </summary>
        public static void HandleCharacterCreate(NetworkClient Client, ProcessedPacket P)
        {
            Logger.LogInfo("Received CharacterCreate!");

            string AccountName = SanitizeAccount(P.ReadPascalString());

            using (var db = DataAccess.Get())
            {
                Account Acc = db.Accounts.GetByUsername(AccountName);

                //TODO: Send GUID to client...
                Sim Char = new Sim(Guid.NewGuid());
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

                        //This actually updates the record, not sure how.
                        Acc.NumCharacters++;

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

        /// <summary>
        /// Client wanted to transfer to a city server.
        /// </summary>
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

        /// <summary>
        /// Client wanted to retire a character.
        /// </summary>
        public static void HandleCharacterRetirement(NetworkClient Client, ProcessedPacket P)
        {
            PacketStream Packet;

            string AccountName = P.ReadPascalString();
            string GUID = P.ReadPascalString();

            using (var db = DataAccess.Get())
            {
                Account Acc = db.Accounts.GetByUsername(AccountName);
                IQueryable<Character> Query = db.Characters.GetForAccount(Acc.AccountID);

                //FUCK, I hate LINQ.
                Guid CharGUID = new Guid(GUID);
                Character Char = Query.Where(x => x.GUID == CharGUID).SingleOrDefault();
                db.Characters.RetireCharacter(Char);

                //This actually updates the record, not sure how.
                Acc.NumCharacters--;

                for (int i = 0; i < NetworkFacade.CServerListener.CityServers.Count; i++)
                {
                    if (NetworkFacade.CServerListener.CityServers[i].ServerInfo.Name == Char.CityName)
                    {
                        Packet = new PacketStream(0x02, 0);
                        Packet.WriteHeader();

                        ushort PacketLength = (ushort)(PacketHeaders.UNENCRYPTED + 4 + GUID.Length + 1);

                        Packet.WriteUInt16(PacketLength);
                        Packet.WriteInt32(Acc.AccountID);
                        Packet.WritePascalString(GUID);
                        NetworkFacade.CServerListener.CityServers[i].Send(Packet.ToArray());

                        break;
                    }
                }
            }

            Packet = new PacketStream((byte)PacketType.RETIRE_CHARACTER_STATUS, 0);
            Packet.WritePascalString(GUID);
            Client.SendEncrypted((byte)PacketType.RETIRE_CHARACTER_STATUS, Packet.ToArray());
        }

        private static string SanitizeAccount(string AccountName)
        {
            return AccountName.Replace("İ", "I");
        }
    }
}