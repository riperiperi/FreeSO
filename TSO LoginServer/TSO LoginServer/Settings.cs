using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Configuration;

namespace TSO_LoginServer
{
    public class Settings
    {
        public static IPEndPoint BINDING
        {
            get
            {
                var binding = ConfigurationManager.AppSettings["BINDING"];
                if (binding == null)
                {
                    return new IPEndPoint(IPAddress.Any, 2106);
                }
                string[] components = binding.Split(new char[]{':'}, 2);
                if (components.Length == 0)
                {
                    return new IPEndPoint(IPAddress.Any, 2106);
                }
                else if (components.Length == 1)
                {
                    return new IPEndPoint(IPAddress.Parse(components[0]), 2106);
                }
                else
                {
                    return new IPEndPoint(IPAddress.Parse(components[0]), int.Parse(components[1]));
                }
            }
        }
    }
}
