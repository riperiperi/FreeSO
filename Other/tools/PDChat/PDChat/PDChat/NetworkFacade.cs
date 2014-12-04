using System;
using System.Collections.Generic;
using System.Text;
using GonzoNet;
using ProtocolAbstractionLibraryD;
using PDChat.Sims;

namespace PDChat
{
    public class NetworkFacade
    {
        public static NetworkClient Client;
        public static List<Sim> Avatars = new List<Sim>();
        public static List<CityInfo> Cities = new List<CityInfo>();

        static NetworkFacade()
        {
            GonzoNet.PacketHandlers.Register((byte)PacketType.INVALID_VERSION, false, 2, new OnPacketReceive(NetworkController._OnLoginFailure));
            GonzoNet.PacketHandlers.Register((byte)PacketType.LOGIN_FAILURE, true, 0, new OnPacketReceive(NetworkController._OnLoginFailure));
            GonzoNet.PacketHandlers.Register((byte)PacketType.LOGIN_NOTIFY, false, 0, new OnPacketReceive(NetworkController._OnLoginNotify));
            GonzoNet.PacketHandlers.Register((byte)PacketType.LOGIN_SUCCESS, true, 0, new OnPacketReceive(NetworkController._OnLoginSuccess));
            GonzoNet.PacketHandlers.Register((byte)PacketType.CHARACTER_LIST, true, 0, new OnPacketReceive(NetworkController._OnCharacterList));
        }
    }
}
