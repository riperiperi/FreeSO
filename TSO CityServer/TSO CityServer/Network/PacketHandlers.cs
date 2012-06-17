using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using TSO_CityServer.VM;

namespace TSO_CityServer.Network
{
    class PacketHandlers
    {
        //Can be set in order to stop this thread waiting for a specific packet.
        public static AutoResetEvent AResetEvent = new AutoResetEvent(false);

        public static void HandleCharacterCreate(PacketStream P, CityClient Client)
        {
            byte PacketLength = (byte)P.ReadByte();
            //Length of the unencrypted data, excluding the header (ID, length, unencrypted length).
            byte UnencryptedLength = (byte)P.ReadByte();

            //Accountname isn't encrypted in this packet.
            string AccountName = P.ReadString();

            PacketStream FetchKeyPack = new PacketStream(0x01, 00);
            FetchKeyPack.WriteString(AccountName);
            Client.Send(FetchKeyPack.ToArray());

            //TODO: Wait until the key has been received...
            AResetEvent.WaitOne();

            P.DecryptPacket(Client.EncKey, Client.CryptoService, UnencryptedLength);

            PacketStream OutPacket = new PacketStream(0x01, 0x00);
            OutPacket.WriteByte((byte)0x01);
            OutPacket.WriteByte((byte)AccountName.Length);
            OutPacket.Write(Encoding.ASCII.GetBytes(AccountName), 0, AccountName.Length);

            Logger.LogDebug("Received CharacterCreate!");

            Guid ID = new Guid();

            Sim Character = new Sim(ID.ToString());
            Character.Timestamp = P.ReadString();
            Character.Name = P.ReadString();
            Character.Sex = P.ReadString();

            //TODO: This should check if the character exists in the DB...
            Database.CreateCharacter(Character);
        }

        public static void HandleClientKeyReceive(PacketStream P, ref CityClient C)
        {
            //TODO: Read packet and assign the key to the client.

            AResetEvent.Set();
        }

        public static void HandleCreateSimulationObject(PacketStream P, ref CityClient C)
        {
            byte PacketLength = (byte)P.ReadByte();
            //Length of the unencrypted data, excluding the header (ID, length, unencrypted length).
            byte UnencryptedLength = (byte)P.ReadByte();

            BinaryFormatter BinFormatter = new BinaryFormatter();
            SimulationObject CreatedSimObject = (SimulationObject)BinFormatter.Deserialize(P);

            //TODO: Add the object to the client's lot's VM...
        }
    }
}
