using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using LogThis;
using SimsLib.IFF;
using GonzoNet;

namespace TSO_CityServer.VM
{
    public delegate void OnNewSimulationState(PacketStream Packet);

    class VirtualMachine
    {
        private List<VirtualThread> m_Threads = new List<VirtualThread>();
        private int m_TickCounter = 0;
        public event OnNewSimulationState NewSimulationStateEvent;

        /// <summary>
        /// Adds an object that will be run by this virtual machine.
        /// </summary>
        /// <param name="Obj">The object to run.</param>
        /// <param name="ObjectContainer">The object's container.</param>
        public void AddObject(OBJD Obj, Iff ObjectContainer, string GUID)
        {
            VirtualThread VThread = new VirtualThread(new SimulationObject(Obj, ObjectContainer, GUID));
            m_Threads.Add(VThread);
        }

        /// <summary>
        /// Ticks the VM one step.
        /// Should be called once every loop.
        /// </summary>
        public void Tick()
        {
            foreach (VirtualThread Thread in m_Threads)
            {
                Thread.Tick();
            }

            m_TickCounter++;

            if (m_TickCounter == 4)
            {
                //TODO: Change ID...
                PacketStream SimulationStatePacket = new PacketStream(0x10, 0x00);
                SimulationStatePacket.WriteByte(0x10);                  //TODO: Change ID
                SimulationStatePacket.WriteByte((byte)m_TickCounter);   //Number of ticks since last update.
                SimulationStatePacket.WriteInt32(m_Threads.Count);      //Number of objects in this VM.
                BinaryFormatter BinFormatter = new BinaryFormatter();

                foreach (VirtualThread VThread in m_Threads)
                    BinFormatter.Serialize(SimulationStatePacket, VThread);

                //TODO: Compress packet...

                NewSimulationStateEvent(SimulationStatePacket);

                m_TickCounter = 0;
            }
        }
    }
}
