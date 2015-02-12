using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO;

namespace KISS.net
{
    /// <summary>
    /// A RequestState is used to store request information between
    /// asynchronous calls to callbacks.
    /// </summary>
    public class RequestState
    {
        public WebRequest Request;
        public WebResponse Response;
        public string ContentType = "";
        public int ContentLength;

        public Stream ResponseStream;
        
        public DateTime TransferStart;
        public int BytesRead = 0;
        public double PctComplete = 0;
        public double KBPerSec = 0;

        public byte[] RequestBuffer;

        public RequestState()
        {
            RequestBuffer = new byte[11024];
        }
    }
}
