using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDChat
{
    public class PlayerAccount
    {
        //The hash of the username and password. See UIPacketSenders.SendLoginRequest()
        public static byte[] Hash = new byte[1];
        public static string Username = "";
        public static string CityToken = "";
    }
}
