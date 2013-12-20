using System;
using System.Collections.Generic;
using System.Text;
using ProtocolAbstractionLibraryD.VM;
using TSO_CityServer.Network;
using GonzoNet;

namespace TSO_CityServer.Lot
{
    public class LotSimulation
    {
        private VirtualMachine m_VM = new VirtualMachine();
        private List<NetworkClient> m_Clients = new List<NetworkClient>();

        private NetworkClient m_Owner;

        public LotSimulation(NetworkClient Owner)
        {
            m_Owner = Owner;
            m_VM.NewSimulationStateEvent += new OnNewSimulationState(m_VM_NewSimulationStateEvent);
        }

        /// <summary>
        /// The VM for this lot has ticked 5 ticks and compiled a 
        /// SimulationState packet.
        /// </summary>
        /// <param name="Packet">The SimulationState packet.</param>
        private void m_VM_NewSimulationStateEvent(GonzoNet.PacketStream Packet)
        {
            m_Owner.Send(Packet.ToArray());

            foreach (NetworkClient Client in m_Clients)
                Client.Send(Packet.ToArray());
        }
    }
}
