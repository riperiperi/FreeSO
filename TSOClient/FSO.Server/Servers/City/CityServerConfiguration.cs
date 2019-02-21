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

        public CityServerNhoodConfiguration Neighborhoods = new CityServerNhoodConfiguration();
        public CityServerMaintenanceConfiguration Maintenance;
    }


    public class CityServerNhoodConfiguration
    {
        /** Minimum number of nominations required to run for mayor. */
        public int Min_Nominations = 3;

        /** 
         * if a neighbourhood with no elections is within this number from the top in activity (and not reserved),
         * we should start an election cycle anyways 
         */
        public int Mayor_Elegibility_Limit = 2;

        /**
          * if a neighbourhood that had elections is no longer within the falloff range in popularity,
          * elections are disabled.
          */
        public int Mayor_Elegilility_Falloff = 4;

        /**
         * The number of days you must wait after moving before participating in an election.
         */
        public int Election_Move_Penalty = 30;

        /**
         * The number of days you must wait after moving before rating a mayor.
         */
        public int Rating_Move_Penalty = 7;

        /**
         * The number of days you must wait after moving before posting on a bulletin board.
         */
        public int Bulletin_Move_Penalty = 7;

        /**
         * The number of days you must wait between bulletin posts.
         */
        public int Bulletin_Post_Frequency = 3;

        /**
         * The number of days the mayor must wait between bulletin posts.
         */
        public int Bulletin_Mayor_Frequency = 1;

        /**
         * If true, starts elections on the last monday in a month, rather than 7 days before the end of the month.
         */
        public bool Election_Week_Align = true;
    }

    public class CityServerMaintenanceConfiguration
    {
        public string Cron;
        public int Timeout = 3600;
        public int Visits_Retention_Period = 7;
    }
}
