using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using TSODataModel;
using TSO_CityServer.VM;
using GonzoNet;
using GonzoNet.Encryption;

namespace TSO_CityServer.Network
{
    class ClientPacketHandlers
    {
        public static AutoResetEvent AResetEvent = new AutoResetEvent(false);

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

                foreach (ClientToken CToken in NetworkFacade.TransferringClients.GetList())
                {
                    if (CToken.ClientIP == Client.RemoteIP)
                    {
                        PacketStream SuccessPacket = new PacketStream(0x64, (int)(PacketHeaders.ENCRYPTED + 1));
                        SuccessPacket.WriteByte((byte)TSODataModel.Entities.CharacterCreationStatus.Success);
                        Client.SendEncrypted(0x64, SuccessPacket.ToArray());
                        ClientAuthenticated = true;

                        break;
                    }
                }

                //TODO: Receive GUID from client...
                Sim Char = new Sim(Guid.NewGuid());
                Char.Timestamp = P.ReadString();
                Char.Name = P.ReadString();
                Char.Sex = P.ReadString();
                Char.Description = P.ReadString();
                Char.HeadOutfitID = P.ReadUInt64();
                Char.BodyOutfitID = P.ReadUInt64();
                Char.Appearance = (SimsLib.ThreeD.AppearanceType)P.ReadByte();
                Char.CreatedThisSession = true;

                var characterModel = new Character();
                characterModel.Name = Char.Name;
                characterModel.Sex = Char.Sex;
                characterModel.Description = Char.Description;
                characterModel.LastCached = Char.Timestamp;
                characterModel.GUID = Char.GUID.ToString();
                characterModel.HeadOutfitID = (long?)Char.HeadOutfitID;
                characterModel.BodyOutfitID = (long?)Char.BodyOutfitID;
                characterModel.AccountID = account.AccountID;
                characterModel.AppearanceType = (int?)Char.Appearance;
                characterModel.City = GlobalSettings.Default.ServerID;

                var status = db.Characters.CreateCharacter(characterModel);
            }

            //Invalid token, should never occur...
            if (!ClientAuthenticated)
            {
                PacketStream SuccessPacket = new PacketStream(0x65, (int)(PacketHeaders.ENCRYPTED + 1));
                SuccessPacket.WriteByte((byte)TSODataModel.Entities.CharacterCreationStatus.GeneralError);
                Client.SendEncrypted(0x64, SuccessPacket.ToArray());
                Client.Disconnect();
            }
        }

        public static void HandleClientKeyReceive(ProcessedPacket P, NetworkClient C)
        {
            //TODO: Read packet and assign the key to the client.

            AResetEvent.Set();
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
