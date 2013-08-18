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
            //2 bytes is the payload size, the header of the packet is not included
            Register(0x01, 2, new OnPacketReceive(NetworkController._OnLoginNotify));
            Register(0x02, 2, new OnPacketReceive(NetworkController._OnLoginFailure));
            Register(0x05, 0, new OnPacketReceive(NetworkController._OnCharacterList));
            Register(0x06, 0, new OnPacketReceive(NetworkController._OnCityList));



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
        private static Dictionary<ushort, PacketHandler> m_Handlers = new Dictionary<ushort, PacketHandler>();
        public static void Register(ushort id, int size, OnPacketReceive handler)
        {
            //2 bytes for header
            if (size != 0)
            {
                size += 2;
            }
            m_Handlers.Add(id, new PacketHandler(id, size, handler));
        }

        public static void Handle(PacketStream stream, NetworkClient session)
        {
            ushort ID = (ushort)stream.ReadUShort();
            if (m_Handlers.ContainsKey(ID))
            {
                m_Handlers[ID].Handler(session, stream);
            }
        }

        public static PacketHandler Get(ushort id)
        {
            return m_Handlers[id];
        }

    }






    public delegate void OnPacketReceive(NetworkClient Session, PacketStream Packet);

    public class PacketHandler
    {
        private ushort m_ID;
        private int m_Length;
        private OnPacketReceive m_Handler;

        public PacketHandler(ushort id, int size, OnPacketReceive handler)
        {
            this.m_ID = id;
            this.m_Length = size;
            this.m_Handler = handler;
        }

        public ushort ID
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
