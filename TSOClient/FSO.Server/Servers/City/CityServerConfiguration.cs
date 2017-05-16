using FSO.Server.Framework.Aries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City
{
    public class CityServerConfiguration : AbstractAriesServerConfig
    {
        public int ID;

        public CityServerMaintenanceConfiguration Maintenance;
    }

    public class CityServerMaintenanceConfiguration
    {
        public string Cron;
        public int Timeout = 3600;
        public int Visits_Retention_Period = 7;
    }
}
