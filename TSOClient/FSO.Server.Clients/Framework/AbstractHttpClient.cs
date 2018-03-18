using RestSharp;
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
        private CookieContainer Cookies = new CookieContainer();

        public AbstractHttpClient(string baseUrl){
            this.BaseUrl = baseUrl;
        }

        public virtual void SetBaseUrl(string url)
        {
            BaseUrl = url;
        }

        protected RestClient Client()
        {
            var client = new RestClient(BaseUrl);
            client.CookieContainer = Cookies;
            return client;
        }
    }
}
