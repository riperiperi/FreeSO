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
using System.Data.Sql;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace TSO_LoginServer.Network
{
    /// <summary>
    /// An object used to pass other objects asynchronously to a callback function (see Database.cs and MySQLDatabase.cs)
    /// </summary>
    class DatabaseAsyncObject
    {
        private LoginClient m_Client;
        private SqlCommand m_Command;
        private MySqlCommand m_MySqlCmd;
        private string m_AccountName, m_Password, m_CharacterName;
        private DateTime m_CharacterTimestamp;      //Timestamp for when a character was created or updated.
        private byte[] m_Hash;                      //Hash of a client's username and password, used for encryption.
        private string m_CharacterCity;             //The city of a character.

        private CityServerListener m_CServerListener;

        //IDs corresponding to unique characters in the DB.
        public int CharacterID1, CharacterID2, CharacterID3;
        //The number of characters an account has.
        public int NumCharacters = 0;

        #region SQL Server constructors

        /// <summary>
        /// Constructs a DBAsyncObject instance that can be used to communicate with a Microsoft SQL Server.
        /// </summary>
        /// <param name="CityName">The name of the city that a character resides in.</param>
        /// <param name="CharacterName">The name of a character.</param>
        /// <param name="Command">The SQL command.</param>
        public DatabaseAsyncObject(string CityName, string CharacterName, SqlCommand Command)
        {
            m_CharacterName = CharacterName;
            m_CharacterCity = CityName;
            m_Command = Command;
        }

        /// <summary>
        /// Constructs a DBAsyncObject instance that can be used to communicate with a Microsoft SQL Server.
        /// </summary>
        /// <param name="AccountName">The name of the client's account.</param>
        /// <param name="Client">The client.</param>
        /// <param name="Command">The SQL command.</param>
        public DatabaseAsyncObject(string AccountName, ref LoginClient Client, SqlCommand Command)
        {
            m_Client = Client;
            m_AccountName = AccountName;
            m_Command = Command;
        }

        /// <summary>
        /// Constructs a DBAsyncObject instance that can be used to communicate with a Microsoft SQL Server.
        /// </summary>
        /// <param name="AccountName">The name of the client's account.</param>
        /// <param name="Client">The client.</param>
        /// <param name="Timestamp">The timestamp of when a character was last cached by the client.</param>
        /// <param name="Command">The database command used to communicate with the DB.</param>
        public DatabaseAsyncObject(string AccountName, ref LoginClient Client, DateTime Timestamp, SqlCommand Command)
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
        public DatabaseAsyncObject(string AccountName, ref LoginClient Client, SqlCommand Command, byte[] Hash)
        {
            m_Client = Client;
            m_AccountName = AccountName;
            m_Command = Command;
            m_Hash = Hash;
        }

        /// <summary>
        /// Constructs a DBAsyncObject instance.
        /// </summary>
        /// <param name="Client">The client.</param>
        /// <param name="Command">The SQL command.</param>
        /// <param name="CSListener">A CityServerListener that holds information about connected CityServers.
        ///                          Must be passed by reference in order to ensure that any servers that might
        ///                          connect before a reply is sent to the client will be written into the reply.</param>
        public DatabaseAsyncObject(LoginClient Client, SqlCommand Command, ref CityServerListener CSListener)
        {
            m_Client = Client;
            m_Command = Command;
            m_CServerListener = CSListener;
        }

        #endregion

        #region MySQL constructors

        /// <summary>
        /// Constructs a DBAsyncObject instance that can be used to communicate with a MySQL Server.
        /// </summary>
        /// <param name="CityName">The name of the city that a character resides in.</param>
        /// <param name="CharacterName">The name of a character.</param>
        /// <param name="Command">The SQL command.</param>
        public DatabaseAsyncObject(string CityName, string CharacterName, MySqlCommand Command)
        {
            m_CharacterName = CharacterName;
            m_CharacterCity = CityName;
            m_MySqlCmd = Command;
        }

        /// <summary>
        /// Constructs a DBAsyncObject instance.
        /// </summary>
        /// <param name="AccountName">The name of the client's account.</param>
        /// <param name="Client">The client.</param>
        /// <param name="Command">The SQL command.</param>
        public DatabaseAsyncObject(string AccountName, ref LoginClient Client, MySqlCommand Command)
        {
            m_Client = Client;
            m_AccountName = AccountName;
            m_MySqlCmd = Command;
        }

        /// <summary>
        /// Constructs a DBAsyncObject instance that can be used to communicate with a MySQL SQL Server.
        /// </summary>
        /// <param name="AccountName">The name of the client's account.</param>
        /// <param name="Client">The client.</param>
        /// <param name="Timestamp">The timestamp of when a character was last cached by the client.</param>
        /// <param name="Command">The database command used to communicate with the DB.</param>
        public DatabaseAsyncObject(string AccountName, ref LoginClient Client, DateTime Timestamp, MySqlCommand Command)
        {
            m_Client = Client;
            m_AccountName = AccountName;
            m_CharacterTimestamp = Timestamp;
            m_MySqlCmd = Command;
        }

        /// <summary>
        /// Constructs a DBAsyncObject instance.
        /// </summary>
        /// <param name="AccountName">The name of the client's account.</param>
        /// <param name="Client">The client.</param>
        /// <param name="Command">The SQL command.</param>
        public DatabaseAsyncObject(string AccountName, ref LoginClient Client, MySqlCommand Command, byte[] Hash)
        {
            m_Client = Client;
            m_AccountName = AccountName;
            m_MySqlCmd = Command;
            m_Hash = Hash;
        }

        /// <summary>
        /// Constructs a DBAsyncObject instance.
        /// </summary>
        /// <param name="Client">The client.</param>
        /// <param name="Command">The SQL command.</param>
        /// <param name="CSListener">A CityServerListener that holds information about connected CityServers.
        ///                          Must be passed by reference in order to ensure that any servers that might
        ///                          connect before a reply is sent to the client will be written into the reply.</param>
        public DatabaseAsyncObject(LoginClient Client, MySqlCommand Command, ref CityServerListener CSListener)
        {
            m_Client = Client;
            m_MySqlCmd = Command;
            m_CServerListener = CSListener;
        }

        #endregion

        /// <summary>
        /// Client's accountname.
        /// </summary>
        public string AccountName
        {
            get { return m_AccountName; }
        }

        /// <summary>
        /// Client's password.
        /// </summary>
        public string Password
        {
            get { return m_Password; }
            set { m_Password = value; }
        }

        /// <summary>
        /// Name of a character in the DB.
        /// </summary>
        public string CharacterName
        {
            get { return m_CharacterName; }
        }

        public LoginClient Client
        {
            get { return m_Client; }
        }

        /// <summary>
        /// The command used to communicate with a Microsoft SQL server.
        /// Will be null if a SqlCommand wasn't passed to the constructor
        /// of this instance!
        /// </summary>
        public SqlCommand Cmd
        {
            get { return m_Command; }
        }

        /// <summary>
        /// The command used to communicate with a MySQL server.
        /// Will be null if a MySqlCommand wasn't passed to the constructor
        /// of this instance!
        /// </summary>
        public MySqlCommand MySQLCmd
        {
            get { return m_MySqlCmd; }
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

        /// <summary>
        /// A CityServerListener that contains all the CityServers
        /// currently connected to the LoginServer.
        /// </summary>
        public CityServerListener CSListener
        {
            get { return m_CServerListener; }
        }

        /// <summary>
        /// The name of a city that a character resides in.
        /// </summary>
        public string CharacterCity
        {
            get { return m_CharacterCity; }
        }
    }
}
