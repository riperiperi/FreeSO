using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Clients.Framework
{
    public abstract class AbstractHttpClient
    {
        public string BaseUrl { get; internal set; }

        public AbstractHttpClient(string baseUrl){
            this.BaseUrl = BaseUrl;
        }

        protected HttpClient CreateClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);
            return client;
        }
    }
}
