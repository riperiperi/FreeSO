/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO CityServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.IO;
using TSO_CityServer;
using TSO_CityServer.VM;
using GonzoNet;

namespace TSO_CityServer.Network
{
    /// <summary>
    /// An object used to pass other objects asynchronously to a callback function.
    /// </summary>
    class DBAsyncObject
    {
        private NetworkClient m_Client;
        private SqlCommand m_Command;
        private string m_AccountName, m_Password;
        private DateTime m_CharacterTimestamp;
        private byte[] m_Hash;

        //IDs corresponding to unique characters in the DB.
        public int CharacterID1, CharacterID2, CharacterID3;
        //The number of characters an account has.
        public int NumCharacters = 0;

        /// <summary>
        /// Constructs a DBAsyncObject instance.
        /// </summary>
        /// <param name="AccountName">The name of the client's account.</param>
        /// <param name="Client">The client.</param>
        /// <param name="Command">The SQL command.</param>
        public DBAsyncObject(string AccountName, ref NetworkClient Client, SqlCommand Command)
        {
            m_Client = Client;
            m_AccountName = AccountName;
            m_Command = Command;
        }

        public DBAsyncObject(string AccountName, ref NetworkClient Client, DateTime Timestamp, SqlCommand Command)
        {
            m_Client = Client;
            m_AccountName = AccountName;
            m_CharacterTimestamp = Timestamp;
            m_Command = Command;
        }

        /// <summary>
        /// Constructs a DBAsyncObject instance.
        /// </summary>
        /// <param name="AccountName">The name of the client's account.</param>
        /// <param name="Client">The client.</param>
        /// <param name="Command">The SQL command.</param>
        public DBAsyncObject(string AccountName, ref NetworkClient Client, SqlCommand Command, byte[] Hash)
        {
            m_Client = Client;
            m_AccountName = AccountName;
            m_Command = Command;
            m_Hash = Hash;
        }

        public string AccountName
        {
            get { return m_AccountName; }
        }

        public string Password
        {
            get { return m_Password; }
            set { m_Password = value; }
        }

        public NetworkClient Client
        {
            get { return m_Client; }
        }

        public SqlCommand Cmd
        {
            get { return m_Command; }
        }

        /// <summary>
        /// The timestamp received by a client.
        /// If this timestamp is older than the ones in the DB,
        /// it means information should be sent about characters,
        /// because the client's cache was out of date.
        /// </summary>
        public DateTime CharacterTimestamp
        {
            get { return m_CharacterTimestamp; }
        }

        /// <summary>
        /// The hash received by a client.
        /// If the client's password matches this
        /// when hashed, it means the right
        /// password was supplied.
        /// </summary>
        public byte[] Hash
        {
            get { return m_Hash; }
        }
    }

    class Database
    {
        private static SqlConnection m_Connection;

        public static void Connect()
        {
            try
            {
                m_Connection = new SqlConnection("Data Source=AFR0-PC\\SQLEXPRESS;" + 
                    "Initial Catalog=TSO;User Id=Afr0;Password=Prins123;Asynchronous Processing=true");
                m_Connection.Open();
            }
            catch (Exception)
            {
                throw new NoDBConnection("Couldn't connect to database server! Reverting to flat file DB.");
            }
        }

        /// <summary>
        /// Creates a character in the DB.
        /// </summary>
        /// <param name="TimeStamp">When the character was last cached, should be equal to DateTime.Now</param>
        /// <param name="Name">The name of the character.</param>
        /// <param name="Sex">The sex of the character, should be MALE or FEMALE.</param>
        public static void CreateCharacter(Sim Character)
        {
            SqlCommand Command = new SqlCommand("INSERT INTO Character(LastCached, Name, Sex) VALUES('" +
                Character.Timestamp + "', '" + Character.Name + "', '" + Character.Sex + "')");
            Command.BeginExecuteNonQuery(new AsyncCallback(EndCreateCharacter), Command);
        }

        private static void EndCreateCharacter(IAsyncResult AR)
        {
            SqlCommand Cmd = AR.AsyncState as SqlCommand;

            Cmd.EndExecuteNonQuery(AR);
        }
    }
}
