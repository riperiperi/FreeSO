using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO_LoginServer.Network
{
    public class CityInfo
    {
        public int ID;
        private string m_Name;
        private string m_Description;
        private ulong m_Thumbnail;
        private string m_UUID;
        private ulong m_Map;
        public bool Online = true;
        public CityInfoStatus Status;

        private string m_IP;
        private int m_Port;

        /// <summary>
        /// The name of this city.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// This city's description.
        /// </summary>
        public string Description
        {
            get { return m_Description; }
        }

        /// <summary>
        /// The ID of this city's thumbnail.
        /// </summary>
        public ulong Thumbnail
        {
            get { return m_Thumbnail; }
        }

        /// <summary>
        /// This city's server's IP.
        /// </summary>
        public string IP
        {
            get { return m_IP; }
        }

        /// <summary>
        /// This city's server's port.
        /// </summary>
        public int Port
        {
            get { return m_Port; }
        }

        /// <summary>
        /// A unique ID for this city.
        /// </summary>
        public string UUID
        {
            get { return m_UUID; }
        }

        public CityInfo(string Name, string Description, ulong Thumbnail, string UUID, ulong Map, string IP, int Port)
        {
            m_Name = Name;
            m_Description = Description;
            m_Thumbnail = Thumbnail;
            m_UUID = UUID;
            m_Map = Map;
            m_IP = IP;
            m_Port = Port;
        }

        public List<CityInfoMessageOfTheDay> Messages;
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
