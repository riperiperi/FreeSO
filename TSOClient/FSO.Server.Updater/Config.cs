using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Watchdog
{
    public class Config : IniConfig
    {
        private static Config defaultInstance;

        public static Config Default
        {
            get
            {
                if (defaultInstance == null)
                    defaultInstance = new Config("watchdog.ini");
                return defaultInstance;
            }
        }

        public Config(string path) : base(path) { }

        private Dictionary<string, string> _DefaultValues = new Dictionary<string, string>()
        {
            { "ManifestDownload", "True" },

            { "UseTeamCity", "False" },
            { "TeamCityUrl", "http://servo.freeso.org" },
            { "TeamCityProject", "FreeSO_TsoClient" },
            { "Branch", "feature/server-rebuild" },

            { "NormalUpdateUrl", "https://dl.dropboxusercontent.com/u/12239448/FreeSO/devserver.zip" },
        };
        public override Dictionary<string, string> DefaultValues
        {
            get { return _DefaultValues; }
            set { _DefaultValues = value; }
        }

        public bool ManifestDownload { get; set; }
        public bool UseTeamCity { get; set; }
        public string TeamCityUrl { get; set; }
        public string TeamCityProject { get; set; }
        public string Branch { get; set; }

        public string NormalUpdateUrl { get; set; }
    }
}
