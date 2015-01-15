using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using CityDataModel.Entities;

namespace CityDataModel
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

		// Flag: Has Dispose already been called? 
		private bool m_Disposed = false;

        public static DataAccess Get()
        {
            var db = new DB(new MySqlConnection(ConnectionString));
            return new DataAccess(db);
        }


        private DB _Model;
        private CharacterAccess _Character;

        public DataAccess(DB db){
            this._Model = db;
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
			Dispose(true);
			GC.SuppressFinalize(this);
        }

		protected virtual void Dispose(bool Disposing)
		{
			if (m_Disposed)
				return;

			if(Disposing)
			{
				_Model.SubmitChanges();
				_Model.Dispose();
			}

			// Free any unmanaged objects here. 
			//
			m_Disposed = true;
		}
        #endregion
    }
}
