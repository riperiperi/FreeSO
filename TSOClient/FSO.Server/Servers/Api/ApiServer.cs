using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;


namespace FSO.Server.Servers.Api
{
    public class ApiServer : AbstractServer
    {
        private ApiServerConfiguration Config;
        private HttpListener Listener;

        public ApiServer(ApiServerConfiguration config)
        {
            this.Config = config;
        }

        public override void Start()
        {
            /*var certificate = new X509Certificate2("C:\\OpenSSL\\bin\\newcert.p12", "test");


            Listener = new HttpListener();
            foreach (var host in hosts)
            {
                Listener.Prefixes.Add(host);
            }
            Listener.Start();*/
        }
    }
}
