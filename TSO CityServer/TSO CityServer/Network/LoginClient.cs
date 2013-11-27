using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using GonzoNet;

namespace TSO_CityServer.Network
{
    public delegate void NetworkErrorDelegate(SocketException Exception);
    public delegate void ReceivedPacketDelegate(PacketStream Packet);

    public class LoginClient
    {
        private static Dictionary<byte, int> m_PacketIDs = new Dictionary<byte, int>();

        private Socket m_Sock;
        private string m_IP;
        private int m_Port;

        private bool m_Connected = false;

        //Buffer for storing packets that were not fully read.
        private PacketStream m_TempPacket;

        //The number of bytes to be sent. See Send()
        private int m_NumBytesToSend = 0;
        private byte[] m_RecvBuf;

        public event NetworkErrorDelegate OnNetworkError;
        public event ReceivedPacketDelegate OnReceivedData;

        public LoginClient(string IP, int Port)
        {
            m_Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_IP = IP;
            m_Port = Port;

            m_RecvBuf = new byte[11024];
        }

        public void Send(byte[] Data)
        {
            m_NumBytesToSend = Data.Length;
            m_Sock.BeginSend(Data, 0, Data.Length, SocketFlags.None, new AsyncCallback(OnSend), m_Sock);
        }

        protected virtual void OnSend(IAsyncResult AR)
        {
            Socket ClientSock = (Socket)AR.AsyncState;
            int NumBytesSent = ClientSock.EndSend(AR);

            Logger.LogDebug("Sent: " + NumBytesSent.ToString() + "!\r\n");

            if (NumBytesSent < m_NumBytesToSend)
                Logger.LogDebug("Didn't send everything!\r\n");
        }

        private void BeginReceive(/*object State*/)
        {
            //if (m_Connected)
            //{
            m_Sock.BeginReceive(m_RecvBuf, 0, m_RecvBuf.Length, SocketFlags.None,
                new AsyncCallback(ReceivedDataCallback), m_Sock);
            //}
        }

        /// <summary>
        /// Connects to the LoginServer using the IP and port that were specified in the constructor.
        /// </summary>
        public void Connect()
        {
            m_Sock.BeginConnect(IPAddress.Parse(m_IP), m_Port, new AsyncCallback(ConnectCallback), m_Sock);
        }

        private void ConnectCallback(IAsyncResult AR)
        {
            try
            {
                Socket Sock = (Socket)AR.AsyncState;
                Sock.EndConnect(AR);

                m_Connected = true;

                //Send information about this CityServer to the LoginServer...
                PacketStream Packet = new PacketStream(0x00, 0);
                Packet.WriteByte(0x00);

                MemoryStream PacketBody = new MemoryStream();
                BinaryWriter PacketWriter = new BinaryWriter(PacketBody);

                PacketWriter.Write((string)GlobalSettings.Default.CityName);
                PacketWriter.Write((string)GlobalSettings.Default.CityDescription);
                PacketWriter.Write((string)GlobalSettings.Default.ClientIP);
                PacketWriter.Write((int)GlobalSettings.Default.Port);
                PacketWriter.Write((byte)1); //CityInfoStatus.OK
                PacketWriter.Write((ulong)GlobalSettings.Default.CityThumbnail);
                PacketWriter.Write((string)Guid.NewGuid().ToString());
                PacketWriter.Write((ulong)GlobalSettings.Default.Map);
                PacketWriter.Flush();

                Packet.WriteUInt16((ushort)(PacketBody.ToArray().Length + PacketHeaders.UNENCRYPTED));

                Packet.Write(PacketBody.ToArray(), 0, (int)PacketWriter.BaseStream.Length);
                Packet.Flush();

                PacketWriter.Close();

                Send(Packet.ToArray());

                BeginReceive();
            }
            catch (SocketException E)
            {
                OnNetworkError(E);
            }
        }

        public void ReceivedDataCallback(IAsyncResult AR)
        {
            //base.OnReceivedData(AR); //Not needed for this application!
            try
            {
                Socket Sock = (Socket)AR.AsyncState;
                int NumBytesRead = Sock.EndReceive(AR);

                if (NumBytesRead > 0)
                {
                    byte[] TmpBuf = new byte[NumBytesRead];
                    Buffer.BlockCopy(m_RecvBuf, 0, TmpBuf, 0, NumBytesRead);

                    //The packet is given an ID of 0x00 because its ID is currently unknown.
                    PacketStream TempPacket = new PacketStream(0x00, NumBytesRead, TmpBuf);

                    /** Get the packet type **/
                    byte ID = TempPacket.PeekByte(0);
                    var handler = FindPacketHandler(ID);

                    if (handler != null)
                    {
                        var PacketLength = handler.Length;
                        Logger.LogInfo("Found matching PacketID!\r\n\r\n");

                        if (NumBytesRead == PacketLength)
                        {
                            Logger.LogInfo("Got packet - exact length!\r\n\r\n");
                            m_RecvBuf = new byte[11024];
                            OnReceivedData(new PacketStream(ID, PacketLength, TempPacket.ToArray()));
                        }
                        else if (NumBytesRead < PacketLength)
                        {
                            m_TempPacket = new PacketStream(ID, PacketLength);
                            byte[] TmpBuffer = new byte[NumBytesRead];

                            //Store the number of bytes that were read in the temporary buffer.
                            Logger.LogInfo("Got data, but not a full packet - stored " +
                                NumBytesRead.ToString() + "bytes!\r\n\r\n");
                            Buffer.BlockCopy(m_RecvBuf, 0, TmpBuffer, 0, NumBytesRead);
                            m_TempPacket.WriteBytes(TmpBuffer);

                            //And reset the buffers!
                            m_RecvBuf = new byte[11024];
                            TmpBuffer = null;
                        }
                        else if (PacketLength == 0)
                        {
                            Logger.LogInfo("Received variable length packet!\r\n");

                            if (NumBytesRead > (int)PacketHeaders.UNENCRYPTED) //Header is 3 bytes.
                            {
                                PacketLength = TempPacket.PeekUShort(1);

                                if (NumBytesRead == PacketLength)
                                {
                                    Logger.LogInfo("Received exact number of bytes for packet!\r\n");

                                    m_RecvBuf = new byte[11024];
                                    m_TempPacket = null;
                                    OnReceivedData(new PacketStream(ID, PacketLength,
                                        TempPacket.ToArray()));
                                }
                                else if (NumBytesRead < PacketLength)
                                {
                                    Logger.LogInfo("Didn't receive entire packet - stored: " + PacketLength + " bytes!\r\n");

                                    TempPacket.SetLength(PacketLength);
                                    m_TempPacket = TempPacket;
                                    m_RecvBuf = new byte[11024];
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

                                    m_RecvBuf = new byte[11024];
                                    OnReceivedData(new PacketStream(ID, PacketLength, PacketBuffer));
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
                                    Buffer.BlockCopy(m_RecvBuf, 0, TmpBuffer, 0, NumBytesRead);

                                    m_RecvBuf = new byte[11024];
                                    TmpBuffer = null;
                                }
                                //Received more than the number of bytes needed to complete the packet!
                                else if ((m_TempPacket.BufferLength + NumBytesRead) > m_TempPacket.Length)
                                {
                                    int Target = (int)((m_TempPacket.BufferLength + NumBytesRead) - m_TempPacket.Length);
                                    byte[] TmpBuffer = new byte[Target];

                                    Buffer.BlockCopy(m_RecvBuf, 0, TmpBuffer, 0, Target);
                                    m_TempPacket.WriteBytes(TmpBuffer);

                                    //Now we have a full packet, so call the received event!
                                    OnReceivedData(new PacketStream(m_TempPacket.PacketID,
                                        (int)m_TempPacket.Length, m_TempPacket.ToArray()));

                                    //Copy the remaining bytes in the receiving buffer.
                                    TmpBuffer = new byte[NumBytesRead - Target];
                                    Buffer.BlockCopy(m_RecvBuf, Target, TmpBuffer, 0, (NumBytesRead - Target));

                                    //Give the temporary packet an ID of 0x00 since we don't know its ID yet.
                                    TempPacket = new PacketStream(0x00, NumBytesRead - Target, TmpBuffer);
                                    ID = TempPacket.PeekByte(0);
                                    handler = FindPacketHandler(ID);

                                    //This SHOULD be an existing ID, but let's sanity-check it...
                                    if (handler != null)
                                    {
                                        m_TempPacket = new PacketStream(ID, handler.Length, TempPacket.ToArray());

                                        //Congratulations, you just received another packet!
                                        if (m_TempPacket.Length == m_TempPacket.BufferLength)
                                        {
                                            OnReceivedData(new PacketStream(m_TempPacket.PacketID,
                                                (int)m_TempPacket.Length, m_TempPacket.ToArray()));

                                            //No more data to store on this read, so reset everything...
                                            m_TempPacket = null;
                                            TmpBuffer = null;
                                            m_RecvBuf = new byte[11024];
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
                else
                {
                    Disconnect();
                }

                m_Sock.BeginReceive(m_RecvBuf, 0, m_RecvBuf.Length, SocketFlags.None,
                    new AsyncCallback(ReceivedDataCallback), m_Sock);
            }
            catch (SocketException SE)
            {
                Logger.LogWarning("SocketException: " + SE.ToString());
                Disconnect();
            }
        }

        /// <summary>
        /// Disconnects this NetworkClient instance and stops
        /// all sending and receiving of data.
        /// </summary>
        public void Disconnect()
        {
            m_Sock.Shutdown(SocketShutdown.Both);
            m_Sock.Disconnect(true);
        }

        private PacketHandler FindPacketHandler(byte ID)
        {
            return PacketHandlers.Get(ID);
        }
    }
}
