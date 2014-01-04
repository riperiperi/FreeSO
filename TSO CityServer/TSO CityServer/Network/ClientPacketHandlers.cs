using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using CityDataModel;
using SimsLib.ThreeD;
using ProtocolAbstractionLibraryD;
using ProtocolAbstractionLibraryD.VM;
using GonzoNet;
using GonzoNet.Encryption;

namespace TSO_CityServer.Network
{
    class ClientPacketHandlers
    {
        public static void HandleCharacterCreate(NetworkClient Client, ProcessedPacket P)
        {
            Logger.LogDebug("Received CharacterCreate!");

            bool ClientAuthenticated = false;

            byte AccountStrLength = (byte)P.ReadByte();
            byte[] AccountNameBuf = new byte[AccountStrLength];
            P.Read(AccountNameBuf, 0, AccountStrLength);
            string AccountName = Encoding.ASCII.GetString(AccountNameBuf);

            using (var db = DataAccess.Get())
            {
                var account = db.Accounts.GetByUsername(AccountName);

                byte KeyLength = (byte)P.ReadByte();
                byte[] EncKey = new byte[KeyLength];
                P.Read(EncKey, 0, KeyLength);
                Client.ClientEncryptor = new ARC4Encryptor(account.Password, EncKey);
                Client.ClientEncryptor.Username = AccountName;

                string Token = P.ReadString();
                string GUID = "";

                foreach (ClientToken CToken in NetworkFacade.TransferringClients.GetList())
                {
                    if (CToken.ClientIP == Client.RemoteIP)
                    {
                        if (CToken.Token == Token)
                        {
                            PacketStream SuccessPacket = new PacketStream((byte)PacketType.CHARACTER_CREATE_CITY, (int)(PacketHeaders.ENCRYPTED + 1));
                            SuccessPacket.WriteByte((byte)CityDataModel.Entities.CharacterCreationStatus.Success);
                            Client.SendEncrypted((byte)PacketType.CHARACTER_CREATE_CITY, SuccessPacket.ToArray());
                            ClientAuthenticated = true;

                            GUID = CToken.CharacterGUID;
                        }

                        break;
                    }
                }

                SimBase Char = new SimBase(new Guid(GUID));
                Char.Timestamp = P.ReadPascalString();
                Char.Name = P.ReadPascalString();
                Char.Sex = P.ReadPascalString();
                Char.Description = P.ReadPascalString();
                Char.HeadOutfitID = P.ReadUInt64();
                Char.BodyOutfitID = P.ReadUInt64();
                Char.Appearance = (AppearanceType)P.ReadByte();
                Char.CreatedThisSession = true;

                var characterModel = new Character();
                characterModel.Name = Char.Name;
                characterModel.Sex = Char.Sex;
                characterModel.Description = Char.Description;
                characterModel.LastCached = Char.Timestamp;
                characterModel.GUID = Char.GUID;
                characterModel.HeadOutfitID = (long)Char.HeadOutfitID;
                characterModel.BodyOutfitID = (long)Char.BodyOutfitID;
                characterModel.AccountID = account.AccountID;
                characterModel.AppearanceType = (int)Char.Appearance;
                characterModel.City = Char.CityID.ToString();

                var status = db.Characters.CreateCharacter(characterModel);
            }

            //Invalid token, should never occur...
            if (!ClientAuthenticated)
            {
                PacketStream SuccessPacket = new PacketStream(0x65, (int)(PacketHeaders.ENCRYPTED + 1));
                SuccessPacket.WriteByte((byte)CityDataModel.Entities.CharacterCreationStatus.GeneralError);
                Client.SendEncrypted(0x64, SuccessPacket.ToArray());
                Client.Disconnect();
            }
        }

        public static void HandleCityToken(NetworkClient Client, ProcessedPacket P)
        {
            bool ClientAuthenticated = false;

            byte AccountStrLength = (byte)P.ReadByte();
            byte[] AccountNameBuf = new byte[AccountStrLength];
            P.Read(AccountNameBuf, 0, AccountStrLength);
            string AccountName = Encoding.ASCII.GetString(AccountNameBuf);
            Logger.LogInfo("Accountname: " + AccountName + "\r\n");

            using (var db = DataAccess.Get())
            {
                var account = db.Accounts.GetByUsername(AccountName);

                byte KeyLength = (byte)P.ReadByte();
                byte[] EncKey = new byte[KeyLength];
                P.Read(EncKey, 0, KeyLength);
                Client.ClientEncryptor = new ARC4Encryptor(account.Password, EncKey);

                string Token = P.ReadString();

                foreach (ClientToken Tok in NetworkFacade.TransferringClients.GetList())
                {
                    if (Tok.Token == Token)
                    {
                        ClientAuthenticated = true;
                        PacketStream SuccessPacket = new PacketStream((byte)PacketType.CITY_TOKEN, 0);
                        SuccessPacket.WriteByte((byte)CityTransferStatus.Success);
                        Client.SendEncrypted((byte)PacketType.CITY_TOKEN, SuccessPacket.ToArray());
                    }
                }

                if (!ClientAuthenticated)
                {
                    PacketStream ErrorPacket = new PacketStream((byte)PacketType.CITY_TOKEN, 0);
                    ErrorPacket.WriteByte((byte)CityTransferStatus.GeneralError);
                    Client.SendEncrypted((byte)PacketType.CITY_TOKEN, ErrorPacket.ToArray());
                }
            }
        }

        public static void HandleCreateSimulationObject(ProcessedPacket P, NetworkClient C)
        {
            byte PacketLength = (byte)P.ReadUShort();
            //Length of the unencrypted data, excluding the header (ID, length, unencrypted length).
            byte UnencryptedLength = (byte)P.ReadUShort();

            BinaryFormatter BinFormatter = new BinaryFormatter();
            SimulationObject CreatedSimObject = (SimulationObject)BinFormatter.Deserialize(P);

            //TODO: Add the object to the client's lot's VM...
        }
    }
}
