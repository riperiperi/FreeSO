using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Utils
{
    public static class DebugUtils
    {
        public static string LogObject(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
