using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Diagnostics;
using GonzoNet;

namespace ProtocolAbstractionLibraryD
{
    /// <summary>
    /// CityInfo holds information about, and can be used to represent a city server.
    /// </summary>
    public class CityInfo
    {
        public NetworkClient Client;

        public string Name;
        public string Description;
        public ulong Thumbnail;
        public string UUID;
        public ulong Map;
        public bool Online = true;
        public CityInfoStatus Status;

        public string IP;
        public int Port;

        /// <summary>
        /// Initializes a new instance of CityInfo.
        /// </summary>
        /// <param name="IsServer">Is this a city server?</param>
        public CityInfo(bool IsServer)
        {
            if (IsServer)
            {
                LastPulseReceived = DateTime.Now;

                m_PulseTimer = new System.Timers.Timer(1500);
                m_PulseTimer.AutoReset = true;
                m_PulseTimer.Elapsed += new ElapsedEventHandler(m_PulseTimer_Elapsed);
                m_PulseTimer.Start();
            }
        }

        public List<CityInfoMessageOfTheDay> Messages = new List<CityInfoMessageOfTheDay>();

        public DateTime LastPulseReceived;
        private System.Timers.Timer m_PulseTimer;

        private void m_PulseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            double Secs = (((TimeSpan)(DateTime.Now - LastPulseReceived)).TotalMilliseconds / 1000);

            //More than 30 secs since last pulse was received, server is offline!
            if (Secs > 30)
            {
                Debug.WriteLine("Time since last pulse: " + Secs + " secs\r\n");
                Debug.WriteLine("More than thirty seconds since last pulse - disconnected CityServer.\r\n");

                Client.Disconnect();

                m_PulseTimer.Stop();
            }
        }
    }

    public class CityInfoMessageOfTheDay 
    {
        public string From;
        public string Subject;
        public string Body;
    }

    public enum CityInfoStatus
    {
        Ok = 1,
        Busy = 2,
        Full = 3,
        Reserved = 4
    }
}
