using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace PDPatcher
{
    public class DownloadAsyncObject
    {
        private List<List<DownloadFile>> m_AllFilesToDownload = new List<List<DownloadFile>>();
        private HttpWebRequest m_Request;
        //Keeping these static ensures the values stay the same across all threads.
        private static int m_CurrentManifest = 0, m_CurrentFile = 0;

        /// <summary>
        /// A list of manifests with a list of files to download.
        /// </summary>
        public List<List<DownloadFile>> FilesToDownload
        {
            get { return m_AllFilesToDownload; }
        }

        public HttpWebRequest Request
        {
            get { return m_Request; }
            set { m_Request = value; }
        }

        /// <summary>
        /// The current manifest being processed.
        /// Should always be less than FilesToDownload.Count
        /// while still downloading files.
        public int CurrentManifest
        {
            get { return m_CurrentManifest; }
            set { m_CurrentManifest = value; }
        }

        /// <summary>
        /// The current file being downloaded.
        /// Should always be less than FilesToDownload[CurrentManifest].Count.
        /// </summary>
        public int CurrentFile
        {
            get { return m_CurrentFile; }
            set { m_CurrentFile = value; }
        }

        public DownloadAsyncObject(List<List<DownloadFile>> FilesToDownload, HttpWebRequest Request)
        {
            m_AllFilesToDownload = FilesToDownload;
            m_Request = Request;
        }
    }
}
