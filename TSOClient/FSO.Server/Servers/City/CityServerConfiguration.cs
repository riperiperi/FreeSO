using FSO.Common;
using FSO.Server.Framework.Aries;
using Newtonsoft.Json;

namespace FSO.Server.Servers.City
{
    public class CityServerConfiguration : AbstractAriesServerConfig
    {
        [JsonProperty("id")]
        public int ID;
        [JsonProperty("timeout_no_auth")]
        public bool Timeout_No_Auth = true;

        [JsonProperty("neighborhoods")]
        public CityServerNhoodConfiguration Neighborhoods = new CityServerNhoodConfiguration();
        [JsonProperty("maintenance")]
        public CityServerMaintenanceConfiguration Maintenance;

        // Copied from base config
        public bool AllOpenable;
        public ArchiveConfiguration Archive;
    }


    public class CityServerNhoodConfiguration
    {
        /** Minimum number of nominations required to run for mayor. */
        [JsonProperty("min_nominations")]
        public int Min_Nominations = 3;

        /** 
         * if a neighbourhood with no elections is within this number from the top in activity (and not reserved),
         * we should start an election cycle anyways 
         */
        [JsonProperty("mayor_elegibility_limit")]
        public int Mayor_Elegibility_Limit = 2;

        /**
          * if a neighbourhood that had elections is no longer within the falloff range in popularity,
          * elections are disabled.
          */
        [JsonProperty("mayor_elegibility_falloff")]
        public int Mayor_Elegibility_Falloff = 4;

        /**
         * The number of days you must wait after moving before participating in an election.
         */
        [JsonProperty("election_move_penalty")]
        public int Election_Move_Penalty = 30;

        /**
         * The number of days you must wait after moving before rating a mayor.
         */
        [JsonProperty("rating_move_penalty")]
        public int Rating_Move_Penalty = 7;

        /**
         * The number of days you must wait after moving before posting on a bulletin board.
         */
        [JsonProperty("bulletin_move_penalty")]
        public int Bulletin_Move_Penalty = 7;

        /**
         * The number of days you must wait between bulletin posts.
         */
        [JsonProperty("bulletin_post_frequency")]
        public int Bulletin_Post_Frequency = 3;

        /**
         * The number of days the mayor must wait between bulletin posts.
         */
        [JsonProperty("bulletin_mayor_frequency")]
        public int Bulletin_Mayor_Frequency = 1;

        /**
         * If true, starts elections on the last monday in a month, rather than 7 days before the end of the month.
         */
        [JsonProperty("election_week_align")]
        public bool Election_Week_Align = true;

        /**
         * If true, sims in areas without an election are offered a free vote.
         */
        [JsonProperty("election_free_vote")]
        public bool Election_Free_Vote = true;

        /**
         * The value of a vote/nomination made by a resident.
         */
        [JsonProperty("vote_normal_value")]
        public int Vote_Normal_Value = 2;

        /**
         * The value of a vote/nomination made by a non-resident.
         */
        [JsonProperty("vote_free_value")]
        public int Vote_Free_Value = 1;
    }

    public class CityServerMaintenanceConfiguration
    {
        [JsonProperty("cron")]
        public string Cron;
        [JsonProperty("timeout")]
        public int Timeout = 3600;
        [JsonProperty("visits_retention_period")]
        public int Visits_Retention_Period = 7;
    }
}
