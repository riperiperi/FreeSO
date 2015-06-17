/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the KISS.Net.

The Initial Developer of the Original Code is
Afr0. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Security;
using System.IO;
using System.Threading;
using System.Security.Cryptography.X509Certificates;

namespace KISS.net
{
    public delegate void FetchedManifestDelegate(ManifestFile Manifest);
    public delegate void FetchedFileDelegate(MemoryStream FileStream);
    public delegate void DownloadTickDelegate(RequestState State);

    /// <summary>
    /// A requester requests and fetches files from a webserver, and calls
    /// events when the requests are complete.
    /// </summary>
    public class Requester
    {
        //This is implementation specific. Personally, I'm ignoring security to avoid headaches.
        public static bool ACCEPT_ALL_CERTIFICATES = true;

        public event FetchedManifestDelegate OnFetchedManifest;
        private string m_ManifestAddress = "";

        /// <summary>
        /// Contains information about a download in progress,
        /// such as: percent complete and KB/sec.
        /// </summary>
        public event DownloadTickDelegate OnTick;

        /// <summary>
        /// Called when a file was fetched!
        /// </summary>
        public event FetchedFileDelegate OnFetchedFile;

        private bool m_HasFetchedManifest = false;

        public Requester(string ManifestAddress)
        {
            m_ManifestAddress = ManifestAddress;
        }

        public void Initialize()
        {
            try
            {
                WebRequest Request = WebRequest.Create(m_ManifestAddress);
                RequestState ReqState = new RequestState();

                ReqState.Request = Request;
                ReqState.TransferStart = DateTime.Now;
                Request.BeginGetResponse(new AsyncCallback(GotInitialResponse), ReqState);
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(AcceptAllCertifications);
            }
            catch (Exception E)
            {
                Logger.Log("Exception in Requester.Initialize:\n" + E.ToString(), LogLevel.error);
            }
        }

        /// <summary>
        /// Starts fetching a file, and notifies the OnFetchedFile event
        /// when done.
        /// </summary>
        public void FetchFile(string URL)
        {
            WebRequest Request = WebRequest.Create(URL);
            Request.Method = "GET";
            RequestState ReqState = new RequestState();

            ReqState.Request = Request;
            ReqState.TransferStart = DateTime.Now;
            Request.BeginGetResponse(new AsyncCallback(GotInitialResponse), ReqState);
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(AcceptAllCertifications);
        }

        private void GotInitialResponse(IAsyncResult AResult)
        {
            RequestState ReqState = (RequestState)AResult.AsyncState;
            ReqState.Response = ReqState.Request.EndGetResponse(AResult);
            ReqState.ContentType = ReqState.Response.ContentType;
            ReqState.ContentLength = (int)ReqState.Response.ContentLength;

            Stream ResponseStream = ReqState.Response.GetResponseStream();
            ReqState.ResponseStream = ResponseStream;
            ReqState.RequestBuffer = new byte[ReqState.ContentLength];
            ResponseStream.BeginRead(ReqState.RequestBuffer, 0, (int)ReqState.Response.ContentLength, 
                new AsyncCallback(ReadCallback), ReqState);
        }

        private void ReadCallback(IAsyncResult AResult)
        {
            RequestState ReqState = ((RequestState)(AResult.AsyncState));

            Stream ResponseStream = ReqState.ResponseStream;

            // Get results of read operation
            int BytesRead = ResponseStream.EndRead(AResult);

            // Got some data, need to read more
            if (BytesRead > 0)
            {
                // Report some progress, including total # bytes read, % complete, and transfer rate
                ReqState.BytesRead += BytesRead;
                ReqState.PctComplete = ((double)ReqState.BytesRead / (double)ReqState.ContentLength) * 100.0f;

                // Note: bytesRead/totalMS is in bytes/ms. Convert to kb/sec.
                TimeSpan totalTime = DateTime.Now - ReqState.TransferStart;
                ReqState.KBPerSec = (ReqState.BytesRead * 1000.0f) / (totalTime.TotalMilliseconds * 1024.0f);

                OnTick(ReqState);

                // Kick off another read
                IAsyncResult ar = ResponseStream.BeginRead(ReqState.RequestBuffer, ReqState.BytesRead, 
                    (ReqState.RequestBuffer.Length - ReqState.BytesRead), new AsyncCallback(ReadCallback), ReqState);
                return;
            }

            // EndRead returned 0, so no more data to be read
            else
            {
                ResponseStream.Close();
                ReqState.Response.Close();
                ReqState.ResponseStream.Close();
                OnFinishedFile(new MemoryStream(ReqState.RequestBuffer));
            }
        }

        /// <summary>
        /// Finished downloading a file!
        /// </summary>
        /// <param name="FileStr">The stream of the file that was downloaded.</param>
        private void OnFinishedFile(MemoryStream FileStr)
        {
            if (!m_HasFetchedManifest)
            {
                m_HasFetchedManifest = true;
                OnFetchedManifest(new ManifestFile(FileStr));
            }
            else
            {
                OnFetchedFile(FileStr);
            }
        }

        private bool AcceptAllCertifications(object sender, X509Certificate certification, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return ACCEPT_ALL_CERTIFICATES;
        }
    }
}