using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using TSO_CityServer.VM;
using GonzoNet;

namespace TSO_CityServer.Network
{
    class ClientPacketHandlers
    {
        public static AutoResetEvent AResetEvent = new AutoResetEvent(false);

        public static void HandleCharacterCreate(ProcessedPacket P, NetworkClient Client)
        {
            //Accountname isn't encrypted in this packet.
            string AccountName = P.ReadString();

            PacketStream FetchKeyPack = new PacketStream(0x01, 00);
            FetchKeyPack.WriteString(AccountName);
            Client.Send(FetchKeyPack.ToArray());

            //TODO: Wait until the key has been received...
            AResetEvent.WaitOne();

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
