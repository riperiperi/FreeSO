using FSO.Server.Database.DA;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server
{
    /// <summary>
    /// This tool will install and update the SQL database.
    /// 
    /// It does this by reading the manifest.json file from the Scripts folder and compares it
    /// to whats inside the fso_db_changes table.
    /// 
    /// </summary>
    public class ToolInitDatabase : ITool
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DAFactory;

        public ToolInitDatabase(DatabaseInitOptions options, IDAFactory factory)
        {
            this.DAFactory = factory;
        }

        public void Run()
        {
            LOG.Info("Starting database init");

            using (var da = DAFactory.Get())
            {
                    
            }
        }
    }
}
