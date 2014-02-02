using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using TSOClient.VM;
using TSOClient.Lot;

namespace TSOClient.Network
{
    class LotPacketHandlers
    {
        public static void OnSimulationState(NetworkClient Client, PacketStream Packet, LotScreen Lot)
        {
            List<SimulationObject> SimObjects = new List<SimulationObject>();

            byte Opcode = (byte)Packet.ReadByte();

            byte NumTicks = (byte)Packet.ReadByte();
            int NumObjects = Packet.ReadInt32();
            BinaryFormatter BinFormatter = new BinaryFormatter();

            for (int i = 0; i < NumObjects; i++)
            {
                SimulationObject SimObject = (SimulationObject)BinFormatter.Deserialize(Packet);
                SimObjects.Add(SimObject);
            }

            Lot.UpdateSimulationState(NumTicks, SimObjects);
        }
    }
}
