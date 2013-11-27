/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using GonzoNet;

namespace TSO_LoginServer.Network
{
    public class CityServerClient : NetworkClient
    {
        //The number of bytes to be sent. See Send()
        private int m_NumBytesToSend = 0;

        //Information about this CityServer.
        //See CityServerPacketHandlers.HandleCityServerLogin().
        public CityInfo ServerInfo;

        private Timer m_PulseTimer;
        //The time when the last pulse was received from this CityServer.
        public DateTime LastPulseReceived = DateTime.Now;

        public CityServerClient(Socket ClientSocket, CityServerListener Server) : 
            base(ClientSocket, Server)
        {
            m_PulseTimer = new Timer(1500);
            m_PulseTimer.AutoReset = true;
            m_PulseTimer.Elapsed += new ElapsedEventHandler(m_PulseTimer_Elapsed);
            m_PulseTimer.Start();
        }

        private void m_PulseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            double Secs = (((TimeSpan)(DateTime.Now - LastPulseReceived)).TotalMilliseconds / 1000);

            //More than 2 secs since last pulse was received, server is offline!
            if (Secs > 2.0)
            {
                Logger.LogInfo("Time since last pulse: " + Secs + " secs\r\n");
                Logger.LogInfo("More than two seconds since last pulse - disconnected CityServer.\r\n");

                this.Disconnect();
                NetworkFacade.CServerListener.CityServers.Remove(this);
                m_PulseTimer.Stop();
            }
        }
<<<<<<< HEAD

        public void Send(byte[] Data)
        {
            m_NumBytesToSend = Data.Length;
            m_Socket.BeginSend(Data, 0, Data.Length, SocketFlags.None, new AsyncCallback(OnSend), m_Socket);
        }

        protected virtual void OnSend(IAsyncResult AR)
        {
            Socket ClientSock = (Socket)AR.AsyncState;
            int NumBytesSent = ClientSock.EndSend(AR);

            Logger.LogInfo("Sent: " + NumBytesSent.ToString() + "!\r\n");

            if (NumBytesSent < m_NumBytesToSend)
                Logger.LogInfo("Didn't send everything!\r\n");
        }

        public void OnReceivedData(IAsyncResult AR)
        {
            //base.OnReceivedData(AR); //Not needed for this application!
            try
            {
                Socket Sock = (Socket)AR.AsyncState;
                int NumBytesRead = Sock.EndReceive(AR);

                if (NumBytesRead > 0)
                {
                    byte[] TmpBuf = new byte[NumBytesRead];
                    Buffer.BlockCopy(m_RecvBuffer, 0, TmpBuf, 0, NumBytesRead);

                    //The packet is given an ID of 0x00 because its ID is currently unknown.
                    PacketStream TempPacket = new PacketStream(0x00, NumBytesRead, TmpBuf);
                    byte ID = TempPacket.PeekByte(0);
                    int PacketLength = 0;

                    bool FoundMatchingID = false;

                    FoundMatchingID = FindMatchingPacketID(ID);

                    if (FoundMatchingID)
                    {
                        PacketLength = m_PacketIDs[ID];

                        Logger.LogInfo("Found matching PacketID!\r\n\r\n");

                        if (NumBytesRead == PacketLength)
                        {
                            Logger.LogInfo("Got packet - exact length!\r\n\r\n");
                            m_RecvBuffer = new byte[11024];
                            m_Listener.OnReceivedData(new PacketStream(ID, PacketLength, TempPacket.ToArray()), this);
                        }
                        else if (NumBytesRead < PacketLength)
                        {
                            m_TempPacket = new PacketStream(ID, PacketLength);
                            byte[] TmpBuffer = new byte[NumBytesRead];

                            //Store the number of bytes that were read in the temporary buffer.
                            Logger.LogInfo("Got data, but not a full packet - stored " +
                                NumBytesRead.ToString() + "bytes!\r\n\r\n");
                            Buffer.BlockCopy(m_RecvBuffer, 0, TmpBuffer, 0, NumBytesRead);
                            m_TempPacket.WriteBytes(TmpBuffer);

                            //And reset the buffers!
                            m_RecvBuffer = new byte[11024];
                            TmpBuffer = null;
                        }
                        else if (PacketLength == 0)
                        {
                            Logger.LogInfo("Received variable length packet!\r\n");

                            if (NumBytesRead > 2)
                            {
                                PacketLength = TempPacket.PeekUShort(1);
                                Logger.LogInfo("PacketLength: " + PacketLength + "\r\n");

                                if (NumBytesRead == PacketLength)
                                {
                                    Logger.LogInfo("Received exact number of bytes for packet!\r\n");

                                    m_RecvBuffer = new byte[11024];
                                    m_TempPacket = null;
                                    m_Listener.OnReceivedData(new PacketStream(ID, PacketLength,
                                        TempPacket.ToArray()), this);
                                }
                                else if (NumBytesRead < PacketLength)
                                {
                                    Logger.LogInfo("Didn't receive entire packet - stored: " + PacketLength + " bytes!\r\n");

                                    TempPacket.SetLength(PacketLength);
                                    m_TempPacket = TempPacket;
                                    m_RecvBuffer = new byte[11024];
                                }
                                else if (NumBytesRead > PacketLength)
                                {
                                    Logger.LogInfo("Received more bytes than needed for packet. Excess: " +
                                        (NumBytesRead - PacketLength) + "\r\n");

                                    byte[] TmpBuffer = new byte[NumBytesRead - PacketLength];
                                    Buffer.BlockCopy(TempPacket.ToArray(), 0, TmpBuffer, 0, TmpBuffer.Length);
                                    m_TempPacket = new PacketStream(TmpBuffer[0], NumBytesRead - PacketLength,
                                        TmpBuffer);

                                    byte[] PacketBuffer = new byte[PacketLength];
                                    Buffer.BlockCopy(TempPacket.ToArray(), 0, PacketBuffer, 0, PacketBuffer.Length);

                                    m_RecvBuffer = new byte[11024];
                                    m_Listener.OnReceivedData(new PacketStream(ID, PacketLength, PacketBuffer),
                                        this);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (m_TempPacket != null)
                        {
                            if (m_TempPacket.Length < m_TempPacket.BufferLength)
                            {
                                //Received the exact number of bytes needed to complete the stored packet.
                                if ((m_TempPacket.BufferLength + NumBytesRead) == m_TempPacket.Length)
                                {
                                    byte[] TmpBuffer = new byte[NumBytesRead];
                                    Buffer.BlockCopy(m_RecvBuffer, 0, TmpBuffer, 0, NumBytesRead);

                                    m_Listener.OnReceivedData(new PacketStream(ID, TmpBuffer.Length, m_RecvBuffer), this);

                                    m_RecvBuffer = new byte[11024];
                                    TmpBuffer = null;
                                }
                                //Received more than the number of bytes needed to complete the packet!
                                else if ((m_TempPacket.BufferLength + NumBytesRead) > m_TempPacket.Length)
                                {
                                    int Target = (int)((m_TempPacket.BufferLength + NumBytesRead) - m_TempPacket.Length);
                                    byte[] TmpBuffer = new byte[Target];

                                    Buffer.BlockCopy(m_RecvBuffer, 0, TmpBuffer, 0, Target);
                                    m_TempPacket.WriteBytes(TmpBuffer);

                                    //Now we have a full packet, so call the received event!
                                    m_Listener.OnReceivedData(new PacketStream(m_TempPacket.PacketID,
                                        (int)m_TempPacket.Length, m_TempPacket.ToArray()), this);

                                    //Copy the remaining bytes in the receiving buffer.
                                    TmpBuffer = new byte[NumBytesRead - Target];
                                    Buffer.BlockCopy(m_RecvBuffer, Target, TmpBuffer, 0, (NumBytesRead - Target));

                                    //Give the temporary packet an ID of 0x00 since we don't know its ID yet.
                                    TempPacket = new PacketStream(0x00, NumBytesRead - Target, TmpBuffer);
                                    ID = TempPacket.PeekByte(0);

                                    //This SHOULD be an existing ID, but let's sanity-check it...
                                    if (FindMatchingPacketID(ID))
                                    {
                                        m_TempPacket = new PacketStream(ID, m_PacketIDs[ID], TempPacket.ToArray());

                                        //Congratulations, you just received another packet!
                                        if (m_TempPacket.Length == m_TempPacket.BufferLength)
                                        {
                                            m_Listener.OnReceivedData(new PacketStream(m_TempPacket.PacketID,
                                                (int)m_TempPacket.Length, m_TempPacket.ToArray()), this);

                                            //No more data to store on this read, so reset everything...
                                            m_TempPacket = null;
                                            TmpBuffer = null;
                                            m_RecvBuffer = new byte[11024];
                                        }
                                    }
                                    else
                                    {
                                        //Houston, we have a problem (this should never occur)!
                                    }
                                }
                            }
                        }
                    }
                }

                m_Socket.BeginReceive(m_RecvBuffer, 0, m_RecvBuffer.Length, SocketFlags.None,
                    new AsyncCallback(OnReceivedData), m_Socket);
            }
            catch (SocketException)
            {
                Disconnect();
            }
        }

        private byte[] CreateABuffer(int Size, byte[] Data)
        {
            byte[] Buf = new byte[Size];
            Buffer.BlockCopy(Data, 0, Buf, 0, Size);

            return Buf;
        }

        /// <summary>
        /// Disconnects this PatchClient instance and stops
        /// all sending and receiving of data.
        /// </summary>
        public void Disconnect()
        {
            m_Socket.Shutdown(SocketShutdown.Both);
            m_Socket.Disconnect(false);
        }

        private bool FindMatchingPacketID(byte ID)
        {
            foreach (KeyValuePair<byte, int> Pair in m_PacketIDs)
            {
                if (ID == Pair.Key)
                {
                    Logger.LogInfo("Found matching Packet ID!");

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Register a Packet ID with a corresponding Packet Length from a specific protocol.
        /// </summary>
        /// <param name="ID">The ID to register.</param>
        /// <param name="Length">The length of the packet to register.</param>
        public static void RegisterCityPacketID(byte ID, int Length)
        {
            m_PacketIDs.Add(ID, Length);
        }
=======
>>>>>>> origin/GonzoNet
    }
}
