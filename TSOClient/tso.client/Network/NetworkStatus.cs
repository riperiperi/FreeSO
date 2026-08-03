using FSO.Content;
using FSO.Files.RC;

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

        public float? RemeshUpdateProgress
        {
            get
            {
                return RCDBPFContent.DownloadPercentage;
            }
        }

        public bool Any
        {
            get
            {
                return CityReconnectAttempt > 0 || LotReconnectAttempt > 0 || RemeshesInProgress > 0 || RemeshUpdateProgress != null;
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
