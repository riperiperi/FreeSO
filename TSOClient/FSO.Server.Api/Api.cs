using FSO.Server.Database.DA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace FSO.Server.Api
{
    public class Api
    {
        public static Api INSTANCE;

        public IDAFactory DAFactory;
        public bool Maintainance;

        public Api()
        {
            INSTANCE = this;
        }

        public void Init()
        {
            Maintainance = bool.Parse(WebConfigurationManager.AppSettings["maintainance"]);

            DAFactory = new MySqlDAFactory(new Database.DatabaseConfiguration()
            {
                ConnectionString = WebConfigurationManager.AppSettings["connectionString"]
            });
        }
    }
}