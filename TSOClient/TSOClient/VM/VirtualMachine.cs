using System;
using System.Collections.Generic;
using System.Text;
using LogThis;
using SimsLib.IFF;
using TSOClient.Network;

namespace TSOClient.VM
{
    class VirtualMachine
    {
        private List<VirtualThread> m_Threads = new List<VirtualThread>();

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

        public void UpdateObjects(List<SimulationObject> SimObjects)
        {
            for (int i = 0; i < SimObjects.Count; i++)
            {
                for(int j = 0; j < m_Threads.Count; j++)
                {
                    if (SimObjects[i].GUID == m_Threads[j].CurrentObject.GUID)
                        m_Threads[j].CurrentObject = SimObjects[i];
                }
            }
        }

        /// <summary>
        /// Ticks the VM one step.
        /// </summary>
        public void Tick()
        {
            foreach (VirtualThread Thread in m_Threads)
            {
                Thread.Tick();
            }
        }
    }
}
