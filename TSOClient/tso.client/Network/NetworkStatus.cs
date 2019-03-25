using FSO.Files.RC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Network
{
    public class NetworkStatus
    {
        public int CityReconnectAttempt;
        public int LotReconnectAttempt;
        public int RemeshesInProgress
        {
            get
            {
                return DGRP3DMesh.GetWorkCount();
            }
        }

        public bool Any
        {
            get
            {
                return CityReconnectAttempt > 0 || LotReconnectAttempt > 0 || RemeshesInProgress > 0;
            }
        }

        public bool Severe
        {
            get
            {
                return CityReconnectAttempt > 0 || LotReconnectAttempt > 0;
            }
        }
    }
}
