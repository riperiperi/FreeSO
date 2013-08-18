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
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using TSO_LoginServer;
using TSO_LoginServer.Network.Encryption;

namespace TSO_LoginServer.Network
{
    public class LoginClient //: iNetClient
    {
        //private static Dictionary<byte, int> m_PacketIDs = new Dictionary<byte, int>();
        
        private Socket m_Socket;
        private LoginListener m_Listener;
        private byte[] m_RecvBuffer = new byte[11024];
        public DESCryptoServiceProvider CryptoService = new DESCryptoServiceProvider();

        private Sim m_CurrentlyActiveSim;

        //Buffer for storing packets that were not fully read.
        private PacketStream m_TempPacket;

        //The number of bytes to be sent. See Send()
        private int m_NumBytesToSend = 0;

        public byte[] Hash;
        public byte[] EncKey;

        /// <summary>
        /// The client's username.
        /// </summary>
        public string Username;

        /// <summary>
        /// Account ID for this session
        /// </summary>
        public int AccountID;

        /// <summary>
        /// The character the client's player is currently playing as.
        /// </summary>
        public Sim CurrentlyActiveSim
        {
            get { return m_CurrentlyActiveSim; }
            set { m_CurrentlyActiveSim = value; }
        }

        public LoginClient(Socket ClientSocket, LoginListener Server)
            //: base(ClientSocket, (Listener)Server)
        {
            m_Socket = ClientSocket;
            m_Listener = Server;

            m_Socket.BeginReceive(m_RecvBuffer, 0, m_RecvBuffer.Length, SocketFlags.None,
                new AsyncCallback(OnReceivedData), m_Socket);
        }

        public void Send(PacketStream stream)
        {
            this.Send(stream.ToArray());
        }

        public void Send(byte[] Data)
        {
            m_NumBytesToSend = Data.Length;
            //m_Socket.BeginSend(Data, 0, Data.Length, SocketFlags.None, new AsyncCallback(OnSend), m_Socket);
            m_Socket.Send(Data, 0, Data.Length, SocketFlags.None);
        }

        /// <summary>
        /// Sends an encrypted packet to the client.
        /// Automatically appends the length of the packet after the ID, as 
        /// the encrypted data can be smaller or longer than that of the
        /// unencrypted data.
        /// </summary>
        /// <param name="PacketID">The ID of the packet (will remain unencrypted).</param>
        /// <param name="Data">The data that will be encrypted.</param>
        public void SendEncrypted(byte PacketID, byte[] Data)
        {
            m_NumBytesToSend = Data.Length;
            byte[] EncryptedData = FinalizePacket(PacketID, Data);

            m_Socket.BeginSend(EncryptedData, 0, EncryptedData.Length, SocketFlags.None, 
                new AsyncCallback(OnSend), m_Socket);
        }

        /// <summary>
        /// Writes a packet's header and encrypts the contents of the packet (not the header).
        /// </summary>
        /// <param name="PacketID">The ID of the packet.</param>
        /// <param name="PacketData">The packet's contents.</param>
        /// <returns>The finalized packet!</returns>
        private byte[] FinalizePacket(ushort PacketID, byte[] PacketData)
        {
            MemoryStream FinalizedPacket = new MemoryStream();
            BinaryWriter PacketWriter = new BinaryWriter(FinalizedPacket);

            MemoryStream TempStream = new MemoryStream();
            CryptoStream EncryptedStream = new CryptoStream(TempStream,
                CryptoService.CreateEncryptor(EncKey, Encoding.ASCII.GetBytes("@1B2c3D4e5F6g7H8")),
                CryptoStreamMode.Write);
            EncryptedStream.Write(PacketData, 0, PacketData.Length);
            EncryptedStream.FlushFinalBlock();

            PacketWriter.Write(PacketID);
            //The length of the encrypted data can be longer or smaller than the original length,
            //so write the length of the encrypted data.
            PacketWriter.Write((ushort)(6 + TempStream.Length));
            //Also write the length of the unencrypted data.
            PacketWriter.Write((ushort)PacketData.Length);
            PacketWriter.Flush();

            PacketWriter.Write(TempStream.ToArray());
            PacketWriter.Flush();

            byte[] ReturnPacket = FinalizedPacket.ToArray();

            PacketWriter.Close();

            return ReturnPacket;
        }

        protected virtual void OnSend(IAsyncResult AR)
        {
            try
            {
                Socket ClientSock = (Socket)AR.AsyncState;
                int NumBytesSent = ClientSock.EndSend(AR);

                Logger.LogInfo("Sent: " + NumBytesSent.ToString() + "!\r\n");

                if (NumBytesSent < m_NumBytesToSend)
                    Logger.LogInfo("Didn't send everything!");
            }
            catch (SocketException E)
            {
                Logger.LogInfo("Exception when sending: " + E.ToString());
            }
        }
        
        public void OnReceivedData(IAsyncResult AR)
        {
            //base.OnReceivedData(AR); //Not needed for this application!
            try
            {
                if (Thread.CurrentThread.IsThreadPoolThread)
                    Logger.LogDebug("Current thread is threadpool thread.");
                else if (Thread.CurrentThread.IsBackground)
                    Logger.LogDebug("Current thread is background thread.");

                Socket Sock = (Socket)AR.AsyncState;
                int NumBytesRead = Sock.EndReceive(AR);

                if (NumBytesRead > 0)
                {

                    byte[] TmpBuf = new byte[NumBytesRead];
                    Buffer.BlockCopy(m_RecvBuffer, 0, TmpBuf, 0, NumBytesRead);

                    //The packet is given an ID of 0x00 because its ID is currently unknown.
                    PacketStream TempPacket = new PacketStream(0x00, NumBytesRead, TmpBuf);
                    
                    /** Get the packet type **/
                    ushort ID = TempPacket.ReadUShort();

                    //byte ID = TempPacket.PeekByte(0);



                    var handler = FindPacketHandler(ID);

                    if (handler != null)
                    {
                        var PacketLength = handler.Length;
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
                                PacketLength = TempPacket.PeekUShort(2);

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
                                    handler = FindPacketHandler(ID);

                                    //This SHOULD be an existing ID, but let's sanity-check it...
                                    if (handler != null)
                                    {
                                        m_TempPacket = new PacketStream(ID, handler.Length, TempPacket.ToArray());

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
                else
                {
                    //Client disconnected!
                    //TODO: Figure out if this client was successfully authenticated before transferring. 
                    m_Listener.TransferClient(this);
                }

                m_Socket.BeginReceive(m_RecvBuffer, 0, m_RecvBuffer.Length, SocketFlags.None,
                    new AsyncCallback(OnReceivedData), m_Socket);
            }
            catch (SocketException)
            {
                Disconnect();
            }
        }

        /// <summary>
        /// Disconnects this LoginClient instance and stops
        /// all sending and receiving of data.
        /// </summary>
        public void Disconnect()
        {
            m_Socket.Shutdown(SocketShutdown.Both);
            m_Socket.Disconnect(false);

            m_Listener.RemoveClient(this);
        }

        private PacketHandler FindPacketHandler(ushort ID)
        {
            return PacketHandlers.Get(ID);
        }
    }
}
