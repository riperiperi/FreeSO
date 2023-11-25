using Newtonsoft.Json;

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
