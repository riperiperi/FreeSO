using System;
using System.Collections.Generic;
using System.Text;

namespace PDPatcher
{
    public class DownloadFile
    {
        private string m_VirtualPath = "", m_Checksum = "", m_ManifestVersion = "";

        public string VirtualPath
        {
            get { return m_VirtualPath; }
            set { m_VirtualPath = value; }
        }

        /// <summary>
        /// This file's checksum. Will contain an empty string before
        /// a manifest has been saved (which is when checksums are calculated).
        /// </summary>
        public string Checksum
        {
            get { return m_Checksum; }
        }

        /// <summary>
        /// The version of the manifest that this file belongs to.
        /// </summary>
        public string ManifestVersion
        {
            get { return m_ManifestVersion; }
        }

        public DownloadFile(string VirtualPath, string Checksum, string ManifestVersion)
        {
            m_VirtualPath = VirtualPath;
            m_Checksum = Checksum;
            m_ManifestVersion = ManifestVersion;
        }
    }
}
