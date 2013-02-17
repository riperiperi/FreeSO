using System;
using System.Collections.Generic;
using System.Text;
using TSOClient.VM;
using TSOClient.Network;
using TSOClient.Code.UI.Framework;

namespace TSOClient.Lot
{
    public class LotScreen : GameScreen
    {
        private VirtualMachine m_VM;

        private List<UIElement> m_UIElements = new List<UIElement>();
        //private List<NetworkedUIElement> m_NetUIElements = new List<NetworkedUIElement>();
        private NetworkClient m_Client;

        public LotScreen(ScreenManager ScreenMgr, NetworkClient Client)
            : base(ScreenMgr)
        {
            m_VM = new VirtualMachine();
            m_Client = Client;

            m_Client.OnReceivedData += new ReceivedPacketDelegate(m_Client_OnReceivedData);
        }

        private void m_Client_OnReceivedData(PacketStream Packet)
        {
            switch (Packet.PacketID)
            {
                case 0x10: //TODO: Change ID for this packet!
                    LotPacketHandlers.OnSimulationState(m_Client, Packet, this);
                    break;
            }
        }

        /// <summary>
        /// Updates this lot's simulation state. Called whenever the client
        /// receives a SimulationState packet from the cityserver.
        /// </summary>
        /// <param name="SimObjects">The SimulationObject instances to update.</param>
        public void UpdateSimulationState(byte NumTicks, List<SimulationObject> SimObjects)
        {
            m_VM.UpdateObjects(SimObjects);

            //TODO: Synchronize this to run at a specific TPS.
            for (int i = 0; i < NumTicks; i++)
                m_VM.Tick();
        }

        public override void Update(Microsoft.Xna.Framework.GameTime GTime)
        {
            base.Update(GTime);
        }

        public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch SBatch)
        {
            base.Draw(SBatch);
        }
    }
}
