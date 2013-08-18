using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using TSODataModel.Entities;

namespace TSODataModel
{
    /// <summary>
    /// This is the data access layer. Whenever code wants to get/put/search something
    /// in the database, it should use this class like this:
    /// 
    /// using(var model = DataAccess.Get()){
    ///     model.(whatever i want)
    /// }
    /// </summary>
    public class DataAccess : IDisposable
    {
        public static string ConnectionString;

        public static DataAccess Get()
        {
            var db = new DB(new MySqlConnection(ConnectionString));
            return new DataAccess(db);
        }


        private DB _Model;
        private AccountAccess _Accounts;
        private CharacterAccess _Character;

        public DataAccess(DB db){
            this._Model = db;
        }


        public AccountAccess Accounts
        {
            get
            {
                if (_Accounts == null)
                {
                    _Accounts = new AccountAccess(this);
                }
                return _Accounts;
            }
        }

        public CharacterAccess Characters
        {
            get
            {
                if (_Character == null)
                {
                    _Character = new CharacterAccess(this);
                }
                return _Character;
            }
        }

        public DB Context {
            get
            {
                return _Model;
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            _Model.SubmitChanges();
            _Model.Dispose();
        }
        #endregion
    }
}
