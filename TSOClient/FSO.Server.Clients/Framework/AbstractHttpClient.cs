using RestSharp;
using System.Net;

namespace FSO.Server.Clients.Framework
{
    public abstract class AbstractHttpClient
    {
        public string BaseUrl { get; internal set; }
        private readonly CookieContainer Cookies = new CookieContainer();
        private RestClient _client;

        public AbstractHttpClient(string baseUrl)
        {
            BaseUrl = baseUrl;
        }

        public virtual void SetBaseUrl(string url)
        {
            BaseUrl = url;
            _client?.Dispose();
            _client = null;
        }

        protected RestClient Client()
        {
            if (_client == null)
            {
                var options = new RestClientOptions(BaseUrl)
                {
                    CookieContainer = Cookies
                };

                _client = new RestClient(options);
            }

            return _client;
        }
    }
}
