using System;
using System.Collections.Generic;
using System.Text;

namespace Manifestation
{
    public class ManifestFile
    {
        private string m_VirtualPath, m_RealPath, m_Checksum = "";

        public string VirtualPath
        {
            get { return m_VirtualPath; }
            set { m_VirtualPath = value; }
        }

        public string RealPath
        {
            get { return m_RealPath; }
        }

        /// <summary>
        /// This file's checksum. Will contain an empty string before
        /// a manifest has been saved (which is when checksums are calculated).
        /// </summary>
        public string Checksum
        {
            get { return m_Checksum; }
        }

        public ManifestFile(string VirtualPath, string RealPath)
        {
            m_VirtualPath = VirtualPath;
            m_RealPath = RealPath;
        }

        public ManifestFile(string VirtualPath, string RealPath, string Checksum)
        {
            m_VirtualPath = VirtualPath;
            m_RealPath = RealPath;
            m_Checksum = Checksum;
        }
    }
}
