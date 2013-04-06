using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace TSOServiceClient.Model
{
    public enum TSOServiceStatus
    {
        OK,
        Error
    }

    public class TSOServiceResponse <T>
    {
        public TSOServiceStatus Status;

        [JsonProperty("status")]
        public string StatusString
        {
            set
            {
                if (value == "ok")
                {
                    Status = TSOServiceStatus.OK;
                }
                else
                {
                    Status = TSOServiceStatus.Error;
                }
            }
        }

        [JsonProperty("body")]
        public T Body;
    }
}
