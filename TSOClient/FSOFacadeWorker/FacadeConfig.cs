using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSOFacadeWorker
{
    public class FacadeConfig
    {
        public string Api_Url;
        public string User;
        public string Password;
        public string Game_Path;
        public int Limit = 2000;
        public int Sleep_Time = 30000;
    }
}
