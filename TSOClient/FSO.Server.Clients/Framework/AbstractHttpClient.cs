using RestSharp;
using System.Net;

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
