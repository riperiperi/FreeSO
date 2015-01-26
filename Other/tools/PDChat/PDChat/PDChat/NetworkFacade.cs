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
        public static List<Sim> AvatarsInSession = new List<Sim>();
        public static List<CityInfo> Cities = new List<CityInfo>();

        static NetworkFacade()
        {
            GonzoNet.PacketHandlers.Register((byte)PacketType.INVALID_VERSION, false, 2, new OnPacketReceive(NetworkController._OnLoginFailure));
            GonzoNet.PacketHandlers.Register((byte)PacketType.LOGIN_FAILURE, true, 0, new OnPacketReceive(NetworkController._OnLoginFailure));
            GonzoNet.PacketHandlers.Register((byte)PacketType.LOGIN_NOTIFY, false, 0, new OnPacketReceive(NetworkController._OnLoginNotify));
            GonzoNet.PacketHandlers.Register((byte)PacketType.LOGIN_SUCCESS, true, 0, new OnPacketReceive(NetworkController._OnLoginSuccess));
            GonzoNet.PacketHandlers.Register((byte)PacketType.CHARACTER_LIST, true, 0, new OnPacketReceive(NetworkController._OnCharacterList));
            GonzoNet.PacketHandlers.Register((byte)PacketType.CITY_LIST, true, 0, new OnPacketReceive(NetworkController._OnCityList));
            GonzoNet.PacketHandlers.Register((byte)PacketType.REQUEST_CITY_TOKEN, true, 0, new OnPacketReceive(NetworkController._OnCityTokenRequest));
            GonzoNet.PacketHandlers.Register((byte)PacketType.LOGIN_NOTIFY_CITY, false, 0, new OnPacketReceive(NetworkController._OnLoginNotifyCity));
            GonzoNet.PacketHandlers.Register((byte)PacketType.LOGIN_SUCCESS_CITY, true, 0, new OnPacketReceive(NetworkController._OnLoginSuccessCity));
            GonzoNet.PacketHandlers.Register((byte)PacketType.CITY_TOKEN, true, 0, new OnPacketReceive(NetworkController._OnCityTokenResponse));
            GonzoNet.PacketHandlers.Register((byte)PacketType.PLAYER_JOINED_SESSION, true, 0, new OnPacketReceive(NetworkController._OnPlayerJoinedSession));
            GonzoNet.PacketHandlers.Register((byte)PacketType.PLAYER_RECV_LETTER, true, 0, new OnPacketReceive(NetworkController._OnReceivedMessage));
        }
    }
}
