using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOServiceClient.Model;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace TSOServiceClient
{
    /// <summary>
    /// Communicates with the first service layer used for authentication
    /// and basic data services
    /// </summary>
    public class TSOServiceClient
    {
        private AuthResponse LastAuth;

        public TSOServiceResponse<AuthResponse> Authenticate(AuthRequest request)
        {
            var result = 
                HandleRequest<AuthResponse>("auth.service", request);

            if (result.Status == TSOServiceStatus.OK)
            {
                LastAuth = result.Body;
            }
            return result;
        }

        public TSOServiceResponse<CityList> GetCityList()
        {
            return HandleRequest<CityList>("cityList.service", null);
        }

        public TSOServiceResponse<AvatarList> GetAvatarList()
        {
            return HandleRequest<AvatarList>("avatars.service?auth=" + LastAuth.SessionID, null);
        }

        private TSOServiceResponse<T> HandleRequest<T>(string url, object body)
        {
            url = "http://127.0.0.1:8887/" + url;

            var rq = (HttpWebRequest)HttpWebRequest.Create(url);
            rq.Method = body != null ? "POST" : "GET";
            rq.Accept = "text/json";

            if (body != null)
            {
                var jsonBody = JsonConvert.SerializeObject(body);

                rq.ContentType = "text/json";
                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonBody);
                rq.ContentLength = jsonBytes.Length;

                var inputStream = rq.GetRequestStream();
                inputStream.Write(jsonBytes, 0, jsonBytes.Length);
                inputStream.Close();
            }
            
            HttpWebResponse response = (HttpWebResponse)rq.GetResponse();
            string result;
            using (StreamReader rdr = new StreamReader(response.GetResponseStream()))
            {
                result = rdr.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<TSOServiceResponse<T>>(result);
        }


    }
}
