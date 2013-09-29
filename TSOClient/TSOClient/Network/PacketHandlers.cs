using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Network
{
    public class PacketHandlers
    {
        public static void Init()
        {
            Register(0x01, 1, new OnPacketReceive(NetworkFacade.Controller._OnLoginNotify));
            Register(0x02, 2, new OnPacketReceive(NetworkFacade.Controller._OnLoginFailure));
            Register(0x05, 0, new OnPacketReceive(NetworkFacade.Controller._OnCharacterList));
            Register(0x06, 0, new OnPacketReceive(NetworkFacade.Controller._OnCityList));

            ////InitLoginNotify - 2 bytes
            //NetworkClient.RegisterLoginPacketID(0x01, 2);
            ////LoginFailResponse - 2 bytes
            //NetworkClient.RegisterLoginPacketID(0x02, 2);
            ///*LoginSuccessResponse - 33 bytes
            //NetworkClient.RegisterLoginPacketID(0x04, 33);*/
            ////CharacterInfoResponse - Variable size
            //NetworkClient.RegisterLoginPacketID(0x05, 0);
            ////CityInfoResponse
            //NetworkClient.RegisterLoginPacketID(0x06, 0);
            ////CharacterCreate
            //NetworkClient.RegisterLoginPacketID(0x07, 0);

            /*
            LOGIN_NOTIFY = ,
            LOGIN_FAILURE = 0x2,
            CHARACTER_LIST = 0x5,
            CITY_LIST = 0x6*/
        }

        /**
         * Framework
         */
        private static Dictionary<byte, PacketHandler> m_Handlers = new Dictionary<byte, PacketHandler>();
        public static void Register(byte id, int size, OnPacketReceive handler)
        {
            //2 bytes for header
            //Why is this here? This is fucking things up!
            /*if (size != 0)
            {
                size += 2;
            }*/
            m_Handlers.Add(id, new PacketHandler(id, size, handler));
        }

        public static void Handle(PacketStream stream)
        {
            byte ID = (byte)stream.ReadByte();
            if (m_Handlers.ContainsKey(ID))
            {
                m_Handlers[ID].Handler(stream);
            }
        }

        public static PacketHandler Get(byte id)
        {
            return m_Handlers[id];
        }
    }

    public delegate void OnPacketReceive(PacketStream Packet);

    public class PacketHandler
    {
        private byte m_ID;
        private int m_Length;
        private OnPacketReceive m_Handler;

        public PacketHandler(byte id, int size, OnPacketReceive handler)
        {
            this.m_ID = id;
            this.m_Length = size;
            this.m_Handler = handler;
        }

        public byte ID
        {
            get { return m_ID; }
        }

        public int Length
        {
            get { return m_Length; }
        }

        public OnPacketReceive Handler
        {
            get
            {
                return m_Handler;
            }
        }
    }
}
