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
        //Keeping thes static ensures the values stay the same across all threads.
        private static int m_CurrentManifest = 0, m_CurrentFile = 0;

        public List<List<DownloadFile>> FilesToDownload
        {
            get { return m_AllFilesToDownload; }
        }

        public HttpWebRequest Request
        {
            get { return m_Request; }
            set { m_Request = value; }
        }

        public int CurrentManifest
        {
            get { return m_CurrentManifest; }
            set { m_CurrentManifest = value; }
        }

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
