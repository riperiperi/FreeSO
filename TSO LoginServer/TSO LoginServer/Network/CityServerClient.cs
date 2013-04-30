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

namespace TSO_LoginServer.Network
{
    public class CityServerClient
    {
        private static Dictionary<byte, int> m_PacketIDs = new Dictionary<byte, int>();

        private Socket m_Socket;
        private CityServerListener m_Listener;
        private byte[] m_RecvBuffer = new byte[11024];

        //Buffer for storing packets that were not fully read.
        private PacketStream m_TempPacket;

        //The number of bytes to be sent. See Send()
        private int m_NumBytesToSend = 0;

        //Information about this CityServer.
        //See CityServerPacketHandlers.HandleCityServerLogin().
        public CityInfo ServerInfo;

        public CityServerClient(Socket ClientSocket, CityServerListener Server)
        {
            m_Socket = ClientSocket;
            m_Listener = Server;

            m_Socket.BeginReceive(m_RecvBuffer, 0, m_RecvBuffer.Length, SocketFlags.None,
                new AsyncCallback(OnReceivedData), m_Socket);
        }

        public void Send(byte[] Data)
        {
            m_NumBytesToSend = Data.Length;
            m_Socket.BeginSend(Data, 0, Data.Length, SocketFlags.None, new AsyncCallback(OnSend), m_Socket);
        }

        protected virtual void OnSend(IAsyncResult AR)
        {
            Socket ClientSock = (Socket)AR.AsyncState;
            int NumBytesSent = ClientSock.EndSend(AR);

            Console.WriteLine("Sent: " + NumBytesSent.ToString() + "!");

            if (NumBytesSent < m_NumBytesToSend)
                Console.WriteLine("Didn't send everything!");
        }

        public void OnReceivedData(IAsyncResult AR)
        {
            //base.OnReceivedData(AR); //Not needed for this application!

            try
            {
                int NumBytesRead = m_Socket.EndReceive(AR);

                Logger.LogInfo("Received: " + NumBytesRead.ToString() + " bytes!\r\n\r\n");

                //The packet is given an ID of 0x00 because its ID is currently unknown.
                PacketStream TempPacket = new PacketStream(0x00, NumBytesRead, CreateABuffer(NumBytesRead, m_RecvBuffer));
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
                        m_Listener.OnReceivedData(new PacketStream(ID, PacketLength, TempPacket.ToArray()), this);
                        m_RecvBuffer = new byte[11024];
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
                    Console.WriteLine("Found matching Packet ID!");

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
        public static void RegisterPatchPacketID(byte ID, int Length)
        {
            m_PacketIDs.Add(ID, Length);
        }
    }
}
